using System.Collections.Generic;
using StackExchange.Opserver.Data.Elastic;

namespace StackExchange.Opserver.Views.Elastic
{
    public class DashboardModel
    {
        public List<ElasticCluster> Clusters => ElasticCluster.AllClusters;

        public string CurrentNodeName { get; set; }
        public string CurrentClusterName { get; set; }
        public string CurrentIndexName { get; set; }

        public ElasticCluster CurrentCluster { get; set; }
        public ElasticCluster.NodeInfo CurrentNode { get; set; }


        //TODO: Global settings pre-websockets
        public int Refresh { get; set; } = 10;
        public bool WarningsOnly { get; set; }

        public Views View { get; set; }

        public enum Views
        {
            AllClusters,
            Cluster,
            Node,
            Indexes,
            Shards
        }
    }
}