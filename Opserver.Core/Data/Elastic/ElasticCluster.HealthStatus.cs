using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<ClusterHealthInfo> _healthStatus;
        public Cache<ClusterHealthInfo> HealthStatus => _healthStatus ?? (_healthStatus = new Cache<ClusterHealthInfo>
        {
            CacheForSeconds = RefreshInterval,
            UpdateCache = UpdateFromElastic(nameof(HealthStatus),
                async () =>
                    (await GetAsync<ClusterHealthInfo>("_cluster/health?level=shards").ConfigureAwait(false))?.Prep())
        });

        /// <summary>
        /// The Index info API changes in ElasticSearch 0.9, it's not really reasonable to support 
        /// data before that given that it's going away
        /// </summary>
        public static readonly Version MinIndexInfoVersion = new Version(0, 90);

        public IEnumerable<ClusterHealthInfo.IndexHealthInfo> TroubledIndexes
        {
            get
            {
                return HealthStatus.Data?.Indexes != null
                    ? HealthStatus.Data.Indexes.Values.Where(i => i.MonitorStatus != MonitorStatus.Good).OrderByWorst()
                    : Enumerable.Empty<ClusterHealthInfo.IndexHealthInfo>();
            }
        }

        public class ClusterHealthInfo : IMonitorStatus
        {
            public MonitorStatus MonitorStatus => ColorToStatus(Status);
            public string MonitorStatusReason => "Cluster is " + Status;

            public ClusterHealthInfo Prep()
            {
                if (Indexes != null)
                {
                    foreach (var i in Indexes)
                    {
                        i.Value.Name = i.Key;
                        if (i.Value.Shards != null)
                        {
                            foreach (var s in i.Value.Shards)
                            {
                                s.Value.Name = s.Key;
                            }
                        }
                    }
                }
                return this;
            }

            [DataMember(Name = "cluster_name")]
            public string Name { get; internal set; }
            [DataMember(Name = "status")]
            public string Status { get; internal set; }
            [DataMember(Name = "timed_out")]
            public bool TimedOut { get; internal set; }

            [DataMember(Name = "number_of_nodes")]
            public int TotalNodeCount { get; internal set; }
            [DataMember(Name = "number_of_data_nodes")]
            public int DataNodeCount { get; internal set; }

            [DataMember(Name = "active_primary_shards")]
            public int ActivePrimaryShards { get; internal set; }
            [DataMember(Name = "active_shards")]
            public int ActiveShards { get; internal set; }
            [DataMember(Name = "relocating_shards")]
            public int RelocatingShards { get; internal set; }
            [DataMember(Name = "initializing_shards")]
            public int InitializingShards { get; internal set; }
            [DataMember(Name = "unassigned_shards")]
            public int UnassignedShards { get; internal set; }

            [DataMember(Name = "indices")]
            public Dictionary<string, IndexHealthInfo> Indexes { get; set; }

            public class IndexHealthInfo : IMonitorStatus
            {
                public MonitorStatus MonitorStatus => ColorToStatus(Status);
                public string MonitorStatusReason => "Index is " + Status;

                // Set by dictionary key on fetch
                public string Name { get; internal set; }

                [DataMember(Name = "status")]
                public string Status { get; set; }

                [DataMember(Name = "number_of_shards")]
                public int NumberOfShards { get; set; }
                [DataMember(Name = "number_of_replicas")]
                public int NumberOfReplicas { get; set; }

                [DataMember(Name = "active_primary_shards")]
                public int ActivePrimaryShards { get; set; }
                [DataMember(Name = "active_shards")]
                public int ActiveShards { get; set; }
                [DataMember(Name = "relocating_shards")]
                public int RelocatingShards { get; set; }
                [DataMember(Name = "initializing_shards")]
                public int InitializingShards { get; set; }
                [DataMember(Name = "unassigned_shards")]
                public int UnassignedShards { get; set; }

                [DataMember(Name = "shards")]
                public Dictionary<string, ShardHealthInfo> Shards { get; set; }
            }

            public class ShardHealthInfo
            {
                // Set by dictionary key on fetch
                public string Name { get; internal set; }

                [DataMember(Name = "status")]
                public string Status { get; set; }
                [DataMember(Name = "primary_active")]
                public bool PrimaryActive { get; set; }
                [DataMember(Name = "active_shards")]
                public int ActiveShards { get; set; }
                [DataMember(Name = "relocating_shards")]
                public int RelocatingShards { get; set; }
                [DataMember(Name = "initializing_shards")]
                public int InitializingShards { get; set; }
                [DataMember(Name = "unassigned_shards")]
                public int UnassignedShards { get; set; }
            }
        }
    }
}
