using System.Collections.Generic;
using Opserver.Data.Elastic;

namespace Opserver.Views.Elastic
{
    public class DashboardModel
    {
        public List<ElasticCluster> Clusters { get; set; }

        public string CurrentNodeName { get; set; }
        public string CurrentClusterName { get; set; }
        public string CurrentIndexName { get; set; }

        public ElasticCluster CurrentCluster { get; set; }
        public ElasticCluster.NodeInfo CurrentNode { get; set; }

        //TODO: Global settings pre-websockets
        public int Refresh { get; set; } = 10;
        public DisplayModes DisplayMode { get; set; }

        public enum DisplayModes
        {
            All,
            InterestingOnly,
            WarningsOnly
        }

        public IEnumerable<ElasticCluster.ClusterHealthInfo.IndexHealthInfo> DisplayIndexexs
        {
            get
            {
                switch (DisplayMode)
                {
                    case DisplayModes.InterestingOnly:
                        return CurrentCluster?.TroubledIndexes; // TODO: Differentiate both
                    case DisplayModes.WarningsOnly:
                        return CurrentCluster?.TroubledIndexes;
                    //case DashboardModel.DisplayModes.All:
                    default:
                return CurrentCluster?.HealthStatus.Data?.Indexes?.Values;
                }
            }
        }

        public IEnumerable<ElasticCluster.ClusterStateInfo.ShardState> DisplayShards
        {
            get
            {
                switch (DisplayMode)
                {
                    case DisplayModes.InterestingOnly:
                        return CurrentCluster?.TroubledShards; // TODO: Differentiate both
                    case DisplayModes.WarningsOnly:
                        return CurrentCluster?.TroubledShards;
                    //case DashboardModel.DisplayModes.All:
                    default:
                        return CurrentCluster?.AllShards;
                }
            }
        }

        public Views View { get; set; }

        public enum Views
        {
            AllClusters = 0,
            Cluster = 1,
            Node = 2,
            Indexes = 3,
            Shards = 4
        }
    }
}
