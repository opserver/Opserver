using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Volume
    {
        public class VolumeUtilization
        {
            public DateTime DateTime { get; internal set; }

            public Double AvgDiskUsed { get; internal set; }
            public Double MaxDiskUsed { get; internal set; }
            public Double DiskSize { get; internal set; }
            public Single PercentDiskUsed { get; internal set; }
        }

        /// <summary>
        /// Gets usage for this volume (optionally) for the given time period, optionally sampled if pointCount is specified
        /// </summary>
        /// <param name="start">Start date, unbounded if null</param>
        /// <param name="end">End date, unbounded if null</param>
        /// <param name="pointCount">Points to return, if specified results will be sampled rather than including every point</param>
        /// <returns>Volume usage data points</returns>
        public Task<IEnumerable<VolumeUtilization>> GetUtilization(DateTime? start, DateTime? end, int? pointCount = null)
        {
            return DataProvider.GetUtilization(this, start, end, pointCount);
        }
    }
}