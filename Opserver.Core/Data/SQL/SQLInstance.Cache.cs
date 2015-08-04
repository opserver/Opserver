using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        /// <summary>
        /// All SQL Instances, including those in clusters
        /// </summary>
        public static List<SQLInstance> AllInstances => _sqlInstances ?? (_sqlInstances = LoadInstances());

        /// <summary>
        /// SQL Instances not in clusters
        /// </summary>
        public static List<SQLInstance> AllStandalone => _standaloneInstances ?? (_standaloneInstances = LoadStandalone());

        private static readonly object _loadLock = new object();
        private static List<SQLInstance> _standaloneInstances;
        private static List<SQLInstance> LoadStandalone()
        {
            lock (_loadLock)
            {
                if (_standaloneInstances != null) return _standaloneInstances;
                return Current.Settings.SQL.Enabled
                           ? Current.Settings.SQL.Instances
                                    .Select(i => new SQLInstance(i.Name, i.ConnectionString, i.ObjectName))
                                    .Where(i => i.TryAddToGlobalPollers())
                                    .ToList()
                           : new List<SQLInstance>();
            }
        }
        private static List<SQLInstance> _sqlInstances;
        private static List<SQLInstance> LoadInstances()
        {
            lock (_loadLock)
            {
                if (_sqlInstances != null) return _sqlInstances;
                return SQLCluster.AllClusters.SelectMany(n => n.Nodes).Union(AllStandalone).ToList();
            }
        } 

        public static bool IsSQLServer(string server)
        {
            return AllInstances.Any(i => string.Equals(i.Name, server, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
