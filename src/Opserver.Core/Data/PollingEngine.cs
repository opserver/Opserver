using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Opserver.Helpers;

namespace StackExchange.Opserver.Data
{
    public static class PollingEngine
    {
        private static readonly object _addLock = new object();
        private static readonly object _pollAllLock = new object();
        public static readonly HashSet<PollNode> AllPollNodes = new HashSet<PollNode>();

        private static CancellationToken _cancellationToken;
        private static Thread _globalPollingThread;
        private static volatile bool _shuttingDown;
        private static long _totalPollIntervals;
        internal static long _activePolls;
        private static DateTime? _lastPollAll;
        private static DateTime _startTime;
        private static Action<Func<Task>> _taskRunner = t => Task.Run(t);

        public static void Configure(Action<Func<Task>> taskRunner)
        {
            _taskRunner = taskRunner;
        }

        /// <summary>
        /// Adds a node to the global polling list ONLY IF IT IS NEW
        /// If a node with the same unique key was already added, it will not be added again
        /// </summary>
        /// <param name="node">The node to add to the global polling list</param>
        /// <returns>Whether the node was added</returns>
        public static bool TryAdd(PollNode node)
        {
            lock (_addLock)
            {
                return AllPollNodes.Add(node);
            }
        }

        public static bool TryRemove(PollNode node)
        {
            if (node == null || !node.AddedToGlobalPollers) return false;
            lock (_addLock)
            {
                return AllPollNodes.Remove(node);
            }
        }

        /// <summary>
        /// What do you think it does?
        /// </summary>
        public static void StartPolling(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _startTime = DateTime.UtcNow;
            _globalPollingThread = _globalPollingThread ?? new Thread(MonitorPollingLoop)
                {
                    Name = "GlobalPolling",
                    Priority = ThreadPriority.Lowest,
                    IsBackground = true
                };
            if (!_globalPollingThread.IsAlive)
                _globalPollingThread.Start();
        }

        /// <summary>
        /// Performs a soft shutdown after the current poll finishes
        /// </summary>
        public static void StopPolling()
        {
            _shuttingDown = true;
        }

        public static GlobalPollingStatus GetPollingStatus() => new GlobalPollingStatus
        {
            MonitorStatus = _globalPollingThread.IsAlive ? (AllPollNodes.Count > 0 ? MonitorStatus.Good : MonitorStatus.Unknown) : MonitorStatus.Critical,
            MonitorStatusReason = _globalPollingThread.IsAlive ? (AllPollNodes.Count > 0 ? null : "No Poll Nodes") : "Global Polling Thread Dead",
            StartTime = _startTime,
            LastPollAll = _lastPollAll,
            IsAlive = _globalPollingThread.IsAlive,
            TotalPollIntervals = _totalPollIntervals,
            ActivePolls = _activePolls,
            NodeCount = AllPollNodes.Count,
            TotalPollers = AllPollNodes.Sum(n => n.DataPollers.Count()),
            NodeBreakdown = AllPollNodes.GroupBy(n => n.GetType()).Select(g => Tuple.Create(g.Key, g.Count())).ToList(),
            Nodes = AllPollNodes.ToList()
        };

        private static void MonitorPollingLoop()
        {
            while (!_shuttingDown && !_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    StartPollLoop();
                }
                catch (ThreadAbortException e)
                {
                    if (!_shuttingDown)
                        new Exception("Global polling loop shutting down", e).Log();
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
                try
                {
                    Thread.Sleep(2000);
                }
                catch (ThreadAbortException)
                {
                    // application is cycling, AND THAT'S OKAY
                }
            }
        }

        private static void StartPollLoop()
        {
            while (!_shuttingDown)
            {
                PollAllAndForget();
                Thread.Sleep(1000);
            }
        }

        public static void PollAllAndForget()
        {
            if (!Monitor.TryEnter(_pollAllLock, 500)) return;

            Interlocked.Increment(ref _totalPollIntervals);
            try
            {
                foreach (var n in AllPollNodes)
                {
                    if (n.IsPolling || !n.NeedsPoll)
                    {
                        continue;
                    }
                    _taskRunner?.Invoke(() => n.PollAsync());
                }
            }
            catch (Exception e)
            {
                e.Log();
            }
            finally
            {
                Monitor.Exit(_pollAllLock);
            }
            _lastPollAll = DateTime.UtcNow;
        }

        /// <summary>
        /// Polls all caches on a specific PollNode
        /// </summary>
        /// <param name="nodeType">Type of node to poll</param>
        /// <param name="key">Unique key of the node to poll</param>
        /// <param name="cacheGuid">If included, the specific cache to poll</param>
        /// <returns>Whether the poll was successful</returns>
        public static async Task<bool> PollAsync(string nodeType, string key, Guid? cacheGuid = null)
        {
            if (nodeType == Cache.TimedCacheKey)
            {
                Cache.Purge(key);
                return true;
            }

            var node = AllPollNodes.FirstOrDefault(p => p.NodeType == nodeType && p.UniqueKey == key);
            if (node == null) return false;

            if (cacheGuid.HasValue)
            {
                var cache = node.DataPollers.FirstOrDefault(p => p.UniqueId == cacheGuid);
                if (cache != null)
                {
                    await cache.PollGenericAsync(true);
                }
                return cache?.LastPollSuccessful ?? false;
            }
            // Polling an entire server
            await node.PollAsync(true);
            return true;
        }

        public static List<PollNode> GetNodes(string type)
        {
            return AllPollNodes.Where(pn => string.Equals(pn.NodeType, type, StringComparison.InvariantCultureIgnoreCase)).ToList();
        }

        public static PollNode GetNode(string type, string key)
        {
            return AllPollNodes.FirstOrDefault(pn => string.Equals(pn.NodeType, type, StringComparison.InvariantCultureIgnoreCase) && pn.UniqueKey == key);
        }

        public static Cache GetCache(Guid id)
        {
            foreach (var pn in AllPollNodes)
            {
                foreach (var c in pn.DataPollers)
                {
                    if (c.UniqueId == id) return c;
                }
            }
            return null;
        }

        public static ThreadStats GetThreadStats() => new ThreadStats();
    }
}
