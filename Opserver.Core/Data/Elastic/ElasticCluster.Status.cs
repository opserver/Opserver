using System.Collections.Generic;
using System.Linq;
using StackExchange.Elastic;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<ClusterStatusInfo> _status;
        public Cache<ClusterStatusInfo> Status => _status ?? (_status = new Cache<ClusterStatusInfo>
        {
            CacheForSeconds = RefreshInterval,
            UpdateCache = UpdateFromElastic(nameof(Status), async cli =>
            {
                var state = (await cli.GetClusterStateAsync().ConfigureAwait(false)).Data;
                return new ClusterStatusInfo
                {
                    ClusterName = state?.ClusterName,
                    MasterNode = state?.MasterNode,
                    Nodes = state?.Nodes,
                    RoutingNodes = state?.RoutingNodes,
                    RoutingIndices = state?.RoutingTable?.Indices
                };
            })
        });

        public IEnumerable<ShardState> TroubledShards =>
            ShardStates.Where(s => s.State != "STARTED");

        public IEnumerable<ShardState> ShardStates =>
            Status.Data?.RoutingNodes?.Nodes.Values.SelectMany(i => i)
                .Union(Status.Data.RoutingNodes.Unassigned) ?? Enumerable.Empty<ShardState>();

        public class ClusterStatusInfo : IMonitorStatus
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
                            .Select(i => i.GetMonitorStatus())
                            .GetWorstStatus() ?? MonitorStatus.Unknown;
                    }
                    return _monitorStatus.Value;
                }
            }
            // TODO: Implement
            public string MonitorStatusReason => null;

            public RoutingNodesState RoutingNodes { get; internal set; }
            public Dictionary<string, RoutingTableState.IndexShardsState> RoutingIndices { get; internal set; }
            public string MasterNode { get; internal set; }
            public string ClusterName { get; internal set; }
            public Dictionary<string, NodeState> Nodes { get; internal set; }
        }
    }
}
