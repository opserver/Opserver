using System;
using StackExchange.Opserver.Monitoring;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Node
    {
        public PerfCounters.QueryResult<PerfCounters.CPUUtilization> GetCPUUtilization()
        {
            if (IsWindows)
            {
                return PerfCounters.Windows.GetCPUUtilization(Host);
            }
            return new PerfCounters.QueryResult<PerfCounters.CPUUtilization>
                {
                    Duration = TimeSpan.Zero
                };
        }
    }
}