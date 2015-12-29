using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        /// <summary>
        /// SQL Instances not in clusters
        /// </summary>
        public static List<SQLInstance> AllStandalone { get; } =
            Current.Settings.SQL.Enabled
                ? Current.Settings.SQL.Instances
                    .Select(i => new SQLInstance(i))
                    .Where(i => i.TryAddToGlobalPollers())
                    .ToList()
                : new List<SQLInstance>();

        private static List<SQLInstance> _allInstances; 
        /// <summary>
        /// All SQL Instances, including those in clusters
        /// </summary>
        public static List<SQLInstance> AllInstances =>
            _allInstances ?? (_allInstances = SQLCluster.AllClusters.SelectMany(n => n.Nodes).Union(AllStandalone).ToList());

        public static bool IsSQLServer(string server)
        {
            return AllInstances.Any(i => string.Equals(i.Name, server, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
