using System.Collections.Generic;
using StackExchange.Elastic;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<ClusterStatsInfo> _stats;
        public Cache<ClusterStatsInfo> Stats
        {
            get { return _stats ?? (_stats = GetCache<ClusterStatsInfo>(10)); }
        }

        public class ClusterStatsInfo : ElasticDataObject
        {
            public IndexStats GlobalStats { get; internal set; }
            public ShardCountStats Shards { get; internal set; }
            public Dictionary<string, IndexStats> Indices { get; internal set; }
            
            public override ElasticResponse RefreshFromConnection(SearchClient cli)
            {
                var health = cli.GetIndexStats();
                if (health.HasData)
                {
                    GlobalStats = health.Data.All;
                    Shards = health.Data.Shards;
                    Indices = health.Data.Indices;
                }
                else
                {
                    GlobalStats = new IndexStats();
                    Shards = new ShardCountStats();
                    Indices = new Dictionary<string, IndexStats>();
                }
                return health;
            }

            public Dictionary<string, IndexStats> GetIndexStats()
            {
                return Indices ?? new Dictionary<string, IndexStats>();
            }
        }
    }
}
