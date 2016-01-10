using System.Collections.Generic;
using StackExchange.Opserver.Data.Elastic;

namespace StackExchange.Opserver.Views.Elastic
{
    public class DashboardModel
    {
        public List<ElasticCluster> Clusters { get; set; }
        public CurrentData Current { get; set; }
        public bool WarningsOnly { get; set; }
        public Popups Popup { get; set; }
        
        public bool Refresh { get; set; }
        public enum Views
        {
            Cluster,
            Node,
            Indices,
            Shards
        }
        public Views View { get; set; }

        public enum Popups
        {
            None,
            Indices
        }

        public class CurrentData
        {
            public string NodeName { get; set; }
            public string ClusterName { get; set; }
            public string IndexName { get; set; }
            public ElasticCluster Cluster { get; set; }
            public ElasticCluster.NodeInfo Node { get; set; }

            public bool WarningsOnly { get; set; }
        }
    }
}