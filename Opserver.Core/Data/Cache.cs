using System;
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

        private T DataBacker { get; set; }
        public T Data
        {
            get
            {
                if (NeedsPoll)
                {
                    Poll();
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

        public override int Poll(bool force = false)
        {
            int result;
            if (CacheKey.HasValue())
            {
                result = CachePoll(force);
            }
            else
            {
                if (force) NeedsPoll = true;
                result = Update();
            }
            return result;
        }

        private int Update()
        {
            if (!NeedsPoll && !IsStale) return 0;

            if (IsPolling) return 0;

            var sw = Stopwatch.StartNew();
            IsPolling = true;
            try
            {
                Interlocked.Increment(ref _pollsTotal);
                UpdateCache(this);
                LastPollStatus = LastSuccess.HasValue && LastSuccess == LastPoll
                                     ? FetchStatus.Success
                                     : FetchStatus.Fail;
                NeedsPoll = false;
                if (DataBacker != null)
                    Interlocked.Increment(ref _pollsSuccessful);
                return DataBacker != null ? 1 : 0;
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                if (e.InnerException != null) ErrorMessage += "\n" + e.InnerException.Message;
                LastPollStatus = FetchStatus.Fail;
                return 0;
            }
            finally
            {
                IsPolling = false;
                sw.Stop();
                LastPollDuration = sw.Elapsed;
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
        
        public override void PollBackground()
        {
            Task.Run(() => Poll());
        }

        public override void Purge()
        {
            NeedsPoll = true;
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

        internal bool NeedsPoll = true;
        private volatile bool _isPolling;
        public bool IsPolling { get { return _isPolling; } internal set { _isPolling = value; } }
        public bool IsStale => NextPoll < DateTime.UtcNow;
        public bool IsExpired => LastPoll.AddSeconds(CacheForSeconds + CacheStaleForSeconds) < DateTime.UtcNow;

        protected long _pollsTotal, _pollsSuccessful;
        public long PollsTotal => _pollsTotal;
        public long PollsSuccessful => _pollsSuccessful;
        public DateTime LastPoll { get; internal set; }

        public DateTime NextPoll =>
            LastPoll.AddSeconds(LastPollStatus == FetchStatus.Fail
                ? CacheFailureForSeconds.GetValueOrDefault(CacheForSeconds)
                : CacheForSeconds);

        public TimeSpan LastPollDuration { get; internal set; }
        public DateTime? LastSuccess { get; internal set; }
        public FetchStatus LastPollStatus { get; set; }
        public MonitorStatus MonitorStatus
        {
            get
            {
                if (LastPoll == DateTime.MinValue) return MonitorStatus.Unknown;
                return LastPollStatus == FetchStatus.Fail ? MonitorStatus.Critical : MonitorStatus.Good;
            }
        }
        public string MonitorStatusReason
        {
            get
            {
                if (LastPoll == DateTime.MinValue) return "Never Polled";
                return LastPollStatus == FetchStatus.Fail ? "Poll " + LastPoll.ToRelativeTime() + " failed: " + ErrorMessage : null;
            }
        }

        protected static readonly ConcurrentDictionary<string, object> NullLocks = new ConcurrentDictionary<string, object>();
        
        public virtual bool ContainsData => false;
        public virtual object GetData() { return null; }
        public string ErrorMessage { get; internal set; }

        public virtual int Poll(bool force = false)
        {
            return 0;
        }

        public virtual void PollBackground() { }

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
