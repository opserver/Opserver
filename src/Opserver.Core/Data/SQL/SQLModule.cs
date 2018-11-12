using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.SQL
{
    public class SQLModule : StatusModule
    {
        public static bool Enabled => AllInstances.Count > 0;
        /// <summary>
        /// SQL Instances not in clusters
        /// </summary>
        public static List<SQLInstance> StandaloneInstances { get; }
        public static List<SQLCluster> Clusters { get; }
        /// <summary>
        /// All SQL Instances, including those in clusters
        /// </summary>
        public static List<SQLInstance> AllInstances { get; }

        static SQLModule()
        {
            StandaloneInstances = Current.Settings.SQL.Instances
                .Select(i => new SQLInstance(i))
                .Where(i => i.TryAddToGlobalPollers())
                .ToList();
            Clusters = Current.Settings.SQL.Clusters.Select(c => new SQLCluster(c)).ToList();
            AllInstances = StandaloneInstances.Union(Clusters.SelectMany(n => n.Nodes)).ToList();
        }

        public override bool IsMember(string node)
        {
            return AllInstances.Any(i => string.Equals(i.Name, node, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
