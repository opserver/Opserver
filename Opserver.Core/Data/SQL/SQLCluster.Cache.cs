using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLCluster
    {
        public static List<SQLCluster> AllClusters => _sqlClusters ?? (_sqlClusters = LoadSQLClusters());

        private static readonly object _loadLock = new object();
        private static List<SQLCluster> _sqlClusters;
        private static List<SQLCluster> LoadSQLClusters()
        {
            lock (_loadLock)
            {
                if (_sqlClusters != null) return _sqlClusters;
                return Current.Settings.SQL.Enabled
                           ? Current.Settings.SQL.Clusters.Select(c => new SQLCluster(c)).ToList()
                           : new List<SQLCluster>();
            }
        }
    }
}
