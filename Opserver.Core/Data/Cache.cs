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
        public override object GetData() { return DataBacker; }
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
                if (_needsPoll)
                {
                    PollAsync(wait: true).Wait();
                }
                return DataBacker;
            }
            internal set { DataBacker = value; }
        }
        public Action<Cache<T>> UpdateCache { get; set; }

        /// <summary>
        /// If profiling for cache polls is active, this contains a MiniProfiler of the current or last poll
        /// </summary>
        public MiniProfiler Profiler { get; set; }

        public override async Task<int> PollAsync(bool force = false, bool wait = false)
        {
            Interlocked.Increment(ref PollingEngine._activePolls);
            int result;
            if (CacheKey.HasValue())
            {
                result = await CachePollAsync(force);
            }
            else
            {
                if (force) _needsPoll = true;
                result = await UpdateAsync();
                // If we're in need of cache and don't have it, then wait on the polling thread
                if (wait && IsPolling)
                {
                    await _pollSemaphoreSlim.WaitAsync(5000);
                }
            }
            Interlocked.Decrement(ref PollingEngine._activePolls);
            return result;
        }

        private async Task<int> UpdateAsync()
        {
            PollStatus = "UpdateAsync";
            if (!_needsPoll && !IsStale) return 0;

            PollStatus = "Awaiting Semaphore";
            await _pollSemaphoreSlim.WaitAsync();
            if (_isPolling) return 0;
            CurrentPollDuration = Stopwatch.StartNew();
            _isPolling = true;
            bool errored = false;
            try
            {
                Interlocked.Increment(ref _pollsTotal);
                PollStatus = "UpdateCache";
                // TODO: Async
                UpdateCache(this);
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
                CurrentPollDuration.Stop();
                LastPollDuration = CurrentPollDuration.Elapsed;
                _isPolling = false;
                CurrentPollDuration = null;
                _pollSemaphoreSlim.Release();
                PollStatus = errored ? "Failed" : "Completed";
            }
        }

        private async Task<int> CachePollAsync(bool force)
        {
            // Cache is valid, nothing to do
            if (!force && !IsStale) return 0;

            if (DataBacker != null) return 0;

            Action<Cache<T>> copyPropertiesFrom = c =>
                {
                    DataBacker = c.DataBacker;
                    LastPoll = c.LastPoll;
                    LastSuccess = c.LastSuccess;
                    ErrorMessage = c.ErrorMessage;
                };

            var lockKey = CacheKey;
            var cached = Current.LocalCache.Get<Cache<T>>(CacheKey);
            var loadLock = NullLocks.AddOrUpdate(lockKey, k => new object(), (k, old) => old);
            var loadSemaphore = NullSlims.AddOrUpdate(lockKey, k => new SemaphoreSlim(1), (k, old) => old);
            if (cached == null)
            {
                await loadSemaphore.WaitAsync();
                try
                {
                    // See if we have the value cached
                    cached = Current.LocalCache.Get<Cache<T>>(CacheKey);
                    if (cached == null)
                    {
                        // No data, run this synchronously to get data
                        var result = await UpdateAsync();
                        Current.LocalCache.Set(CacheKey, this, CacheForSeconds + CacheStaleForSeconds);
                        return result;
                    }
                }
                finally
                {
                    loadSemaphore.Release();
                }
            }
            // So we hit cache, copy stuff over
            copyPropertiesFrom(cached);
            // If we're not stale or don't cache stale, nothing else to do
            if (!IsStale || CacheStaleForSeconds == 0) return 0;
            // Oh no, we're stale - kick off a background refresh

            var refreshLockSuccess = false;
            if (Monitor.TryEnter(loadLock, 0))
            {
                try
                {
                    refreshLockSuccess = GotCompeteLock();
                }
                finally
                {
                    Monitor.Exit(loadLock);
                }
            }
            // TODO: Address pile-up on background refreshes
            if (refreshLockSuccess)
            {
                var task = new Task(async () =>
                {
                    await loadSemaphore.WaitAsync();
                    try
                    {
                        await UpdateAsync();
                        Current.LocalCache.Set(CacheKey, this, CacheForSeconds + CacheStaleForSeconds);
                    }
                    finally
                    {
                        Current.LocalCache.Remove(CompeteKey);
                        loadSemaphore.Release();
                    }
                });
                // Intentional background
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Current.LogException(t.Exception);
                    }
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                task.Start();
            }
            return 0;
        }

        private bool GotCompeteLock()
        {
            while (true)
            {
                if (!Current.LocalCache.SetNXSync(CompeteKey, DateTime.UtcNow))
                {
                    var x = Current.LocalCache.Get<DateTime>(CompeteKey);
                    // Somebody abandoned the lock, clear it and try again
                    if (DateTime.UtcNow - x > TimeSpan.FromMinutes(5))
                    {
                        Current.LocalCache.Remove(CompeteKey);
                        continue;
                    }
                    return false;
                }
                return true;
            }
        }

        public override void Purge()
        {
            _needsPoll = true;
            Data = null;
            if (CacheKey.HasValue())
                Current.LocalCache.Remove(CacheKey);
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
        public string CacheKey { get; set; }
        protected string CompeteKey => CacheKey + "-compete";
        public int? CacheFailureForSeconds { get; set; }
        public int CacheForSeconds { get; set; }
        public int CacheStaleForSeconds { get; set; }
        public bool AffectsNodeStatus { get; set; }
        public virtual Type Type => typeof(Cache);
        public Guid UniqueId { get; private set; }
        
        public bool ShouldPoll => _needsPoll || IsStale && !_isPolling;

        internal volatile bool _needsPoll = true;
        protected volatile bool _isPolling;
        public bool IsPolling => _isPolling;
        public bool IsStale => LastPoll?.AddSeconds(LastPollSuccessful
            ? CacheForSeconds
            : CacheFailureForSeconds.GetValueOrDefault(CacheForSeconds)) < DateTime.UtcNow;
        public bool IsExpired => LastPoll?.AddSeconds(CacheForSeconds + CacheStaleForSeconds) < DateTime.UtcNow;

        protected long _pollsTotal, _pollsSuccessful;
        public long PollsTotal => _pollsTotal;
        public long PollsSuccessful => _pollsSuccessful;

        public Stopwatch CurrentPollDuration { get; protected set; }
        public DateTime? LastPoll { get; internal set; }
        public TimeSpan? LastPollDuration { get; internal set; }
        public DateTime? LastSuccess { get; internal set; }
        public bool LastPollSuccessful { get; internal set; }
        
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
        public virtual object GetData() { return null; }
        public string ErrorMessage { get; internal set; }
        public virtual string InventoryDescription => null;

        public abstract Task<int> PollAsync(bool force = false, bool wait = false);

        public virtual void Purge() { }

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
