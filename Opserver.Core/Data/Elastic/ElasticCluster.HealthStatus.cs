using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Elastic;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<ClusterHealthStatusInfo> _healthStatus;
        public Cache<ClusterHealthStatusInfo> HealthStatus => _healthStatus ?? (_healthStatus = GetCache<ClusterHealthStatusInfo>(10));

        /// <summary>
        /// The Index info API changes in ElasticSearch 0.9, it's not really reasonable to support 
        /// data before that given that it's going away
        /// </summary>
        public static readonly Version MinIndexInfoVersion = new Version(0, 90);

        public IEnumerable<NodeIndexInfo> TroubledIndexes
        {
            get
            {
                return HealthStatus.Data?.Indices != null
                           ? HealthStatus.Data.Indices.Where(i => i.MonitorStatus != MonitorStatus.Good).OrderByWorst()
                           : Enumerable.Empty<NodeIndexInfo>();
            }
        }

        public class ClusterHealthStatusInfo : ElasticDataObject, IMonitorStatus
        {
            public string Name { get; internal set; }
            public string StringStatus { get; internal set; }
            public int TotalNodeCount { get; internal set; }
            public int DataNodeCount { get; internal set; }
            public int ActiveShards { get; internal set; }
            public int ActivePrimaryShards { get; internal set; }
            public int InitializingShards { get; internal set; }
            public int RelocatingShards { get; internal set; }
            public int UnassignedShards { get; internal set; }

            public List<NodeIndexInfo> Indices { get; internal set; }

            public MonitorStatus MonitorStatus
            {
                get
                {
                    switch (StringStatus)
                    {
                        case "green":
                            return MonitorStatus.Good;
                        case "yellow":
                            return MonitorStatus.Warning;
                        case "red":
                            return MonitorStatus.Critical;
                        default:
                            return MonitorStatus.Unknown;
                    }
                }
            }
            public string MonitorStatusReason => "Cluster is " + StringStatus;

            public override ElasticResponse RefreshFromConnection(SearchClient cli)
            {
                var rawHealth = cli.GetClusterHealth();
                if (rawHealth.HasData)
                {
                    var health = rawHealth.Data;
                    Name = health.ClusterName;
                    TotalNodeCount = health.NumberOfNodes;
                    DataNodeCount = health.NumberOfDataNodes;
                    ActiveShards = health.ActiveShards;
                    ActivePrimaryShards = health.ActivePrimaryShards;
                    InitializingShards = health.InitializingShards;
                    RelocatingShards = health.RelocatingShards;
                    UnassignedShards = health.UnassignedShards;
                    StringStatus = health.Status;

                    Indices = health.Indices.Select(i => new NodeIndexInfo
                    {
                        Name = i.Key,
                        StringStatus = i.Value.Status,
                        NumberOfShards = i.Value.NumberOfShards,
                        NumberOfReplicas = i.Value.NumberOfReplicas,
                        ActiveShards = i.Value.ActiveShards,
                        ActivePrimaryShards = i.Value.ActivePrimaryShards,
                        InitializingShards = i.Value.InitializingShards,
                        RelocatingShards = i.Value.RelocatingShards,
                        UnassignedShards = i.Value.UnassignedShards,
                        Shards = i.Value.Shards.Select(s => new NodeIndexShardInfo
                        {
                            Name = s.Key,
                            StringStatus = s.Value.Status,
                            PrimaryActive = s.Value.PrimaryActive,
                            ActiveShards = s.Value.ActiveShards,
                            InitializingShards = s.Value.InitializingShards,
                            RelocatingShards = s.Value.RelocatingShards,
                            UnassignedShards = s.Value.UnassignedShards
                        }).ToList()
                    }).OrderBy(i =>
                    {
                        int j;
                        return int.TryParse(i.Name, out j) ? j : 0;
                    }).ToList();
                }
                else
                {
                    Indices = new List<NodeIndexInfo>();
                }
                return rawHealth;
            }
        }

        public class NodeIndexInfo : IMonitorStatus
        {
            public string Name { get; internal set; }
            public int ActivePrimaryShards { get; internal set; }
            public int NumberOfShards { get; internal set; }
            public int NumberOfReplicas { get; internal set; }
            public string StringStatus { get; internal set; }
            public int ActiveShards { get; internal set; }
            public int InitializingShards { get; internal set; }
            public int RelocatingShards { get; internal set; }
            public int UnassignedShards { get; internal set; }

            public List<NodeIndexShardInfo> Shards { get; internal set; }

            public MonitorStatus MonitorStatus
            {
                get
                {
                    switch (StringStatus)
                    {
                        case "green":
                            return MonitorStatus.Good;
                        case "yellow":
                            return MonitorStatus.Warning;
                        case "red":
                            return MonitorStatus.Critical;
                        default:
                            return MonitorStatus.Unknown;
                    }
                }
            }
            public string MonitorStatusReason => StringStatus != "green" ? "Index is " + StringStatus : null;
        }

        public class NodeIndexShardInfo
        {
            public string Name { get; internal set; }
            public bool PrimaryActive { get; internal set; }
            public string StringStatus { get; internal set; }
            public int ActiveShards { get; internal set; }
            public int InitializingShards { get; internal set; }
            public int RelocatingShards { get; internal set; }
            public int UnassignedShards { get; internal set; }

            public MonitorStatus MonitorStatus
            {
                get
                {
                    switch (StringStatus)
                    {
                        case "green":
                            return MonitorStatus.Good;
                        case "yellow":
                            return MonitorStatus.Warning;
                        case "red":
                            return MonitorStatus.Critical;
                        default:
                            return MonitorStatus.Unknown;
                    }
                }
            }
        }
    }
}
