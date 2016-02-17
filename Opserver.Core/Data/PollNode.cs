using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Opserver.Monitoring;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data
{
    public abstract partial class PollNode : IMonitorStatus, IDisposable, IEquatable<PollNode>
    {
        private int _totalPolls, _totalCachePolls, _totalCacheQueues;

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
        public Stopwatch CurrentPollDuration { get; protected set; }
        protected int PollFailsInaRow;

        public virtual Cache LastFetch { get; private set; }

        protected volatile bool _isPolling;
        public bool IsPolling => _isPolling;
        public string PollStatus { get; protected set; }

        public AutoResetEvent FirstPollRun = new AutoResetEvent(false);

        public virtual void Poll(bool force = false)
        {
            PollImpl(force, true);
        }

        public virtual void PollAsync(bool force = false)
        {
            PollImpl(force, false);
        }
        
        protected virtual void PollImpl(bool force, bool wait)
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

                PollStatus = "Poll Started";
                InnerPollImpl(force, wait);
                PollStatus = "Poll Complete";
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
        private void InnerPollImpl(bool force = false, bool sync = false)
        {
            CurrentPollDuration = Stopwatch.StartNew();
            try
            {
                PollStatus = "InnerPoll Started";
                if (Polling != null)
                {
                    var ps = new PollStartArgs();
                    Polling(this, ps);
                    if (ps.AbortPoll) return;
                }

                int toPoll = 0;
                if (sync || FirstPollRun != null)
                {
                    PollStatus = "DataPollers Queueing (Sync)";
                    var tasks = DataPollers
                        .Where(p => force || p.ShouldPoll)
                        .Select(p => p.PollAsync(force))
                        .ToArray<Task>();
                    Task.WaitAll(tasks);
                    PollStatus = "DataPollers Complete (Sync)";
                }
                else
                {
                    PollStatus = "DataPollers Queueing";
                    foreach (var p in DataPollers)
                    {
                        // Cheap checks to eliminate many uncessary task creations
                        if (!force && !p.ShouldPoll) continue;
                        // Kick off the poll and don't wait for it to continue;
#pragma warning disable 4014
                        p.PollStatus = "Kicked off by Node";
                        Interlocked.Add(ref _totalCacheQueues, 1);
                        p.PollAsync(force).ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                Current.LogException(t.Exception);
                                PollStatus = "Faulted";
                            }
                            else
                            {
                                PollStatus = "Completed";
                            }
                            Interlocked.Add(ref _totalCachePolls, t.Result);
                        }, TaskContinuationOptions.ExecuteSynchronously).ConfigureAwait(false);
                        toPoll++;
#pragma warning restore 4014
                    }
                    PollStatus = toPoll.ToComma() + " DataPollers Started";
                }

                LastPoll = DateTime.UtcNow;
                Polled?.Invoke(this, new PollResultArgs {Queued = toPoll});
                if (FirstPollRun != null)
                {
                    FirstPollRun.Set();
                    FirstPollRun = null;
                }
                
                Interlocked.Increment(ref _totalPolls);
            }
            finally
            {
                CurrentPollDuration.Stop();
                LastPollDuration = CurrentPollDuration.Elapsed;
                 _isPolling = false;
                CurrentPollDuration = null;
                PollStatus = "InnerPoll Complete";
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
        protected Func<Cache<T>, Task> UpdateCacheItem<T>(string description,
                                                      Func<Task<T>> getData,
                                                      bool logExceptions = false, // TODO: Settings
                                                      Action<Exception> addExceptionData = null,
                                                      int? timeoutMs = null) where T : class
        {
            return async cache =>
            {
                cache.PollStatus = "UpdateCacheItem";
                if (OpserverProfileProvider.EnablePollerProfiling)
                {
                    cache.Profiler = OpserverProfileProvider.CreateContextProfiler("Poll: " + description, cache.UniqueId, store: false);
                }
                using (MiniProfiler.Current.Step(description))
                {
                    CacheItemFetching?.Invoke(this, EventArgs.Empty);
                    try
                    {
                        cache.PollStatus = "Fetching";
                        using (MiniProfiler.Current.Step("Data Fetch"))
                        {
                            var fetch = getData();
                            if (timeoutMs.HasValue)
                            {
                                if (await Task.WhenAny(fetch, Task.Delay(timeoutMs.Value)) == fetch)
                                {
                                    // Re-await for throws.
                                    cache.SetData(await fetch.ConfigureAwait(false));
                                }
                                else
                                {
                                    throw new TimeoutException($"Fetch timed out after {timeoutMs.ToString()} ms.");
                                }
                            }
                            else
                            {
                                cache.SetData(await fetch.ConfigureAwait(false));
                            }
                        }
                        cache.PollStatus = "Fetch Complete";
                        cache.SetSuccess();
                        PollFailsInaRow = 0;
                    }
                    catch (Exception e)
                    {
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
                        cache.PollStatus = "Fetch Failed";
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
                cache.PollStatus = "UpdateCacheItem Complete";
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
