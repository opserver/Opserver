using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Node
    {
        public class CPUUtilization : GraphPoint
        {
            public short? AvgLoad { get; internal set; }
            public override double? Value => AvgLoad;
        }

        public class MemoryUtilization : GraphPoint
        {
            public float? AvgMemoryUsed { get; internal set; }
            public override double? Value => AvgMemoryUsed;
        }

        /// <summary>
        /// Gets CPU usage for this node (optionally) for the given time period, optionally sampled if pointCount is specified
        /// </summary>
        /// <param name="start">Start date, unbounded if null</param>
        /// <param name="end">End date, unbounded if null</param>
        /// <param name="pointCount">Points to return, if specified results will be sampled rather than including every point</param>
        /// <returns>CPU usage data points</returns>
        public Task<List<GraphPoint>> GetCPUUtilization(DateTime? start, DateTime? end, int? pointCount = null)
        {
            return DataProvider.GetCPUUtilizationAsync(this, start, end, pointCount);
        }

        /// <summary>
        /// Gets memory usage for this node (optionally) for the given time period, optionally sampled if pointCount is specified
        /// </summary>
        /// <param name="start">Start date, unbounded if null</param>
        /// <param name="end">End date, unbounded if null</param>
        /// <param name="pointCount">Points to return, if specified results will be sampled rather than including every point</param>
        /// <returns>Memory usage data points</returns>
        public Task<List<GraphPoint>> GetMemoryUtilization(DateTime? start, DateTime? end, int? pointCount = null)
        {
            return DataProvider.GetMemoryUtilizationAsync(this, start, end, pointCount);
        }

        /// <summary>
        /// Gets network usage for this node (optionally) for the given time period, optionally sampled if pointCount is specified
        /// </summary>
        /// <param name="start">Start date, unbounded if null</param>
        /// <param name="end">End date, unbounded if null</param>
        /// <param name="pointCount">Points to return, if specified results will be sampled rather than including every point</param>
        /// <returns>Network usage data points</returns>
        public Task<List<DoubleGraphPoint>> GetNetworkUtilization(DateTime? start, DateTime? end, int? pointCount = null)
        {
            return DataProvider.GetNetworkUtilizationAsync(this, start, end, pointCount);
        }
    }
}