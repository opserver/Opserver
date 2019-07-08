using System;
using System.Threading.Tasks;
using StackExchange.Opserver.Monitoring;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Node
    {
        public async Task<PerfCounters.QueryResult<PerfCounters.CPUUtilization>> GetCPUUtilization()
        {
            if (MachineType.Contains("Windows"))
            {
                return await PerfCounters.Windows.GetCPUUtilization(Ip);
            }
            return new PerfCounters.QueryResult<PerfCounters.CPUUtilization>
                {
                    Duration = TimeSpan.Zero
                };
        }
    }
}