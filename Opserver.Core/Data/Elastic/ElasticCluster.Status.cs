using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Elastic;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<ClusterStatusInfo> _status;
        public Cache<ClusterStatusInfo> Status =>
            _status ?? (_status = GetCache<ClusterStatusInfo>(Settings.RefreshIntervalSeconds));

        public IEnumerable<ShardState> TroubledShards =>
            ShardStates.Where(s => s.State != "STARTED");

        public IEnumerable<ShardState> ShardStates =>
            Status.Data?.RoutingNodes?.Nodes.Values.SelectMany(i => i)
                .Union(Status.Data.RoutingNodes.Unassigned) ?? Enumerable.Empty<ShardState>();

        public class ClusterStatusInfo : ElasticDataObject, IMonitorStatus
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

            public RoutingNodesState RoutingNodes { get; private set; }
            public Dictionary<string, RoutingTableState.IndexShardsState> RoutingIndices { get; private set; }
            public string MasterNode { get; private set; }
            public string ClusterName { get; private set; }
            public Dictionary<string, NodeState> Nodes { get; private set; }

            public override async Task<ElasticResponse> RefreshFromConnectionAsync(SearchClient cli)
            {
                var rawState = await cli.GetClusterStateAsync().ConfigureAwait(false);
                if (rawState.HasData)
                {
                    var state = rawState.Data;
                    ClusterName = state.ClusterName;
                    MasterNode = state.MasterNode;
                    Nodes = state.Nodes;
                    RoutingNodes = state.RoutingNodes;
                    if (state.RoutingTable != null)
                        RoutingIndices = state.RoutingTable.Indices;
                }

                return rawState;
            }
        }
    }
}
