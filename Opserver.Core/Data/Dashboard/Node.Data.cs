using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Node
    {
        public class CPUUtilization
        {
            public DateTime DateTime { get; internal set; }

            public Int16? AvgLoad { get; internal set; }
            public Int16? MaxLoad { get; internal set; }
        }

        public class MemoryUtilization
        {
            public DateTime DateTime { get; internal set; }

            public Single? AvgMemoryUsed { get; internal set; }
            public Single? MaxMemoryUsed { get; internal set; }
            public Single? TotalMemory { get; internal set; }
        }
        
        /// <summary>
        /// Gets CPU usage for this node (optionally) for the given time period, optionally sampled if pointCount is specified
        /// </summary>
        /// <param name="start">Start date, unbounded if null</param>
        /// <param name="end">End date, unbounded if null</param>
        /// <param name="pointCount">Points to return, if specified results will be sampled rather than including every point</param>
        /// <returns>CPU usage data points</returns>
        public Task<IEnumerable<CPUUtilization>> GetCPUUtilization(DateTime? start, DateTime? end, int? pointCount = null)
        {
            return DataProvider.GetCPUUtilization(this, start, end, pointCount);
        }

        /// <summary>
        /// Gets memory usage for this node (optionally) for the given time period, optionally sampled if pointCount is specified
        /// </summary>
        /// <param name="start">Start date, unbounded if null</param>
        /// <param name="end">End date, unbounded if null</param>
        /// <param name="pointCount">Points to return, if specified results will be sampled rather than including every point</param>
        /// <returns>Memory usage data points</returns>
        public Task<IEnumerable<MemoryUtilization>> GetMemoryUtilization(DateTime? start, DateTime? end, int? pointCount = null)
        {
            return DataProvider.GetMemoryUtilization(this, start, end, pointCount);
        }
    }
}