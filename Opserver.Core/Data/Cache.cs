using System;
using System.Collections;
using System.Diagnostics;
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
                var tmp = Data;
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
            await _pollSemaphoreSlim.WaitAsync();
            bool errored = false;
            try
            {
                if (!_needsPoll && !IsStale) return Data;
                if (_isPolling) return Data;
                CurrentPollDuration = Stopwatch.StartNew();
                _isPolling = true;
                PollStatus = "UpdateCache";
                await _updateFunc();
                PollStatus = "UpdateCache Complete";
                _needsPoll = false;
                Interlocked.Increment(ref _pollsTotal);
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
            TimeSpan cacheDuration,
            Func<Task<T>> getData,
            int? timeoutMs = null,
            bool logExceptions = false, // TODO: Settings
            Action<Exception> addExceptionData = null,
            Action<Cache<T>> afterPoll = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
            : base(cacheDuration, memberName, sourceFilePath, sourceLineNumber)
        {
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

    /// <summary>
    /// A lightweight cache class for storing and handling overlap for cache refreshment for on-demand items
    /// </summary>
    /// <typeparam name="T">Type stored in this cache</typeparam>
    public class LightweightCache<T> where T : class
    {
        public T Data { get; private set; }
        public DateTime? LastFetch { get; private set; }
        public Exception Error { get; private set; }
        public bool Successful => Error == null;
        public string ErrorMessage => Error?.Message + (Error?.InnerException != null ? "\n" + Error.InnerException.Message : "");

        // Temp: all async when views can be in MVC Core
        public static LightweightCache<T> Get(string key, Func<T> getData, TimeSpan duration, TimeSpan staleDuration)
        {
            using (MiniProfiler.Current.Step("LightweightCache: " + key))
            {
                // Let GetSet handle the overlap and locking, for now. That way it's store dependent.
                return Current.LocalCache.GetSet<LightweightCache<T>>(key, (old, ctx) =>
                {
                    var tc = new LightweightCache<T>();
                    try
                    {
                        tc.Data = getData();
                    }
                    catch (Exception e)
                    {
                        tc.Error = e;
                        Current.LogException(e);
                    }
                    tc.LastFetch = DateTime.UtcNow;
                    return tc;
                }, duration, staleDuration);
            }
        }
    }

    public abstract class Cache : IMonitorStatus
    {
        public virtual Type Type => typeof(Cache);
        public Guid UniqueId { get; }
        public TimeSpan CacheDuration { get; }
        public TimeSpan? CacheFailureDuration { get; set; } = TimeSpan.FromSeconds(15);
        public bool AffectsNodeStatus { get; set; }
        
        public bool ShouldPoll => _needsPoll || IsStale && !_isPolling;

        internal volatile bool _needsPoll = true;
        protected volatile bool _isPolling;
        public bool IsPolling => _isPolling;
        public bool IsStale => (NextPoll ?? DateTime.MinValue) < DateTime.UtcNow;

        protected long _pollsTotal, _pollsSuccessful;
        public long PollsTotal => _pollsTotal;
        public long PollsSuccessful => _pollsSuccessful;

        public Stopwatch CurrentPollDuration { get; protected set; }
        public DateTime? NextPoll { get; protected set; }
        public DateTime? LastPoll { get; internal set; }
        public TimeSpan? LastPollDuration { get; internal set; }
        public DateTime? LastSuccess { get; internal set; }
        public bool LastPollSuccessful { get; internal set; }

        /// <summary>
        /// If profiling for cache polls is active, this contains a MiniProfiler of the current or last poll
        /// </summary>
        public MiniProfiler Profiler { get; protected set; }

        internal void SetSuccess()
        {
            LastSuccess = LastPoll = DateTime.UtcNow;
            NextPoll = DateTime.UtcNow.Add(CacheDuration);
            LastPollSuccessful = true;
            ErrorMessage = "";
        }

        internal void SetFail(string errorMessage)
        {
            LastPoll = DateTime.UtcNow;
            NextPoll = DateTime.UtcNow.Add(CacheFailureDuration ?? CacheDuration);
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
        
        public virtual bool ContainsData => false;
        internal virtual object InnerCache => null;
        public Exception Error { get; internal set; }
        public string ErrorMessage { get; internal set; }
        public virtual string InventoryDescription => null;

        public abstract Task PollGenericAsync(bool force = false);

        /// <summary>
        /// Info for monitoring the monitoring, debugging, etc.
        /// </summary>
        public string ParentMemberName { get; }
        public string SourceFilePath { get; }
        public int SourceLineNumber { get; }

        protected Cache(
            TimeSpan cacheDuration,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            UniqueId = Guid.NewGuid();
            CacheDuration = cacheDuration;
            ParentMemberName = memberName;
            SourceFilePath = sourceFilePath;
            SourceLineNumber = sourceLineNumber;
        }

        /// <summary>
        /// Gets a cache stored in LocalCache by key...these are not polled and expire when stale
        /// </summary>
        public static LightweightCache<T> GetTimedCache<T>(string key, Func<T> getData, TimeSpan duration, TimeSpan staleDuration) where T : class
            => LightweightCache<T>.Get(key, getData, duration, staleDuration);
    }
}
