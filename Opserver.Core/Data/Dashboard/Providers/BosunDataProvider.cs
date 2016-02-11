using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Jil;
using StackExchange.Profiling;
using static StackExchange.Opserver.Data.Dashboard.Providers.BosunMetric;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public partial class BosunDataProvider : DashboardDataProvider<BosunSettings>
    {
        public override bool HasData => NodeCache.HasData();
        public string Host => Settings.Host;
        public override int MinSecondsBetweenPolls => 5;
        public override string NodeType => "Bosun";

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return NodeCache;
                yield return NodeMetricCache;
                yield return DayCache;
            }
        }

        public BosunDataProvider(BosunSettings settings) : base(settings) { }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() { return null; }

        public override List<Node> AllNodes => NodeCache.Data ?? new List<Node>();
        

        private string GetUrl(string path)
        {
            // Note: Host is normalized with a trailing slash when settings are loaded
            return Host + path;
        }

        public class BosunApiResult<T>
        {
            public T Result { get; internal set; }
            public string Error { get; internal set; }
            public bool Success => Error.IsNullOrEmpty();
        }

        public async Task<BosunApiResult<T>> GetFromBosunAsync<T>(string url)
        {
            using (MiniProfiler.Current.Step("Bosun Fetch"))
            using (MiniProfiler.Current.CustomTiming("bosun", url))
            using (var wc = new WebClient())
            {
                try
                {
                    using (var s = await wc.OpenReadTaskAsync(url).ConfigureAwait(false))
                    using (var sr = new StreamReader(s))
                    {
                        var result = JSON.Deserialize<T>(sr, Options.SecondsSinceUnixEpochExcludeNullsUtc);
                        return new BosunApiResult<T> { Result = result };
                    }
                }
                catch (DeserializationException de)
                {
                    Current.LogException(de);
                    return new BosunApiResult<T>
                    {
                        Error = $"Error deserializing response from bosun to {typeof(T).Name}: {de}. Details logged."
                    };
                }
                catch (Exception e)
                {
                    e.AddLoggedData("Url", url);
                    // TODO Log response in Data? Could be huge, likely truncate
                    Current.LogException(e);
                    return new BosunApiResult<T>
                    {
                        Error = $"Error fetching data from bosun: {e}. Details logged."
                    };
                }
            }
        }

        public override string GetManagementUrl(Node node)
        {   
            return !Host.HasValue() ? null : $"http://{Host}/host?host={node.Id.UrlEncode()}&time=1d-ago";
        }

        public override Task<List<GraphPoint>> GetCPUUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return GetRecentAsync(node.Id, start, end, p => p?.CPU, Globals.CPU);
        }

        public override Task<List<GraphPoint>> GetMemoryUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return GetRecentAsync(node.Id, start, end, p => p?.Memory, Globals.MemoryUsed);
        }

        private async Task<List<GraphPoint>> GetRecentAsync(
            string id,
            DateTime? start,
            DateTime? end,
            Func<IntervalCache, Dictionary<string, PointSeries>> get,
            string metricName)
        {
            if (IsApproximatelyLast24Hrs(start, end))
            {
                PointSeries series = null;
                if (get(DayCache.Data)?.TryGetValue(id.NormalizeForCache(), out series) == true)
                    return series.PointData;
            }

            var apiResponse = await GetMetric(
                metricName,
                start.GetValueOrDefault(DateTime.UtcNow.AddYears(-1)),
                end,
                id).ConfigureAwait(false);
            return apiResponse?.Series?[0]?.PointData ?? new List<GraphPoint>();
        }

        public override async Task<List<DoubleGraphPoint>> GetNetworkUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            if (IsApproximatelyLast24Hrs(start, end))
            {
                List<PointSeries> series = null;
                var cache = DayCache.Data?.Network;
                if (cache?.TryGetValue(node.Id.NormalizeForCache(), out series) == true)
                {
                    var result = JoinNetwork(series);
                    if (result != null)
                        return result;
                }
            }

            var apiResponse = await GetMetric(
                Globals.NetBytes,
                start.GetValueOrDefault(DateTime.UtcNow.AddYears(-1)),
                end,
                node.Id,
                TagCombos.AllNetDirections).ConfigureAwait(false);

            return JoinNetwork(apiResponse?.Series) ?? new List<DoubleGraphPoint>();
        }

        public override async Task<List<GraphPoint>> GetUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var apiResponse = await GetMetric(
                Globals.DiskUsed,
                start.GetValueOrDefault(DateTime.UtcNow.AddYears(-1)),
                end,
                volume.NodeId,
                TagCombos.AllDisks).ConfigureAwait(false);

            return apiResponse?.Series?[0]?.PointData ?? new List<GraphPoint>();
        }

        public override async Task<List<DoubleGraphPoint>> GetUtilizationAsync(Interface nodeInteface, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var apiResponse = await GetMetric(
                InterfaceMetricName(nodeInteface),
                start.GetValueOrDefault(DateTime.UtcNow.AddYears(-1)),
                end,
                nodeInteface.NodeId,
                TagCombos.AllDirectionsForInterface(nodeInteface.Id)).ConfigureAwait(false);

            return JoinNetwork(apiResponse.Series) ?? new List<DoubleGraphPoint>();
        }

        /// <summary>
        /// Determines if the passed in dates are approximately the last 24 hours, 
        /// so that we can share the day cache more efficiently
        /// </summary>
        /// <param name="start">Start date of the range</param>
        /// <param name="end">Optional end date of the range</param>
        /// <param name="fuzzySeconds">How many seconds to allow on each side of *exactly* 24 hours ago to be a match</param>
        /// <returns></returns>
        public bool IsApproximatelyLast24Hrs(DateTime? start, DateTime? end, int fuzzySeconds = 90)
        {
            if (!start.HasValue) return false;
            if (Math.Abs((DateTime.UtcNow.AddDays(-1) - start.Value).TotalSeconds) <= fuzzySeconds)
            {
                return !end.HasValue || Math.Abs((DateTime.UtcNow - end.Value).TotalSeconds) <= fuzzySeconds;
            }
            return false;
        }

        private List<DoubleGraphPoint> JoinNetwork(List<PointSeries> allSeries)
        {
            var inData = allSeries?.FirstOrDefault(s => s.Tags[Tags.Direction] == TagValues.In)?.PointData;
            var outData = allSeries?.FirstOrDefault(s => s.Tags[Tags.Direction] == TagValues.Out)?.PointData;

            if (inData == null || outData == null)
                return null;

            return inData.Join(outData,
                i => i.DateEpoch,
                o => o.DateEpoch,
                (i, o) => new DoubleGraphPoint
                {
                    DateEpoch = i.DateEpoch,
                    Value = i.Value,
                    BottomValue = o.Value
                }).ToList();
        } 
    }
}
