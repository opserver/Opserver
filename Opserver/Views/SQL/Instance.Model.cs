using System.Collections.Generic;
using StackExchange.Opserver.Data.SQL;

namespace StackExchange.Opserver.Views.SQL
{
    public class InstanceModel : DashboardModel
    {
        public List<SQLInstance.PerfCounterRecord> PerfCounters { get; set; }
    }
}