using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data
{
    public abstract partial class PollNode : IMonitorStatus, IDisposable, IEquatable<PollNode>
    {
        private int _totalPolls, _totalCachePolls;

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

        public Cache LastFetch => _lastFetch;
        private Cache _lastFetch;

        internal void PollComplete(Cache cache, bool success)
        {
            Interlocked.Exchange(ref _lastFetch, cache);
            if (success)
            {
                Interlocked.Exchange(ref PollFailsInaRow, 0);
            }
            else
            {
                Interlocked.Increment(ref PollFailsInaRow);
            }
            CachedMonitorStatus = null; // nullable type, not instruction level swappable
        }

        protected int _isPolling;
        public bool IsPolling => _isPolling > 0;
        public string PollStatus { get; protected set; }
        
        public virtual void PollAndForget(bool force = false)
        {
            PollAsync(force).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Current.LogException(t.Exception);
                }
                Interlocked.Add(ref _totalCachePolls, 1);
            }, TaskContinuationOptions.ExecuteSynchronously).ConfigureAwait(false);
        }
        
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
                if (Interlocked.CompareExchange(ref _isPolling, 1, 0) != 0)
                {
                    // We're already running, abort!'
                    // TODO: Date check for sanity and restart?
                }

                PollStatus = "Poll Started";
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

                    PollStatus = "DataPollers Queueing";
                    var tasks = DataPollers
                        .Where(p => force || p.ShouldPoll)
                        .Select(p => p.PollGenericAsync(force))
                        .ToArray();

                    // Hop out early, run nothing else
                    if (!tasks.Any())
                    {
                        PollStatus = "DataPollers Complete (None to run)";
                        return;
                    }

                    // Await all children (this itself will be a background fire and forget if desired
                    await Task.WhenAll(tasks);
                    PollStatus = "DataPollers Complete (Awaited)";

                    // Old

                    LastPoll = DateTime.UtcNow;
                    Polled?.Invoke(this, new PollResultArgs());
                    Interlocked.Increment(ref _totalPolls);
                }
                finally
                {
                    Interlocked.Exchange(ref _isPolling, 0);
                    CurrentPollDuration.Stop();
                    LastPollDuration = CurrentPollDuration.Elapsed;
                    CurrentPollDuration = null;
                }
                PollStatus = "Poll Complete";
            }
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
