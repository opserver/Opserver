using System.Collections.Generic;
using Opserver.Data.SQL;

namespace Opserver.Views.SQL
{
    public class InstanceModel : DashboardModel
    {
        public List<SQLInstance.PerfCounterRecord> PerfCounters { get; set; }
    }
}