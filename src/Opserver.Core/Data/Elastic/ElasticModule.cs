using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Opserver.Data.Elastic
{
    public class ElasticModule : StatusModule<ElasticSettings>
    {
        public override string Name => "Elastic";
        public override bool Enabled => Clusters.Count > 0;

        public List<ElasticCluster> Clusters { get; }

        public ElasticModule(IConfiguration config, PollingService poller) : base(config, poller)
        {
            Clusters = Settings.Clusters
                .Select(c => new ElasticCluster(this, c))
                .Where(i => i.TryAddToGlobalPollers())
                .ToList();
        }

        public override MonitorStatus MonitorStatus => Clusters.GetWorstStatus();
        public override bool IsMember(string node)
        {
            return Clusters.Any(c => c.KnownNodes.Any(sn => string.Equals(sn.Host, node, StringComparison.InvariantCultureIgnoreCase)));
        }
    }
}
