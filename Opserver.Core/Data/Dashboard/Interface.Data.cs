using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Interface
    {
        public class InterfaceUtilization : DoubleGraphPoint
        {
            public override long DateEpoch { get; set; }
            public float? InAvgBps { get; internal set; }
            public float? OutAvgBps { get; internal set; }

            public override double? Value => InAvgBps;
            public override double? BottomValue => OutAvgBps;
        }

        /// <summary>
        /// Gets usage for this interface (optionally) for the given time period, optionally sampled if pointCount is specified
        /// </summary>
        /// <param name="start">Start date, unbounded if null</param>
        /// <param name="end">End date, unbounded if null</param>
        /// <param name="pointCount">Points to return, if specified results will be sampled rather than including every point</param>
        /// <returns>Interface usage data points</returns>
        public Task<List<DoubleGraphPoint>> GetUtilization(DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Node.DataProvider.GetUtilizationAsync(this, start, end, pointCount);
        }
    }
}