using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Opserver.Data
{
    public partial class PollingService
    {
        private Thread _globalPollingThread;
        private readonly object _addLock = new object();
        private readonly object _pollAllLock = new object();
        public readonly HashSet<PollNode> AllPollNodes = new HashSet<PollNode>();
        private readonly Action<Func<Task>> _taskRunner = t => Task.Run(t);

        private long _totalPollIntervals;
        private DateTime? _lastPollAll;
        internal static long _globalActivePolls;

        /// <summary>
        /// Adds a node to the global polling list ONLY IF IT IS NEW
        /// If a node with the same unique key was already added, it will not be added again
        /// </summary>
        /// <param name="node">The node to add to the global polling list</param>
        /// <returns>Whether the node was added</returns>
        public bool TryAdd(PollNode node)
        {
            lock (_addLock)
            {
                var success = AllPollNodes.Add(node);
                if (success)
                {
                    if (node is IIssuesProvider iProvider)
                    {
                        IssueProviders.Add(iProvider);
                    }
                }
                return success;
            }
        }

        public bool TryRemove(PollNode node)
        {
            if (node == null || !node.AddedToGlobalPollers) return false;
            lock (_addLock)
            {
                return AllPollNodes.Remove(node);
            }
        }

        public List<PollNode> GetNodes(string type)
        {
            return AllPollNodes.Where(pn => string.Equals(pn.NodeType, type, StringComparison.InvariantCultureIgnoreCase)).ToList();
        }

        public PollNode GetNode(string type, string key)
        {
            return AllPollNodes.FirstOrDefault(pn => string.Equals(pn.NodeType, type, StringComparison.InvariantCultureIgnoreCase) && pn.UniqueKey == key);
        }

        public Cache GetCache(Guid id)
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

        private void MonitorPollingLoop()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    StartPollLoop();
                }
                catch (ThreadAbortException e)
                {
                    if (!_cancellationToken.IsCancellationRequested)
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

        private void StartPollLoop()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                PollAllAndForget();
                Thread.Sleep(1000);
            }
        }

        public void PollAllAndForget()
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
        public async Task<bool> PollAsync(string nodeType, string key, Guid? cacheGuid = null)
        {
            if (nodeType == Cache.TimedCacheKey)
            {
                MemCache.Remove(key);
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
    }
}
