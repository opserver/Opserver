using System.Collections.Generic;
using StackExchange.Opserver.Data.SQL;

namespace StackExchange.Opserver.Views.SQL
{
    public class ServersModel : DashboardModel
    {
        public List<SQLCluster> Clusters { get; set; }
        public List<SQLInstance> StandaloneInstances { get; set; }
        public List<SQLNode.AGInfo> AvailabilityGroups { get; set; }

        public SQLCluster CurrentCluster { get; set; }

        public JobSort? JobSort { get; set; }
        public SortDir? SortDirection { get; set; }
    }

    public enum JobSort
    {
        Server,
        Name,
        LastRun,
        Start,
        End,
        Duration,
        Enabled
    }
}
