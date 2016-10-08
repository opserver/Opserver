using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Configuration;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Opserver.Monitoring;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data
{
    public class Cache<T> : Cache where T : class
    {
        /// <summary>
        /// Returns if this cache has data - THIS WILL NOT TRIGGER A FETCH
        /// </summary>
        public override bool ContainsData => _hasData == 1 && Data != null;
        private int _hasData;
        internal override object InnerCache => DataTask;
        public override Type Type => typeof (T);
        private readonly SemaphoreSlim _pollSemaphoreSlim = new SemaphoreSlim(1);

        public override string InventoryDescription
        {
            get
            {
                var tmp = DataTask;
                return tmp == null ? null : ((tmp as IList)?.Count.Pluralize("item") ?? "1 Item");
            }
        }

        private readonly Func<Task<T>> _updateFunc;
        private Task<T> DataTask { get; set; }
        public T Data { get; private set; }

        // TODO: Find name that doesn't suck, has to override so...
        public override Task PollGenericAsync(bool force = false) => PollAsync(force);
        
        // This makes more semantic sense...
        public Task<T> GetData() => PollAsync();

        public Task<T> PollAsync(bool force = false)
        {
            if (force) _needsPoll = true;

            // First call polls data.
            if ((_hasData == 0 && Interlocked.CompareExchange(ref _hasData, 1, 0) == 0) || force)
            {
                DataTask = UpdateAsync();
            }
            // Force polls and replaces data when done.
            else if (IsStale)
            {
                return UpdateAsync().ContinueWith(_ =>
                    {
                        DataTask = _;
                        return _.GetAwaiter().GetResult();
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
            return DataTask;
        }

        private async Task<T> UpdateAsync()
        {
            Interlocked.Increment(ref PollingEngine._activePolls);
            PollStatus = "UpdateAsync";
            if (!_needsPoll && !IsStale) return Data;

            PollStatus = "Awaiting Semaphore";
            await _pollSemaphoreSlim.WaitAsync().ConfigureAwait(false);
            bool errored = false;
            try
            {
                if (!_needsPoll && !IsStale) return Data;
                if (_isPolling) return Data;
                CurrentPollDuration = Stopwatch.StartNew();
                _isPolling = true;
                Interlocked.Increment(ref _pollsTotal);
                PollStatus = "UpdateCache";
                await _updateFunc().ConfigureAwait(false);
                PollStatus = "UpdateCache Complete";
                _needsPoll = false;
                if (DataTask != null)
                    Interlocked.Increment(ref _pollsSuccessful);
            }
            catch (Exception e)
            {
                var errorMessage = e.Message;
                if (e.InnerException != null) errorMessage += "\n" + e.InnerException.Message;
                SetFail(errorMessage);
                errored = true;
            }
            finally
            {
                if (CurrentPollDuration != null)
                {
                    CurrentPollDuration.Stop();
                    LastPollDuration = CurrentPollDuration.Elapsed;
                }
                CurrentPollDuration = null;
                _isPolling = false;
                PollStatus = errored ? "Failed" : "Completed";
                _pollSemaphoreSlim.Release();
                Interlocked.Decrement(ref PollingEngine._activePolls);
            }
            return Data;
        }

        private string miniProfilerDescription { get; }

        /// <summary>
        /// Creates a cache poller
        /// </summary>
        /// <typeparam name="T">Type of item in the cache</typeparam>
        /// <param name="owner">The PollNode owner of this Cache</param>
        /// <param name="description">Description of the operation, used purely for profiling</param>
        /// <param name="getData">The operation used to actually get data, e.g. <code>using (var conn = GetConnectionAsync()) { return getFromConnection(conn); }</code></param>
        /// <param name="timeoutMs">The timeout in milliseconds for this poll to complete before aborting.</param>
        /// <param name="logExceptions">Whether to log any exceptions to the log</param>
        /// <param name="addExceptionData">Optionally add exception data, e.g. <code>e => e.AddLoggedData("Server", Name)</code></param>
        /// <param name="afterPoll">An optional action to run after polling has completed successfully</param>
        /// <returns>A cache update action, used when creating a <see cref="Cache"/>.</returns>
        public Cache(PollNode owner,
            string description,
            int cacheForSeconds,
            Func<Task<T>> getData,
            int? timeoutMs = null,
            bool logExceptions = false, // TODO: Settings
            Action<Exception> addExceptionData = null,
            Action<Cache<T>> afterPoll = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
            : base(cacheForSeconds, memberName, sourceFilePath, sourceLineNumber)
        {
            ParentMemberName = memberName;
            SourceFilePath = sourceFilePath;
            SourceLineNumber = sourceLineNumber;

            miniProfilerDescription = "Poll: " + description; // contcat once

            _updateFunc = async () =>
            {
                var success = true;
                PollStatus = "UpdateCacheItem";
                if (OpserverProfileProvider.EnablePollerProfiling)
                {
                    Profiler = OpserverProfileProvider.CreateContextProfiler(miniProfilerDescription, UniqueId,
                        store: false);
                }
                using (MiniProfiler.Current.Step(description))
                {
                    try
                    {
                        PollStatus = "Fetching";
                        using (MiniProfiler.Current.Step("Data Fetch"))
                        {
                            var task = getData();
                            if (timeoutMs.HasValue)
                            {
                                if (await Task.WhenAny(task, Task.Delay(timeoutMs.Value)) == task)
                                {
                                    // Re-await for throws.
                                    Data = await task;
                                }
                                else
                                {
                                    // This means the whenany returned the timeout first...boom.
                                    throw new TimeoutException($"Fetch timed out after {timeoutMs.ToString()} ms.");
                                }
                            }
                            else
                            {
                                Data = await task;
                            }
                        }
                        PollStatus = "Fetch Complete";
                        SetSuccess();
                        afterPoll?.Invoke(this);
                    }
                    catch (Exception e)
                    {
                        success = false;
                        if (logExceptions)
                        {
                            addExceptionData?.Invoke(e);
                            Current.LogException(e);
                        }
                        var errorMessage = StringBuilderCache.Get()
                            .Append("Unable to fetch from ")
                            .Append(owner.NodeType)
                            .Append(": ")
                            .Append(e.Message);
#if DEBUG
                        errorMessage.Append(" @ ").Append(e.StackTrace);
#endif
                        if (e.InnerException != null) errorMessage.AppendLine().Append(e.InnerException.Message);
                        PollStatus = "Fetch Failed";
                        SetFail(errorMessage.ToStringRecycle());
                    }
                    owner.PollComplete(this, success);
                }
                if (OpserverProfileProvider.EnablePollerProfiling)
                {
                    OpserverProfileProvider.StopContextProfiler();
                }
                PollStatus = "UpdateCacheItem Complete";
                return Data;
            };
        }
    }

    public abstract class Cache : IMonitorStatus
    {
        public int? CacheFailureForSeconds { get; set; } = 15;
        public int CacheForSeconds { get; }
        public bool AffectsNodeStatus { get; set; }
        public virtual Type Type => typeof(Cache);
        public Guid UniqueId { get; }
        
        public bool ShouldPoll => _needsPoll || IsStale && !_isPolling;

        internal volatile bool _needsPoll = true;
        protected volatile bool _isPolling;
        public bool IsPolling => _isPolling;
        public bool IsStale => (NextPoll ?? DateTime.MinValue) < DateTime.UtcNow;
        public DateTime? NextPoll =>
            LastPoll?.AddSeconds(LastPollSuccessful
                ? CacheForSeconds
                : CacheFailureForSeconds.GetValueOrDefault(CacheForSeconds));

        protected long _pollsTotal, _pollsSuccessful;
        public long PollsTotal => _pollsTotal;
        public long PollsSuccessful => _pollsSuccessful;

        public Stopwatch CurrentPollDuration { get; protected set; }
        public DateTime? LastPoll { get; internal set; }
        public TimeSpan? LastPollDuration { get; internal set; }
        public DateTime? LastSuccess { get; internal set; }
        public bool LastPollSuccessful { get; internal set; }

        /// <summary>
        /// If profiling for cache polls is active, this contains a MiniProfiler of the current or last poll
        /// </summary>
        public MiniProfiler Profiler { get; set; }

        internal void SetSuccess()
        {
            LastSuccess = LastPoll = DateTime.UtcNow;
            LastPollSuccessful = true;
            ErrorMessage = "";
        }

        internal void SetFail(string errorMessage)
        {
            LastPoll = DateTime.UtcNow;
            LastPollSuccessful = false;
            ErrorMessage = errorMessage;
        }
        public string PollStatus { get; internal set; }

        public MonitorStatus MonitorStatus
        {
            get
            {
                if (LastPoll == null ) return MonitorStatus.Unknown;
                return LastPollSuccessful ? MonitorStatus.Good : MonitorStatus.Critical;
            }
        }
        public string MonitorStatusReason
        {
            get
            {
                if (LastPoll == null) return "Never Polled";
                return !LastPollSuccessful ? "Poll " + LastPoll?.ToRelativeTime() + " failed: " + ErrorMessage : null;
            }
        }

        protected static readonly ConcurrentDictionary<string, object> NullLocks = new ConcurrentDictionary<string, object>();
        protected static readonly ConcurrentDictionary<string, SemaphoreSlim> NullSlims = new ConcurrentDictionary<string, SemaphoreSlim>();
        
        public virtual bool ContainsData => false;
        internal virtual object InnerCache => null;
        public string ErrorMessage { get; internal set; }
        public virtual string InventoryDescription => null;

        public abstract Task PollGenericAsync(bool force = false);

        /// <summary>
        /// Info for monitoring the monitoring, debugging, etc.
        /// </summary>
        public string ParentMemberName { get; protected set; }
        public string SourceFilePath { get; protected set; }
        public int SourceLineNumber { get; protected set; }

        protected Cache(
            int cacheForSeconds,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            UniqueId = Guid.NewGuid();
            CacheForSeconds = cacheForSeconds;
            ParentMemberName = memberName;
            SourceFilePath = sourceFilePath;
            SourceLineNumber = sourceLineNumber;
        }
        
        /// <summary>
        /// Gets a cache stored in LocalCache by key...these are not polled and expire when stale
        /// </summary>
        public static Cache<T> GetKeyedCache<T>(string key, Func<Cache<T>> create) where T : class
        {
            var result = Current.LocalCache.Get<Cache<T>>(key);
            if (result == null)
            {
                result = create();
                Current.LocalCache.Set(key, result, result.CacheForSeconds);
            }
            return result;
        }
    }
}
