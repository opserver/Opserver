using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace StackExchange.Opserver.Helpers
{
    public class PerfCounters
    {
        public class Windows
        {
            //public static QueryResult<SystemUtilization> GetSystemUtilization(string machineName)
            //{
            //    var pc = new PerformanceCounter()

            //    //return Query(machineName,
            //    //             "select Name, PercentProcessorTime from Win32_PerfFormattedData_PerfOS_Processor",
            //    //             results => results.Select(mo => new SystemUtilization
            //    //             {
            //    //                 Name = mo["Name"].ToString() == "_Total" ? "Total" : mo["Name"].ToString(),
            //    //                 Utilization = (UInt64)mo["PercentProcessorTime"]
            //    //             }));
            //}
            public static QueryResult<CPUUtilization> GetCPUUtilization(string machineName)
            {
                return Query(machineName,
                             "select Name, PercentProcessorTime from Win32_PerfFormattedData_PerfOS_Processor",
                             results => results.Select(mo => new CPUUtilization
                                 {
                                     Name = mo["Name"].ToString() == "_Total" ? "Total" : mo["Name"].ToString(),
                                     Utilization = (UInt64) mo["PercentProcessorTime"]
                                 }));
            }

            private static QueryResult<T> Query<T>(string machineName, string query, Func<IEnumerable<ManagementObject>, IEnumerable<T>> conversion)
            {
                var timer = Stopwatch.StartNew();

                var scope = new ManagementScope($@"\\{machineName}\root\cimv2", GetConnectOptions(machineName));
                using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query)))
                using (var results = searcher.Get())
                {
                    var queryResults = results.Cast<ManagementObject>();
                    timer.Stop();
                    return new QueryResult<T>
                        {
                            Duration = timer.Elapsed,
                            Data = conversion(queryResults).ToList()
                        };
                }
            }

            private static ConnectionOptions GetConnectOptions(string machineName)
            {
                var co = new ConnectionOptions();
                if (machineName == Environment.MachineName)
                    return co;

                switch (machineName)
                {
                    case "localhost":
                    case "127.0.0.1":
                        return co;
                    default:
                        co = new ConnectionOptions
                            {
                                Authentication = AuthenticationLevel.Packet,
                                Timeout = new TimeSpan(0, 0, 30),
                                EnablePrivileges = true
                            };
                        break;
                }
                var wps = Current.Settings.Polling.Windows;
                if (wps != null && wps.AuthUser.HasValue() && wps.AuthPassword.HasValue())
                {
                    co.Username = wps.AuthUser;
                    co.Password = wps.AuthPassword;
                }
                return co;
            }
        }

        public class QueryResult<T>
        {
            public List<T> Data { get; set; }
            public TimeSpan Duration { get; set; }
        }

        public class CPUUtilization
        {
            public string Name { get; set; }
            public UInt64 Utilization { get; set; }
        }

        public class SystemUtilization
        {
            public string Name { get; set; }
            public UInt64 CPUUtilization { get; set; }
            public UInt64 MemoryUtilization { get; set; }
        }
    }
}