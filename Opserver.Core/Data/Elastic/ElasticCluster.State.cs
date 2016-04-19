using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<ClusterStateInfo> _state;

        public Cache<ClusterStateInfo> State => _state ?? (_state = new Cache<ClusterStateInfo>
        {
            CacheForSeconds = RefreshInterval,
            UpdateCache = UpdateFromElastic(nameof(State), async () =>
                await GetAsync<ClusterStateInfo>("_cluster/state/version,master_node,nodes,routing_table,routing_nodes/").ConfigureAwait(false))
        });

        public NodeInfo MasterNode => Nodes.Data?.Nodes?.FirstOrDefault(n => State?.Data?.MasterNode == n.GUID);

        public IEnumerable<ClusterStateInfo.ShardState> TroubledShards => AllShards.Where(s => s.State != "STARTED");
        public List<ClusterStateInfo.ShardState> AllShards => State.Data?.AllShards ?? new List<ClusterStateInfo.ShardState>();
        
        public class ClusterStateInfo : IMonitorStatus
        {
            private MonitorStatus? _monitorStatus;
            public MonitorStatus MonitorStatus
            {
                get
                {
                    if (!_monitorStatus.HasValue)
                    {
                        _monitorStatus = RoutingNodes?.Nodes.Values.SelectMany(n => n)
                            .Union(RoutingNodes.Unassigned)
                            .GetWorstStatus() ?? MonitorStatus.Unknown;
                    }
                    return _monitorStatus.Value;
                }
            }
            // TODO: Implement
            public string MonitorStatusReason => null;

            private List<ShardState> _allShards;
            public List<ShardState> AllShards =>
                _allShards ??
                (_allShards = RoutingNodes?.Nodes.Values
                    .SelectMany(i => i)
                    .Union(RoutingNodes.Unassigned)
                    .ToList()
                              ?? new List<ShardState>());

            [DataMember(Name = "cluster_name")]
            public string Name { get; internal set; }
            [DataMember(Name = "master_node")]
            public string MasterNode { get; internal set; }

            [DataMember(Name = "nodes")]
            public Dictionary<string, NodeState> Nodes { get; internal set; }

            [DataMember(Name = "routing_table")]
            public RoutingTableState RoutingTable { get; internal set; }

            [DataMember(Name = "routing_nodes")]
            public RoutingNodesState RoutingNodes { get; internal set; }

            public class BlockState
            {
                [DataMember(Name = "read_only")]
                public bool ReadOnly { get; set; }
            }

            public class NodeState
            {
                [DataMember(Name = "name")]
                public string Name { get; internal set; }

                [DataMember(Name = "transport_address")]
                public string TransportAddress { get; internal set; }

                [DataMember(Name = "attributes")]
                public Dictionary<string, string> Attributes { get; internal set; }
            }
            public class RoutingTableState
            {
                [DataMember(Name = "indices")]
                public Dictionary<string, IndexShardsState> Indexes { get; internal set; }
            }

            public class RoutingNodesState
            {
                [DataMember(Name = "unassigned")]
                public List<ShardState> Unassigned { get; internal set; }

                [DataMember(Name = "nodes")]
                public Dictionary<string, List<ShardState>> Nodes { get; internal set; }
            }

            public class IndexShardsState
            {
                [DataMember(Name = "shards")]
                public Dictionary<string, List<ShardState>> Shards { get; internal set; }
            }

            public class ShardState : IMonitorStatus
            {
                public MonitorStatus MonitorStatus
                {
                    get
                    {
                        switch (State)
                        {
                            case ShardStates.Unassigned:
                                return MonitorStatus.Critical;
                            case ShardStates.Initializing:
                                return MonitorStatus.Warning;
                            case ShardStates.Started:
                                return MonitorStatus.Good;
                            case ShardStates.Relocating:
                                return MonitorStatus.Maintenance;
                            default:
                                return MonitorStatus.Unknown;
                        }
                    }
                }

                public string MonitorStatusReason => StateDescription;

                public string StateDescription
                {
                    get
                    {
                        switch (State)
                        {
                            case ShardStates.Unassigned:
                                return "The shard is not assigned to any node";
                            case ShardStates.Initializing:
                                return "The shard is initializing (probably recovering from either a peer shard or gateway)";
                            case ShardStates.Started:
                                return "The shard is started";
                            case ShardStates.Relocating:
                                return "The shard is in the process being relocated";
                            default:
                                return "Unknown";
                        }
                    }
                }
                public string PrettyState
                {
                    get
                    {
                        switch (State)
                        {
                            case ShardStates.Unassigned:
                                return "Unassigned";
                            case ShardStates.Initializing:
                                return "Initializing";
                            case ShardStates.Started:
                                return "Started";
                            case ShardStates.Relocating:
                                return "Relocating";
                            default:
                                return "Unknown";
                        }
                    }
                }

                [DataMember(Name = "state")]
                public string State { get; internal set; }
                [DataMember(Name = "primary")]
                public bool Primary { get; internal set; }
                [DataMember(Name = "node")]
                public string Node { get; internal set; }
                [DataMember(Name = "relocating_node")]
                public string RelocatingNode { get; internal set; }
                [DataMember(Name = "shard")]
                public int Shard { get; internal set; }
                [DataMember(Name = "index")]
                public string Index { get; internal set; }
            }
        }
    }
}
