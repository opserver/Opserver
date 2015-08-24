using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Interface
    {
        public class InterfaceUtilization
        {
            public DateTime DateTime { get; internal set; }

            public Int16? AvgLoad { get; internal set; }
            public Int16? MaxLoad { get; internal set; }

            public Single? InMaxBps { get; internal set; }
            public Single? InAvgBps { get; internal set; }

            public Single? OutMaxBps { get; internal set; }
            public Single? OutAvgBps { get; internal set; }
        }

        /// <summary>
        /// Gets usage for this interface (optionally) for the given time period, optionally sampled if pointCount is specified
        /// </summary>
        /// <param name="start">Start date, unbounded if null</param>
        /// <param name="end">End date, unbounded if null</param>
        /// <param name="pointCount">Points to return, if specified results will be sampled rather than including every point</param>
        /// <returns>Interface usage data points</returns>
        public Task<IEnumerable<InterfaceUtilization>> GetUtilization(DateTime? start, DateTime? end, int? pointCount = null)
        {
            return DataProvider.GetUtilization(this, start, end, pointCount);
        }
    }
}