using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        public static List<ElasticCluster> AllClusters { get; } =
            Current.Settings?.Elastic?.Enabled ?? false
                ? Current.Settings.Elastic.Clusters
                    .Select(c => new ElasticCluster(c))
                    .Where(i => i.TryAddToGlobalPollers())
                    .ToList()
                : new List<ElasticCluster>();

        public static bool IsElasticServer(string node)
        {
            return AllClusters.Any(c => c.KnownNodes.Any(sn => string.Equals(sn.Host, node, StringComparison.InvariantCultureIgnoreCase)));
        }
    }
}
