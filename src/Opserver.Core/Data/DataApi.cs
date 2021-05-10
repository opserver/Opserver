using System;
using System.Collections.Generic;
using System.Linq;

namespace Opserver.Data
{
    public static class DataApi
    {
        public static IEnumerable<NodeData> GetType(PollingService poller, string type, bool includeData = false) =>
            poller.GetNodes(type).Select(n => new NodeData(n, includeData));

        public static NodeData GetNode(PollNode node, bool includeData = false) => new NodeData(node, includeData);

        public static CacheData GetCache(Cache cache, bool includeData = false) => new CacheData(cache, includeData);

        public class NodeData
        {
            public string Name { get; }
            public string Type { get; }
            public DateTime? LastPolled { get; }
            public double LastPollDurationMS { get; }
            public IEnumerable<object> Caches { get; }

            public NodeData(PollNode node, bool includeData = false)
            {
                Name = node.UniqueKey;
                Type = node.NodeType;
                LastPolled = node.LastPoll;
                LastPollDurationMS = node.LastPollDuration.TotalMilliseconds;
                Caches = node.DataPollers.Select(c => new CacheData(c, includeData));
            }
        }

        public class CacheData
        {
            public string Name { get; }
            public DateTime? LastPolled { get; }
            public DateTime? LastSuccess { get; }
            public double? LastPollDurationMs { get; }
            public string LastPollError { get; }
            public bool HasData { get; }
            public object Data { get; }

            public CacheData(Cache cache, bool includeData = false)
            {
                Name = cache.ParentMemberName;
                LastPolled = cache.LastPoll;
                LastSuccess = cache.LastSuccess;
                LastPollDurationMs = cache.LastPollDuration?.TotalMilliseconds;
                LastPollError = cache.ErrorMessage.HasValue() ? cache.ErrorMessage : null;
                HasData = cache.ContainsData;
                Data = includeData ? cache.InnerCache : null;
            }
        }
    }
}
