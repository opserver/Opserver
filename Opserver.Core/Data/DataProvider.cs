using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StackExchange.Opserver.Data.SQL;

namespace StackExchange.Opserver.Data
{
    public class DataProvider
    {
        private static readonly JsonSerializerSettings _serializationSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            Formatting = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            ContractResolver = new ExclusionResolver()
        };

        public class ExclusionResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);
                if (typeof(ISQLVersioned).IsAssignableFrom(property.DeclaringType) && property.PropertyName == nameof(ISQLVersioned.MinVersion))
                {
                    property.ShouldSerialize = i => false;
                }
                return property;
            }
        }
        
        public static string GetNodeJSON(string type, string key = null, string property = null, bool includeCaches = false)
        {
            if (key.IsNullOrEmpty())
            {
                var nodesToSerialize = PollingEngine.GetNodes(type).Select(n => NodeData.FromNode(n));
                return JsonConvert.SerializeObject(nodesToSerialize, _serializationSettings);
            }

            var node = PollingEngine.GetNode(type, key);
            if (node == null)
                throw new DataPullException("No {0} node found with key: {1}", type, key);

            // Node level
            if (property.IsNullOrEmpty())
            {
                var nodeToSerialize = NodeData.FromNode(node, includeCaches);
                return JsonConvert.SerializeObject(nodeToSerialize, _serializationSettings);
            }

            var cache = node.GetCache(property);
            if (cache == null)
                throw new DataPullException("{0} node was found, but no cache for property: {1}", node.NodeType, property);

            var cacheToSerialize = CacheData.FromCache(cache, includeCaches);
            return JsonConvert.SerializeObject(cacheToSerialize, _serializationSettings);
        }

        public class NodeList
        {
            public List<NodeData> Nodes;
        }

        public class NodeData
        {
            public string Name;
            public string Type;
            public DateTime? LastPolled;
            public double LastPollDurationMS;
            public IEnumerable<object> Caches;

            public static NodeData FromNode(PollNode node, bool includeCache = false)
            {
                return new NodeData
                    {
                        Name = node.UniqueKey,
                        Type = node.NodeType,
                        LastPolled = node.LastPoll,
                        LastPollDurationMS = node.LastPollDuration.TotalMilliseconds,
                        Caches = node.DataPollers.Select(c => CacheData.FromCache(c, includeCache))
                    };
            }
        }

        public class CacheData
        {
            public string Name;
            public DateTime? LastPolled;
            public DateTime? LastSuccess;
            public double? LastPollDurationMs;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string LastPollError;
            public bool HasData;
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public object Data;

            public static CacheData FromCache(Cache cache, bool includeData = false)
            {
                return new CacheData
                {
                    Name = cache.ParentMemberName,
                    LastPolled = cache.LastPoll,
                    LastSuccess = cache.LastSuccess,
                    LastPollDurationMs = cache.LastPollDuration?.TotalMilliseconds,
                    LastPollError = cache.ErrorMessage.HasValue() ? cache.ErrorMessage : null,
                    HasData = cache.HasData(),
                    Data = includeData ? cache.InnerCache : null
                };
            }
        }
    }

    public class DataPullException : Exception
    {
        public DataPullException(string format, params object[] args) : base(string.Format(format, args)) { }
    }
}
