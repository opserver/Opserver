using System;
using System.Threading.Tasks;
using Opserver.Helpers;

namespace Opserver.Data.Dashboard
{
    public partial class Node
    {
        public async Task<PerfCounters.QueryResult<PerfCounters.CPUUtilization>> GetCPUUtilization()
        {
            if (OperatingSystem.IsWindows())
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
