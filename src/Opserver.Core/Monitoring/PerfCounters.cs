using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace Opserver.Monitoring
{
    public static class PerfCounters
    {
        public static class Windows
        {
            public static Task<QueryResult<CPUUtilization>> GetCPUUtilization(string machineName)
            {
                return QueryAsync(machineName,
                             "select Name, PercentProcessorTime from Win32_PerfFormattedData_PerfOS_Processor",
                             results => results.Select(mo => new CPUUtilization
                                 {
                                     Name = mo["Name"].ToString() == "_Total" ? "Total" : mo["Name"].ToString(),
                                     Utilization = (ulong) mo["PercentProcessorTime"]
                                 }));
            }

            private static async Task<QueryResult<T>> QueryAsync<T>(string machineName, string query, Func<IEnumerable<ManagementObject>, IEnumerable<T>> conversion)
            {
                var timer = Stopwatch.StartNew();

                // TODO: Poll creds
                using (var q = new Wmi.WmiQuery(machineName, query))
                {
                    var queryResults = (await q.Result).Cast<ManagementObject>();
                    timer.Stop();
                    return new QueryResult<T>
                        {
                            Duration = timer.Elapsed,
                            Data = conversion(queryResults).ToList()
                        };
                }
            }
        }

        public class QueryResult<T>
        {
            public List<T> Data { get; internal set; }
            public TimeSpan Duration { get; internal set; }
        }

        public class CPUUtilization
        {
            public string Name { get; internal set; }
            public ulong Utilization { get; internal set; }
        }

        public class SystemUtilization
        {
            public string Name { get; internal set; }
            public ulong CPUUtilization { get; internal set; }
            public ulong MemoryUtilization { get; internal set; }
        }
    }
}
