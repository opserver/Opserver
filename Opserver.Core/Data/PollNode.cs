using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jil;
using StackExchange.Opserver.Monitoring;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data
{
    public abstract partial class PollNode : IMonitorStatus, IDisposable, IEquatable<PollNode>
    {
        private int _totalPolls;
        private int _totalCachePolls;

        public abstract int MinSecondsBetweenPolls { get; }
        public abstract string NodeType { get; }
        public abstract IEnumerable<Cache> DataPollers { get; }
        protected abstract IEnumerable<MonitorStatus> GetMonitorStatus();
        protected abstract string GetMonitorStatusReason();

        /// <summary>
        /// Number of consecutive cache fetch failures before backing off of polling the entire node for <see cref="BackoffDuration"/>
        /// </summary>
        protected int FailsBeforeBackoff => 3;

        /// <summary>
        /// Length of time to backoff once <see cref="FailsBeforeBackoff"/> is hit
        /// </summary>
        protected virtual TimeSpan BackoffDuration => TimeSpan.FromSeconds(30);

        /// <summary>
        /// Indicates if this was added to the global poller list, if false that means this is a duplicate
        /// and should not be used anywhere, you lost the race, let it go.
        /// </summary>
        public bool AddedToGlobalPollers { get; private set; }
        public string UniqueKey { get; }

        protected PollNode(string uniqueKey)
        {
            UniqueKey = uniqueKey;
        }

        /// <summary>
        /// Tries added this node to the global polling engine.  If another copy of this node is present (same unique key)
        /// then this method will return false, indicating it was not added and will not be polled.
        /// </summary>
        /// <remarks>If this return FALSE, be sure not to cache the node anywhere else, as it should likely be garbage collected</remarks>
        /// <returns>Whether the node was added to the list of global pollers</returns>
        public bool TryAddToGlobalPollers()
        {
            return AddedToGlobalPollers = PollingEngine.TryAdd(this);
        }
        
        private readonly object _monitorStatusLock = new object();
        protected MonitorStatus? PreviousMonitorStatus;
        protected MonitorStatus? CachedMonitorStatus;
        public virtual MonitorStatus MonitorStatus
        {
            get
            {
                if (!CachedMonitorStatus.HasValue)
                {
                    lock (_monitorStatusLock)
                    {
                        if (CachedMonitorStatus.HasValue)
                            return CachedMonitorStatus.Value;

                        var pollers = DataPollers.Where(dp => dp.AffectsNodeStatus).ToList();
                        var fetchStatus = pollers.GetWorstStatus();
                        if (fetchStatus != MonitorStatus.Good)
                        {
                            CachedMonitorStatus = MonitorStatus.Critical;
                            MonitorStatusReason =
                                string.Join(", ", pollers.WithIssues()
                                                         .GroupBy(g => g.MonitorStatus)
                                                         .OrderByDescending(g => g.Key)
                                                         .Select(
                                                             g =>
                                                             g.Key + ": " + string.Join(", ", g.Select(p => p.ParentMemberName))
                                                      ));
                        }
                        else
                        {
                            CachedMonitorStatus = GetMonitorStatus().ToList().GetWorstStatus();
                            MonitorStatusReason = CachedMonitorStatus == MonitorStatus.Good ? null : GetMonitorStatusReason();
                        }
                        if (!PreviousMonitorStatus.HasValue || PreviousMonitorStatus != CachedMonitorStatus)
                        {
                            var handler = MonitorStatusChanged;
                            handler?.Invoke(this, new MonitorStatusArgs
                            {
                                OldMonitorStatus = PreviousMonitorStatus.Value,
                                NewMonitorStatus = CachedMonitorStatus.Value
                            });
                            PreviousMonitorStatus = CachedMonitorStatus;
                        }
                    }
                }
                return CachedMonitorStatus.GetValueOrDefault(MonitorStatus.Unknown);
            }
        }
        public string MonitorStatusReason { get; private set; }
        
        public DateTime? LastPoll { get; protected set; }
        public TimeSpan LastPollDuration { get; protected set; }
        protected int PollFailsInaRow;

        public virtual Cache LastFetch { get; private set; }

        protected volatile bool _isPolling;
        private volatile string _status;
        public bool IsPolling => _isPolling;
        public virtual string PollTaskStatus => _status;

        public AutoResetEvent FirstPollRun = new AutoResetEvent(false);


        public virtual async Task PollAsync(bool force = false)
        {
            using (MiniProfiler.Current.Step("Poll - " + UniqueKey))
            {
                // Don't poll more than once every n seconds, that's just rude
                if (!force && DateTime.UtcNow < LastPoll.GetValueOrDefault().AddSeconds(MinSecondsBetweenPolls)) 
                    return;
                 
                // If we're seeing a lot of poll failures in a row, back the hell off
                if (!force && PollFailsInaRow >= FailsBeforeBackoff && DateTime.UtcNow < LastPoll.GetValueOrDefault() + BackoffDuration)
                    return;
                
                // Prevent multiple poll threads for this node from running at once
                if (_isPolling) return;
                _isPolling = true;

                _status = "Poll Started";
                await InnerPollAsync(force);
                _status = "Poll Complete";
            }
        }

        public bool WaitForFirstPoll(int timeoutMs)
        {
            var fr = FirstPollRun;
            return fr == null || fr.WaitOne(timeoutMs);
        }

        /// <summary>
        /// Called on a background thread for when this node is ACTUALLY polling
        /// This is not called if we're not due for a poll when the pass runs
        /// </summary>
        private async Task InnerPollAsync(bool force = false)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _status = "InnerPoll Started";
                if (Polling != null)
                {
                    var ps = new PollStartArgs();
                    Polling(this, ps);
                    if (ps.AbortPoll) return;
                }

                var polled = 0;
                _status = "DataPollers Queueing";
                var toPoll = DataPollers.Select(i => i.PollAsync(force));
                _status = "DataPollers Running";
                var results = await Task.WhenAll(toPoll);
                _status = "DataPollers Completed";
                foreach (var r in results)
                {
                    Interlocked.Add(ref polled, r);
                }
                
                LastPoll = DateTime.UtcNow;
                Polled?.Invoke(this, new PollResultArgs {Polled = polled});
                if (FirstPollRun != null)
                {
                    FirstPollRun.Set();
                    FirstPollRun = null;
                }

                Interlocked.Add(ref _totalCachePolls, polled);
                Interlocked.Increment(ref _totalPolls);
            }
            finally
            {
                sw.Stop();
                LastPollDuration = sw.Elapsed;
                _isPolling = false;
                _status = "InnerPoll Complete";
            }
        }

        /// <summary>
        /// Invoked by a Cache instance on updating, using properties from the PollNode such as connection strings, etc.
        /// </summary>
        /// <typeparam name="T">Type of item in the cache</typeparam>
        /// <param name="description">Description of the operation, used purely for profiling</param>
        /// <param name="getData">The operation used to actually get data, e.g. <code>using (var conn = GetConnectionAsync()) { return getFromConnection(conn); }</code></param>
        /// <param name="logExceptions">Whether to log any exceptions to the log</param>
        /// <param name="addExceptionData">Optionally add exception data, e.g. <code>e => e.AddLoggedData("Server", Name)</code></param>
        /// <returns>A cache update action, used when creating a <see cref="Cache"/>.</returns>
        protected Action<Cache<T>> UpdateCacheItem<T>(string description,
                                                      Func<Task<T>> getData,
                                                      bool logExceptions = false, // TODO: Settings
                                                      Action<Exception> addExceptionData = null) where T : class
        {
            return async cache =>
            {
                if (OpserverProfileProvider.EnablePollerProfiling)
                {
                    cache.Profiler = OpserverProfileProvider.CreateContextProfiler("Poll: " + description, cache.UniqueId);
                }
                using (MiniProfiler.Current.Step(description))
                {
                    CacheItemFetching?.Invoke(this, EventArgs.Empty);
                    try
                    {
                        using (MiniProfiler.Current.Step("Data Fetch"))
                        {
                            cache.Data = await getData();
                        }
                        cache.SetSuccess();
                        PollFailsInaRow = 0;
                    }
                    catch (Exception e)
                    {
                        var deserializationException = e as DeserializationException;
                        if (deserializationException != null)
                        {
                            e.AddLoggedData("Snippet-After", deserializationException.SnippetAfterError)
                             .AddLoggedData("Position", deserializationException.Position.ToString())
                             .AddLoggedData("Ended-Unexpectedly", deserializationException.EndedUnexpectedly.ToString());
                        }
                        if (logExceptions)
                        {
                            addExceptionData?.Invoke(e);
                            Current.LogException(e);
                        }
                        PollFailsInaRow++;
                        var errorMessage = "Unable to fetch from " + NodeType + ": " + e.Message;
#if DEBUG
                        errorMessage += " @ " + e.StackTrace;
#endif
                        if (e.InnerException != null) errorMessage += "\n" + e.InnerException.Message;
                        cache.SetFail(errorMessage);
                    }
                    CacheItemFetched?.Invoke(this, EventArgs.Empty);
                    CachedMonitorStatus = null;
                    LastFetch = cache;
                }
                if (OpserverProfileProvider.EnablePollerProfiling)
                {
                    OpserverProfileProvider.StopContextProfiler();
                }
            };
        }

        public void Dispose()
        {
            PollingEngine.TryRemove(this);
        }

        public Cache GetCache(string property)
        {
            return DataPollers.FirstOrDefault(p => p.ParentMemberName == property);
        }
    }
}
