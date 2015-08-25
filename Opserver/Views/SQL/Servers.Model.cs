using System.Collections.Generic;
using StackExchange.Opserver.Data.SQL;

namespace StackExchange.Opserver.Views.SQL
{
    public class ServersModel : DashboardModel
    {
        public List<SQLCluster> Clusters { get; set; }
        public List<SQLInstance> StandaloneInstances { get; set; }
        public List<SQLNode.AvailabilityGroupInfo> AvailabilityGroups { get; set; }

        public SQLCluster CurrentCluster { get; set; }
    }
}