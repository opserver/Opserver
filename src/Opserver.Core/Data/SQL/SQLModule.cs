using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Opserver.Data.SQL
{
    public class SQLModule : StatusModule<SQLSettings>
    {
        public override string Name => "SQL";
        public override bool Enabled => AllInstances.Count > 0;
        /// <summary>
        /// SQL Instances not in clusters
        /// </summary>
        public List<SQLInstance> StandaloneInstances { get; }
        public List<SQLCluster> Clusters { get; }
        /// <summary>
        /// All SQL Instances, including those in clusters
        /// </summary>
        public List<SQLInstance> AllInstances { get; }

        public SQLModule(IOptions<SQLSettings> settings, PollingService poller) : base(settings, poller)
        {
            StandaloneInstances = settings.Value.Instances
                .Select(i => new SQLInstance(this, i))
                .Where(i => i.TryAddToGlobalPollers())
                .ToList();
            Clusters = settings.Value.Clusters.Select(c => new SQLCluster(this, c)).ToList();
            AllInstances = StandaloneInstances.Union(Clusters.SelectMany(n => n.Nodes)).ToList();
        }

        public override MonitorStatus MonitorStatus => AllInstances.GetWorstStatus();
        public override bool IsMember(string node) =>
            AllInstances.Any(i => string.Equals(i.Name, node, StringComparison.InvariantCultureIgnoreCase));

        public SQLInstance GetInstance(string name) =>
            AllInstances.Find(i => string.Equals(i.Name, name, StringComparison.InvariantCultureIgnoreCase));
    }
}
