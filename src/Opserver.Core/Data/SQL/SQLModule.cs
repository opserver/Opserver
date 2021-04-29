using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Opserver.Data.SQL
{
    public class SQLModule : StatusModule<SQLSettings>
    {
        public override string Name => "SQL";
        public override bool Enabled => AzureServers.Count > 0 || StandaloneInstances.Count > 0 || Clusters.Count > 0;

        /// <summary>
        /// SQL Azure server instances.
        /// </summary>
        public List<SQLAzureServer> AzureServers { get; }
        /// <summary>
        /// SQL Instances not in clusters
        /// </summary>
        public List<SQLInstance> StandaloneInstances { get; }
        public List<SQLCluster> Clusters { get; }
        /// <summary>
        /// All SQL Instances, including those in clusters or derived from Azure servers
        /// </summary>
        public IEnumerable<SQLInstance> AllInstances { get; }

        public SQLModule(IConfiguration config, PollingService poller) : base(config, poller)
        {
            AzureServers = Settings.Instances
                .Where(i => i.Type == SQLSettings.InstanceType.Azure)
                .Select(i => new SQLAzureServer(this, i))
                .Where(i => i.TryAddToGlobalPollers())
                .ToList();

            StandaloneInstances = Settings.Instances
                .Where(i => i.Type == SQLSettings.InstanceType.Default)
                .Select(i => new SQLInstance(this, i))
                .Where(i => i.TryAddToGlobalPollers())
                .ToList();

            Clusters = Settings.Clusters.Select(c => new SQLCluster(this, c)).ToList();

            AllInstances = StandaloneInstances
                .Union(AzureServers.SelectMany(s => s.Instances.SafeData(true)))
                .Union(Clusters.SelectMany(n => n.Nodes));
        }

        public override MonitorStatus MonitorStatus => AllInstances.GetWorstStatus();
        public override bool IsMember(string node) =>
            AllInstances.Any(i => string.Equals(i.Name, node, StringComparison.InvariantCultureIgnoreCase));

        public SQLInstance GetInstance(string name) =>
            AllInstances
                .Where(i => string.Equals(i.Name, name, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();
    }
}
