using System;
using StackExchange.Opserver.Monitoring;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Node
    {
        public PerfCounters.QueryResult<PerfCounters.CPUUtilization> GetCPUUtilization()
        {
            if (MachineType.Contains("Windows"))
            {
                return PerfCounters.Windows.GetCPUUtilization(Ip);
            }
            return new PerfCounters.QueryResult<PerfCounters.CPUUtilization>
                {
                    Duration = TimeSpan.Zero
                };
        }
    }
}