using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.Elastic
{
    public class ElasticModule : StatusModule
    {
        public static bool Enabled => Clusters.Count > 0;
        public static List<ElasticCluster> Clusters { get; }
        
        static ElasticModule()
        {
            Clusters = Current.Settings.Elastic.Clusters
                .Select(c => new ElasticCluster(c))
                .Where(i => i.TryAddToGlobalPollers())
                .ToList();
        }

        public override bool IsMember(string node)
        {
            return Clusters.Any(c => c.KnownNodes.Any(sn => string.Equals(sn.Host, node, StringComparison.InvariantCultureIgnoreCase)));
        }
    }
}
