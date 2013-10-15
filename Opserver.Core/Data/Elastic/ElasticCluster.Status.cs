using System.Collections.Generic;
using System.Linq;
using Nest;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<ClusterStatusInfo> _status;
        public Cache<ClusterStatusInfo> Status
        {
            get { return _status ?? (_status = GetCache<ClusterStatusInfo>(10)); }
        }

        public IEnumerable<RoutingShard> TroubledShards
        {
            get { return ShardStates.Where(s => s.State != "STARTED"); }
        }

        public IEnumerable<RoutingShard> ShardStates
        {
            get
            {
                return Status.Data != null && Status.Data.RoutingNodes != null
                           ? Status.Data.RoutingNodes.Nodes.Values.SelectMany(i => i)
                                          .Union(Status.Data.RoutingNodes.Unassigned)
                           : Enumerable.Empty<RoutingShard>();
            }
        }

        public class ClusterStatusInfo : ElasticDataObject, IMonitorStatus
        {
            private MonitorStatus? _monitorStatus;
            public MonitorStatus MonitorStatus
            {
                get
                {
                    if (!_monitorStatus.HasValue)
                    {
                        _monitorStatus = RoutingNodes != null
                                             ? RoutingNodes.Nodes.Values.SelectMany(n => n)
                                                           .Union(RoutingNodes.Unassigned)
                                                           .Select(i => i.GetMonitorStatus())
                                                           .GetWorstStatus()
                                             : MonitorStatus.Unknown;
                    }
                    return _monitorStatus.Value;
                }
            }
            // TODO: Implement
            public string MonitorStatusReason { get { return null; } }

            public RoutingNodesState RoutingNodes { get; private set; }
            public Dictionary<string, IndexRoutingTable> RoutingIndices { get; private set; }
            public string MasterNode { get; private set; }
            public string ClusterName { get; private set; }
            public Dictionary<string, NodeState> Nodes { get; private set; }

            public override IResponse RefreshFromConnection(ElasticClient cli)
            {
                var state = cli.ClusterState(ClusterStateInfo.ExcludeMetadata);
                ClusterName = state.ClusterName;
                MasterNode = state.MasterNode;
                Nodes = state.Nodes;
                RoutingNodes = state.RoutingNodes;
                if (state.RoutingTable != null)
                    RoutingIndices = state.RoutingTable.Indices;

                return state;
            }
        }
    }
}
