using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLCluster
    {
        public static List<SQLCluster> AllClusters { get; } =
            Current.Settings.SQL.Enabled
                ? Current.Settings.SQL.Clusters.Select(c => new SQLCluster(c)).ToList()
                : new List<SQLCluster>();
    }
}
