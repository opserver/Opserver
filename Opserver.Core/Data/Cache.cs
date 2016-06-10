using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data
{
    public class Cache<T> : Cache where T : class
    {
        /// <summary>
        /// Returns if this cache has data - THIS WILL NOT TRIGGER A FETCH
        /// </summary>
        public override bool ContainsData => DataBacker != null;
        internal override object InnerCache => DataBacker;
        public override Type Type => typeof (T);
        private readonly SemaphoreSlim _pollSemaphoreSlim = new SemaphoreSlim(1);

        public override string InventoryDescription
        {
            get
            {
                var tmp = DataBacker;
                return tmp == null ? null : ((tmp as IList)?.Count.Pluralize("item") ?? "1 Item");
            }
        }

        private T DataBacker { get; set; }
        public T Data
        {
            get
            {
                // Only wait for stale caches that need a refresh in the background
                // Really, this is going to exit quickly as the background refresh is kicked off.
                if (CacheKey.HasValue() && IsStale)
                {
                    using (MiniProfiler.Current.Step("Cache Wait: " + CacheKey + ":" + UniqueId.ToString()))
                    {
                        PollAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                }
                return DataBacker;
            }
        }

        public void SetData(T data)
        {
            DataBacker = data;
        }
        public Func<Cache<T>, Task> UpdateCache { get; set; }

        public override async Task<int> PollAsync(bool force = false)
        {
            Interlocked.Increment(ref PollingEngine._activePolls);
            int result;
            if (force) _needsPoll = true;
            if (CacheKey.HasValue())
            {
                if (force || (DataBacker == null && !LastPoll.HasValue))
                {
                    // First fetch - just poll...
                    result = await UpdateAsync().ConfigureAwait(false);
                }
                else
                {
                    // This is intentionally background fire and forget
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    UpdateAsync().ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    result = 1;
                }
            }
            else
            {
                result = await UpdateAsync().ConfigureAwait(false);
            }
            Interlocked.Decrement(ref PollingEngine._activePolls);
            return result;
        }

        private async Task<int> UpdateAsync()
        {
            PollStatus = "UpdateAsync";
            if (!_needsPoll && !IsStale) return 0;

            PollStatus = "Awaiting Semaphore";
            await _pollSemaphoreSlim.WaitAsync().ConfigureAwait(false);
            bool errored = false;
            try
            {
                if (!_needsPoll && !IsStale) return 0;
                if (_isPolling) return 0;
                CurrentPollDuration = Stopwatch.StartNew();
                _isPolling = true;
                Interlocked.Increment(ref _pollsTotal);
                PollStatus = "UpdateCache";
                await UpdateCache(this).ConfigureAwait(false);
                PollStatus = "UpdateCache Complete";
                _needsPoll = false;
                if (DataBacker != null)
                    Interlocked.Increment(ref _pollsSuccessful);
                return DataBacker != null ? 1 : 0;
            }
            catch (Exception e)
            {
                var errorMessage = e.Message;
                if (e.InnerException != null) errorMessage += "\n" + e.InnerException.Message;
                SetFail(errorMessage);
                errored = true;
                return 0;
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
            }
        }

        public static Cache<T> WithKey(
            string key,
            Func<Cache<T>, Task> update,
            int cacheSeconds,
            int cacheStaleSeconds)
        {
            var result = Current.LocalCache.Get<Cache<T>>(key);
            if (result == null)
            {
                result = new Cache<T>
                {
                    CacheKey = key,
                    CacheForSeconds = cacheSeconds,
                    UpdateCache = update
                };
                Current.LocalCache.Set(key, result, cacheSeconds + cacheStaleSeconds);
            }
            return result;
        }

        public Cache([CallerMemberName] string memberName = "",
                     [CallerFilePath] string sourceFilePath = "",
                     [CallerLineNumber] int sourceLineNumber = 0)
        {
            ParentMemberName = memberName;
            SourceFilePath = sourceFilePath;
            SourceLineNumber = sourceLineNumber;
        }
    }

    public abstract class Cache : IMonitorStatus
    {
        /// <summary>
        /// Unique key for caching, only used for items that are on-demand, e.g. methods that have cache based on parameters
        /// </summary>
        protected string CacheKey { get; set; }
        public int? CacheFailureForSeconds { get; set; } = 15;
        public int CacheForSeconds { get; set; }
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

        public abstract Task<int> PollAsync(bool force = false);

        /// <summary>
        /// Info for monitoring the monitoring, debugging, etc.
        /// </summary>
        public string ParentMemberName { get; protected set; }
        public string SourceFilePath { get; protected set; }
        public int SourceLineNumber { get; protected set; }

        protected Cache([CallerMemberName] string memberName = "",
                     [CallerFilePath] string sourceFilePath = "",
                     [CallerLineNumber] int sourceLineNumber = 0)
        {
            UniqueId = Guid.NewGuid();
            ParentMemberName = memberName;
            SourceFilePath = sourceFilePath;
            SourceLineNumber = sourceLineNumber;
        }
    }
}
