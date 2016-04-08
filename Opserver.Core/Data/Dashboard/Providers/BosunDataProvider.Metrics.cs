using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jil;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public partial class BosunDataProvider
    {
        public class TSDBQuery
        {
            // ReSharper disable InconsistentNaming
            public string start { get; set; }
            public string end { get; set; }
            public List<object> queries { get; set; }
            // ReSharper restore InconsistentNaming

            public TSDBQuery(DateTime? startTime, DateTime? endTime = null)
            {
                start = ConvertTime(startTime, DateTime.UtcNow.AddYears(-1));
                if (endTime.HasValue) end = ConvertTime(endTime, DateTime.UtcNow);
                queries = new List<object>();
            }

            public static string ConvertTime(DateTime? date, DateTime valueIfNull)
            {
                return (date ?? valueIfNull).ToString("yyyy/MM/dd-HH:mm:ss", CultureInfo.InvariantCulture);
            }

            public void AddQuery(string metric, string host = "*", bool counter = true, IDictionary<string, string> tags = null)
            {
                var query = new
                {
                    metric,
                    aggregator = "sum",
                    tags = new Dictionary<string, string>
                    {
                        [nameof(host)] = host
                    },
                    rate = counter,
                    rateOptions = new
                    {
                        resetValue = 1,
                        counter = true
                    }
                };
                if (tags != null)
                {
                    foreach (var p in tags) query.tags[p.Key] = p.Value;
                }
                queries.Add(query);
            }
        }

        public async Task<BosunMetricResponse> RunTSDBQueryAsync(TSDBQuery query, int? pointCount = null)
        {
            var json = JSON.SerializeDynamic(query, Options.ExcludeNullsUtc);
            var url = GetUrl($"api/graph?json={json}{(pointCount.HasValue ? "&autods=" + pointCount.ToString() : "")}");
            var apiResult = await GetFromBosunAsync<BosunMetricResponse>(url).ConfigureAwait(false);
            return apiResult.Result;
        }

        public Task<BosunMetricResponse> GetMetric(string metricName, DateTime start, DateTime? end = null, string host = "*", IDictionary<string, string> tags = null)
        {
            metricName = BosunMetric.GetDenormalized(metricName, host);
            var query = new TSDBQuery(start, end);
            query.AddQuery(metricName, host, BosunMetric.IsCounter(metricName, host), tags);
            return RunTSDBQueryAsync(query, 500);
        }

        private Cache<IntervalCache> _dayCache;
        public Cache<IntervalCache> DayCache
        {
            get
            {
                return _dayCache ?? (_dayCache = ProviderCache(async () =>
                {
                    var result = new IntervalCache(TimeSpan.FromDays(1));
                    Func<string, string[], Task> addMetric = async (metricName, tags) =>
                    {
                        var tagDict = tags?.ToDictionary(t => t, t => "*");
                        var apiResult = await GetMetric(metricName, result.StartTime, tags: tagDict).ConfigureAwait(false);
                        if (apiResult == null) return;
                        if (tags?.Any() ?? false)
                        {
                            result.MultiSeries[metricName] = apiResult.Series
                                .GroupBy(s => s.Host)
                                .ToDictionary(s => s.Key.NormalizeForCache(), s => s.ToList());
                        }
                        else
                            result.Series[metricName] = apiResult.Series.ToDictionary(s => s.Host.NormalizeForCache());
                    };
                    
                    var c = addMetric(BosunMetric.Globals.CPU, null);
                    var m = addMetric(BosunMetric.Globals.MemoryUsed, null);
                    var n = addMetric(BosunMetric.Globals.NetBytes, new[] {BosunMetric.Tags.Direction});
                    await Task.WhenAll(c, m, n).ConfigureAwait(false); // parallel baby!

                    return result;
                }, 60, 3600));
            }
        }
        
        public class IntervalCache
        {
            public TimeSpan TimeSpan { get; set; }
            public DateTime StartTime { get; set; }

            public Dictionary<string, PointSeries> CPU => Series[BosunMetric.Globals.CPU];
            public Dictionary<string, PointSeries> Memory => Series[BosunMetric.Globals.MemoryUsed];
            public Dictionary<string, List<PointSeries>> Network => MultiSeries[BosunMetric.Globals.NetBytes];

            internal ConcurrentDictionary<string, Dictionary<string, PointSeries>> Series { get; set; }
            internal ConcurrentDictionary<string, Dictionary<string, List<PointSeries>>> MultiSeries { get; set; }

            public IntervalCache(TimeSpan timespan)
            {
                TimeSpan = timespan;
                StartTime = DateTime.UtcNow - timespan;
                Series = new ConcurrentDictionary<string, Dictionary<string, PointSeries>>();
                MultiSeries = new ConcurrentDictionary<string, Dictionary<string, List<PointSeries>>>();
            }
        }
    }
    
    public class BosunMetric
    {
        public BosunMetricType? Type { get; set; }
        public string Unit { get; set; }
        public List<BosunMetricDescription> Description { get; set; }

        public static class Globals
        {
            public const string CPU = "os.cpu";
            public const string MemoryUsed = "os.mem.used";
            public const string NetBytes = "os.net.bytes";
            public const string NetBondBytes = "os.net.bond.bytes";
            public const string NetVirtualBytes = "os.net.virtual.bytes";
            public const string NetTunnelBytes = "os.net.tunnel.bytes";
            public const string NetOtherBytes = "os.net.other.bytes";
            public const string DiskUsed = "os.disk.fs.space_used";
        }

        private static class Suffixes
        {
            public const string CPU = "." + Globals.CPU;
        }

        public static class Tags
        {
            public const string Direction = "direction";
            public const string Disk = "disk";
            public const string Host = "host";
            public const string IFace = "iface";
        }

        public static class TagValues
        {
            public const string In = "in";
            public const string Out = "out";
        }

        public static class TagCombos
        {
            public static readonly Dictionary<string, string>
                AllNetDirections = new Dictionary<string, string> {{Tags.Direction, "*"}},
                AllDisks = new Dictionary<string, string> {{Tags.Disk, "*"}};

            public static Dictionary<string, string> AllDirectionsForInterface(string ifaceId)
                => new Dictionary<string, string> {{Tags.Direction, "*"}, {Tags.IFace, ifaceId}};
        }

        public static bool IsCounter(string metric, string host)
        {
            if (metric.IsNullOrEmpty()) return false;
            if (metric.StartsWith("__"))
            {
                metric = metric.Replace($"__{host}.", "");
            }
            switch (metric)
            {
                case Globals.CPU:
                case Globals.NetBytes:
                case Globals.NetBondBytes:
                case Globals.NetOtherBytes:
                case Globals.NetTunnelBytes:
                case Globals.NetVirtualBytes:
                    return true;
                default:
                    return false;
            }
        }

        public static string InterfaceMetricName(Interface i)
        {
            switch (i.TypeDescription)
            {
                case "bond":
                    return Globals.NetBondBytes;
                case "other":
                    return Globals.NetOtherBytes;
                case "tunnel":
                    return Globals.NetTunnelBytes;
                case "virtual":
                    return Globals.NetVirtualBytes;
                default:
                    return Globals.NetBytes;
            }
        }

        public static string GetDenormalized(string metric, string host)
        {
            if (host != null && !host.Contains("*") && !host.Contains("|"))
            {
                switch (metric)
                {
                    case Globals.CPU:
                    case Globals.MemoryUsed:
                    case Globals.NetBondBytes:
                    case Globals.NetOtherBytes:
                    case Globals.NetTunnelBytes:
                    case Globals.NetVirtualBytes:
                    case Globals.NetBytes:
                        return $"__{host}.{metric}";
                }
            }
            return metric;
        }
    }

    public enum BosunMetricType
    {
        gauge,
        counter,
        rate
    }

    public class BosunMetricDescription
    {
        public string Text { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }

    public class BosunMetricResponse
    {
        public List<string> Queries { get; set; }
        public List<PointSeries> Series { get; set; }
    }

    /// <summary>
    /// The Data field consists of pairs, Data[n][0] is the epoch, Data[n][1] is the value.
    /// </summary>
    public class PointSeries
    {
        private static readonly Regex HostRegex = new Regex(@"\{host=(.*)[,|\}]", RegexOptions.Compiled);
        private string _host;
        public string Host
        {
            get
            {
                if (_host == null)
                {
                    if (Tags.ContainsKey("host"))
                    {
                        Host = Tags["host"];
                    }
                    else
                    {
                        var match = HostRegex.Match(Name);
                        _host = match.Success ? match.Groups[1].Value : "Unknown";
                    }
                }
                return _host;
            }
            set { _host = value; }
        }

        public string Name { get; set; }
        public string Metric { get; set; }
        public string Unit { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public List<float[]> Data { get; set; }

        private List<GraphPoint> _pointData;
        public List<GraphPoint> PointData => (_pointData ?? (_pointData = Data.Select(p => new GraphPoint
        {
            DateEpoch = (long) p[0],
            Value = p[1]
        }).ToList()));

        public PointSeries() { }
        public PointSeries(string host)
        {
            _host = host;
            Data = new List<float[]>();
        }
    }
}
