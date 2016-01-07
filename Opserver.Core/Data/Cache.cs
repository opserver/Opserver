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
        private readonly object _pollLock = new object();

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
                    Poll(wait: true);
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

        public override int Poll(bool force = false, bool wait = false)
        {
            int result;
            if (CacheKey.HasValue())
            {
                result = CachePoll(force);
            }
            else
            {
                if (force) _needsPoll = true;
                result = Update();
                // If we're in need of cache and don't have it, then wait on the polling thread
                if (wait && IsPolling)
                {
                    lock (_pollLock)
                    {
                        Monitor.Wait(_pollLock, 5000);
                    }
                }
            }
            return result;
        }

        private int Update()
        {
            if (!_needsPoll && !IsStale) return 0;
            
            lock (_pollLock)
            {
                if (_isPolling) return 0;
                var sw = Stopwatch.StartNew();
                _isPolling = true;
                try
                {
                    Interlocked.Increment(ref _pollsTotal);
                    UpdateCache(this);
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
                    return 0;
                }
                finally
                {
                    _isPolling = false;
                    sw.Stop();
                    LastPollDuration = sw.Elapsed;
                    Monitor.PulseAll(_pollLock);
                }
            }
        }
        
        private int CachePoll(bool force)
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
            if (cached == null)
            {
                lock (loadLock)
                {
                    // See if we have the value cached
                    cached = Current.LocalCache.Get<Cache<T>>(CacheKey);
                    if (cached == null)
                    {
                        // No data, run this synchronously to get data
                        var result = Update();
                        Current.LocalCache.Set(CacheKey, this, CacheForSeconds + CacheStaleForSeconds);
                        return result;
                    }
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
            if (refreshLockSuccess)
            {
                var task = new Task(() =>
                    {
                        lock (loadLock)
                        {
                            try
                            {
                                Update();
                                Current.LocalCache.Set(CacheKey, this, CacheForSeconds + CacheStaleForSeconds);
                            }
                            finally
                            {
                                Current.LocalCache.Remove(CompeteKey);
                            }
                        }
                    });
                task.ContinueWith(t =>
                    {
                        if (t.IsFaulted) Current.LogException(t.Exception);
                    });
                task.Start();
            }
            return 0;
        }

        private bool GotCompeteLock()
        {
            if (!Current.LocalCache.SetNXSync(CompeteKey, DateTime.UtcNow))
            {
                var x = Current.LocalCache.Get<DateTime>(CompeteKey);
                // Somebody abandoned the lock, clear it and try again
                if (DateTime.UtcNow - x > TimeSpan.FromMinutes(5))
                {
                    Current.LocalCache.Remove(CompeteKey);
                    return GotCompeteLock();
                }
                return false;
            }
            return true;
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

    public class Cache : IMonitorStatus
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

        internal volatile bool _needsPoll = true;
        protected volatile bool _isPolling;
        public bool IsPolling => _isPolling;
        public bool IsStale => NextPoll < DateTime.UtcNow;
        public bool IsExpired => LastPoll.AddSeconds(CacheForSeconds + CacheStaleForSeconds) < DateTime.UtcNow;

        protected long _pollsTotal, _pollsSuccessful;
        public long PollsTotal => _pollsTotal;
        public long PollsSuccessful => _pollsSuccessful;
        // TODO: Convert to nullable, handle everywhere
        public DateTime LastPoll { get; internal set; }

        public DateTime NextPoll =>
            LastPoll.AddSeconds(LastPollSuccessful
                ? CacheForSeconds
                : CacheFailureForSeconds.GetValueOrDefault(CacheForSeconds));

        public TimeSpan LastPollDuration { get; internal set; }
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

        public MonitorStatus MonitorStatus
        {
            get
            {
                if (LastPoll == DateTime.MinValue) return MonitorStatus.Unknown;
                return LastPollSuccessful ? MonitorStatus.Good : MonitorStatus.Critical;
            }
        }
        public string MonitorStatusReason
        {
            get
            {
                if (LastPoll == DateTime.MinValue) return "Never Polled";
                return !LastPollSuccessful ? "Poll " + LastPoll.ToRelativeTime() + " failed: " + ErrorMessage : null;
            }
        }

        protected static readonly ConcurrentDictionary<string, object> NullLocks = new ConcurrentDictionary<string, object>();
        
        public virtual bool ContainsData => false;
        public virtual object GetData() { return null; }
        public string ErrorMessage { get; internal set; }
        public virtual string InventoryDescription => null;

        public virtual int Poll(bool force = false, bool wait = false)
        {
            return 0;
        }

        public virtual void Purge() { }

        /// <summary>
        /// Info for monitoring the monitoring, debugging, etc.
        /// </summary>
        public string ParentMemberName { get; protected set; }
        public string SourceFilePath { get; protected set; }
        public int SourceLineNumber { get; protected set; }
        public Cache([CallerMemberName] string memberName = "",
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
