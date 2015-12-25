using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Elastic;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<ClusterStatsInfo> _stats;
        public Cache<ClusterStatsInfo> Stats =>
            _stats ?? (_stats = GetCache<ClusterStatsInfo>(Settings.RefreshIntervalSeconds));

        public class ClusterStatsInfo : ElasticDataObject
        {
            public IndexStats GlobalStats { get; internal set; }
            public ShardCountStats Shards { get; internal set; }
            public Dictionary<string, IndexStats> Indices { get; internal set; }
            
            public override async Task<ElasticResponse> RefreshFromConnectionAsync(SearchClient cli)
            {
                var health = await cli.GetIndexStatsAsync();
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
        }
    }
}
