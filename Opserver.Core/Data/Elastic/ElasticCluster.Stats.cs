using System.Collections.Generic;
using StackExchange.Elastic;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<ClusterStatsInfo> _stats;
        public Cache<ClusterStatsInfo> Stats => _stats ?? (_stats = new Cache<ClusterStatsInfo>
        {
            CacheForSeconds = RefreshInterval,
            UpdateCache = UpdateFromElastic(nameof(Stats), async cli =>
            {
                var health = (await cli.GetIndexStatsAsync().ConfigureAwait(false)).Data;
                return new ClusterStatsInfo
                {
                    GlobalStats = health?.All ?? new IndexStats(),
                    Shards = health?.Shards ?? new ShardCountStats(),
                    Indices = health?.Indices ?? new Dictionary<string, IndexStats>()
                };
            })
        });

        public class ClusterStatsInfo
        {
            public IndexStats GlobalStats { get; internal set; }
            public ShardCountStats Shards { get; internal set; }
            public Dictionary<string, IndexStats> Indices { get; internal set; }
        }
    }
}
