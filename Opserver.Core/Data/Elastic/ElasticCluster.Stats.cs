using System.Collections.Generic;
using Nest;

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
            public GlobalStats GlobalStats { get; internal set; }
            public ShardsMetaData ShardsMetaData { get; internal set; }
            public Dictionary<string, Stats> Indices { get; internal set; }

            public override IResponse RefreshFromConnection(ElasticClient cli)
            {
                var health = cli.Stats();
                if (health.IsValid && health.OK)
                {
                    GlobalStats = health.Stats;
                    ShardsMetaData = health.Shards;
                    Indices = health.Indices;
                }
                return health;
            }

            public Stats GetIndexStats(string index)
            {
                Stats stats;
                // Elastic 0.90 changes behavior here, temporarily coping with it until 0.20 is pretty rare
                var dict = GetIndexStats();
                if (dict == null) return null;
                dict.TryGetValue(index, out stats);
                return stats;
            }

            public Dictionary<string, Stats> GetIndexStats()
            {
                return Indices ?? new Dictionary<string, Stats>();
            }
        }
    }
}
