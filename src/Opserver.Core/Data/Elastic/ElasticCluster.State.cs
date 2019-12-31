using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<ClusterStateInfo> _state;

        public Cache<ClusterStateInfo> State => _state ??= GetElasticCache(
            () => GetAsync<ClusterStateInfo>("_cluster/state/version,master_node,nodes,routing_table,routing_nodes/")
        );

        public NodeInfo MasterNode => Nodes.Data?.Nodes?.FirstOrDefault(n => State?.Data?.MasterNode == n.GUID);

        public IEnumerable<ClusterStateInfo.ShardState> TroubledShards => AllShards.Where(s => s.State != "STARTED");
        public List<ClusterStateInfo.ShardState> AllShards => State.Data?.AllShards ?? new List<ClusterStateInfo.ShardState>();

        public class ClusterStateInfo : IMonitorStatus
        {
            private MonitorStatus? _monitorStatus;
            public MonitorStatus MonitorStatus =>
                _monitorStatus ??= RoutingNodes?.Nodes.Values.SelectMany(n => n)
                    .Union(RoutingNodes.Unassigned)
                    .GetWorstStatus() ?? MonitorStatus.Unknown;
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
                public MonitorStatus MonitorStatus =>
                    State switch
                    {
                        ShardStates.Unassigned => MonitorStatus.Critical,
                        ShardStates.Initializing => MonitorStatus.Warning,
                        ShardStates.Started => MonitorStatus.Good,
                        ShardStates.Relocating => MonitorStatus.Maintenance,
                        _ => MonitorStatus.Unknown,
                    };

                public string MonitorStatusReason => StateDescription;

                public string StateDescription =>
                    State switch
                    {
                        ShardStates.Unassigned => "The shard is not assigned to any node",
                        ShardStates.Initializing => "The shard is initializing (probably recovering from either a peer shard or gateway)",
                        ShardStates.Started => "The shard is started",
                        ShardStates.Relocating => "The shard is in the process being relocated",
                        _ => "Unknown",
                    };

                public string PrettyState =>
                    State switch
                    {
                        ShardStates.Unassigned => "Unassigned",
                        ShardStates.Initializing => "Initializing",
                        ShardStates.Started => "Started",
                        ShardStates.Relocating => "Relocating",
                        _ => "Unknown",
                    };

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
