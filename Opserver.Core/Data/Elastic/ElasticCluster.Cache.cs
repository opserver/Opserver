using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        public static List<ElasticCluster> AllClusters =>
            _elasticClusters ?? (_elasticClusters = LoadElasticClusters());

        private static readonly object _loadLock = new object();
        private static List<ElasticCluster> _elasticClusters;
        private static List<ElasticCluster> LoadElasticClusters()
        {
            lock (_loadLock)
            {
                if (_elasticClusters != null) return _elasticClusters;
                return Current.Settings.Elastic.Enabled
                           ? Current.Settings.Elastic.Clusters
                                    .Select(c => new ElasticCluster(c))
                                    .Where(i => i.TryAddToGlobalPollers())
                                    .ToList()
                           : new List<ElasticCluster>();
            }
        }

        public static bool IsElasticServer(string node)
        {
            return AllClusters.Any(c => c.SettingsNodes.Any(sn => string.Equals(sn.Host, node, StringComparison.InvariantCultureIgnoreCase)));
        }
    }
}
