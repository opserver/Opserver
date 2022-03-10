using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Jil;
using StackExchange.Profiling;
using StackExchange.Utils;
using static Opserver.Data.Dashboard.Providers.BosunMetric;

namespace Opserver.Data.Dashboard.Providers
{
    public partial class BosunDataProvider : DashboardDataProvider<BosunSettings>
    {
        public override bool HasData => NodeCache.ContainsData;
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

        public BosunDataProvider(DashboardModule module, BosunSettings settings) : base(module, settings) { }

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
            {
                try
                {
                    var result = await Http.Request(url)
                        .AddHeader("X-Access-Token", Settings.APIKey)
                        .ExpectJson<T>(Options.SecondsSinceUnixEpochExcludeNullsUtc)
                        .GetAsync();
                    return new BosunApiResult<T> { Result = result.Data };
                }
                catch (DeserializationException de)
                {
                    de.Log();
                    return new BosunApiResult<T>
                    {
                        Error = $"Error deserializing response from bosun to {typeof(T).Name}: {de}. Details logged."
                    };
                }
                catch (Exception e)
                {
                    e.AddLoggedData("Url", url);
                    // TODO Log response in Data? Could be huge, likely truncate
                    e.Log();
                    return new BosunApiResult<T>
                    {
                        Error = $"Error fetching data from bosun: {e}. Details logged."
                    };
                }
            }
        }

        public override string GetManagementUrl(Node node)
        {
            return !Host.HasValue() ? null : $"{Host}host?host={node.Id.UrlEncode()}&time=1d-ago";
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
                if (get(DayCache.Data)?.TryGetValue(id.NormalizeForCache(), out var series) == true)
                    return series.PointData;
            }

            var apiResponse = await GetMetric(
                metricName,
                start.GetValueOrDefault(DateTime.UtcNow.AddYears(-1)),
                end,
                id);
            return apiResponse?.Series?[0]?.PointData ?? new List<GraphPoint>();
        }

        public override async Task<List<DoubleGraphPoint>> GetNetworkUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            if (IsApproximatelyLast24Hrs(start, end))
            {
                var cache = DayCache.Data?.Network;
                if (cache?.TryGetValue(node.Id.NormalizeForCache(), out var series) == true)
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
                TagCombos.AllNetDirections);

            return JoinNetwork(apiResponse?.Series) ?? new List<DoubleGraphPoint>();
        }

        public override Task<List<DoubleGraphPoint>> GetVolumePerformanceUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<DoubleGraphPoint>());

        public override async Task<List<GraphPoint>> GetUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var apiResponse = await GetMetric(
                Globals.DiskUsed,
                start.GetValueOrDefault(DateTime.UtcNow.AddYears(-1)),
                end,
                volume.NodeId,
                TagCombos.AllDisks);

            return apiResponse?.Series?[0]?.PointData ?? new List<GraphPoint>();
        }

        public override Task<List<DoubleGraphPoint>> GetPerformanceUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Task.FromResult(new List<DoubleGraphPoint>());
        }

        public override async Task<List<DoubleGraphPoint>> GetUtilizationAsync(Interface iface, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var apiResponse = await GetMetric(
                InterfaceMetricName(iface),
                start.GetValueOrDefault(DateTime.UtcNow.AddYears(-1)),
                end,
                iface.NodeId,
                TagCombos.AllDirectionsForInterface(iface.Id));

            return JoinNetwork(apiResponse?.Series) ?? new List<DoubleGraphPoint>();
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
