using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data
{
    public static class PollingEngine
    {
        private static readonly object _addLock = new object();

        public static HashSet<PollNode> AllPollNodes;
        private static Thread _globalPollingThread;
        private static volatile bool _shuttingDown;
        private static readonly object _pollAllLock;
        private static int _totalPollIntervals;
        private static DateTime? _lastPollAll;
        private static DateTime _startTime;

        static PollingEngine()
        {
            _pollAllLock = new object();
            AllPollNodes = new HashSet<PollNode>();
            StartPolling();
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
        public static void StartPolling()
        {
            _startTime = DateTime.UtcNow;
            if (_globalPollingThread == null)
            {
                _globalPollingThread = new Thread(MonitorPollingLoop)
                {
                    Name = "GlobalPolling",
                    Priority = ThreadPriority.Lowest,
                    IsBackground = true
                };
            }
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

        public class GlobalPollingStatus : IMonitorStatus
        {
            public MonitorStatus MonitorStatus { get; internal set; }
            public string MonitorStatusReason { get; internal set; }
            public DateTime StartTime { get; internal set; }
            public DateTime? LastPollAll { get; internal set; }
            public bool IsAlive { get; internal set; }
            public int TotalPollIntervals { get; internal set; }
            public int NodeCount { get; internal set; }
            public int TotalPollers { get; internal set; }
            public List<Tuple<Type, int>> NodeBreakdown { get; internal set; }
            public List<PollNode> Nodes { get; internal set; }
        }

        public static GlobalPollingStatus GetPollingStatus()
        {
            return new GlobalPollingStatus
                {
                    MonitorStatus = _globalPollingThread.IsAlive ? (AllPollNodes.Count > 0 ? MonitorStatus.Good : MonitorStatus.Unknown) : MonitorStatus.Critical,
                    MonitorStatusReason = _globalPollingThread.IsAlive ? (AllPollNodes.Count > 0 ? null : "No Poll Nodes") : "Global Polling Thread Dead",
                    StartTime = _startTime,
                    LastPollAll = _lastPollAll,
                    IsAlive = _globalPollingThread.IsAlive,
                    TotalPollIntervals = _totalPollIntervals,
                    NodeCount = AllPollNodes.Count,
                    TotalPollers = AllPollNodes.Sum(n => n.DataPollers.Count()),
                    NodeBreakdown = AllPollNodes.GroupBy(n => n.GetType()).Select(g => Tuple.Create(g.Key, g.Count())).ToList(),
                    Nodes = AllPollNodes.ToList()
                };
        }

        private static void MonitorPollingLoop()
        {
            while (!_shuttingDown)
            {
                try
                {
                    StartIndexLoop();
                }
                catch (ThreadAbortException e)
                {
                    if (!_shuttingDown)
                        Current.LogException("Global polling loop shutting down", e);
                }
                catch (Exception ex)
                {
                    Current.LogException(ex);
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

        private static void StartIndexLoop()
        {
            while (!_shuttingDown)
            {
                PollAll();
                Thread.Sleep(1000);
            }
        }
        
        public static void PollAll()
        {
            if (!Monitor.TryEnter(_pollAllLock, 500)) return;

            Interlocked.Increment(ref _totalPollIntervals);
            try
            {
                Parallel.ForEach(AllPollNodes, i => i.Poll());
            }
            catch (Exception e)
            {
                Current.LogException(e);
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
        /// <param name="sync">Whether to perform a synchronous poll operation (async by default)</param>
        /// <returns>Whether the poll was successful</returns>
        public static bool Poll(string nodeType, string key, Guid? cacheGuid = null, bool sync = false)
        {
            var node = AllPollNodes.FirstOrDefault(p => p.NodeType == nodeType && p.UniqueKey == key);
            if (node == null) return false;

            if (cacheGuid.HasValue)
            {
                var cache = node.DataPollers.FirstOrDefault(p => p.UniqueId == cacheGuid);
                return cache != null && cache.Poll(true) > 0;
            }
            // Polling an entire server
            node.Poll(true, sync: sync);
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
    }
}
