using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using Jil;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public class BosunDataProvider : DashboardDataProvider
    {
        public override bool HasData { get { return true; } }
        private Uri HostUri { get; set; }
        public BosunDataProvider(string uniqueKey) : base(uniqueKey) { }
        public override int MinSecondsBetweenPolls { get { return 5; } }
        public override string NodeType { get { return "Bosun"; } }

        public BosunDataProvider(DashboardSettings.ProviderSettings provider) : base(provider)
        {
            HostUri = Host.StartsWith("http") ? new Uri(Host) : new Uri("http://" + Host);
        }

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return HostCache;
                yield return DayCache;
            }
        }
        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() { return null; }

        private Uri GetUri(string path)
        {
            return new Uri(HostUri, path);
        }

        private Cache<Dictionary<string, Node>> _hostCache;
        public Cache<Dictionary<string, Node>> HostCache
        {
            get { return _hostCache ?? (_hostCache = ProviderCache(FetchHosts, 60, logExceptions: true)); }
        }

        private static readonly Options JilOptions = new Options(dateFormat: DateTimeFormat.ISO8601);

        public Dictionary<string, Node> FetchHosts()
        {
            var result = GetFromBosun<Dictionary<string, Node>>(GetUri("/api/host"));
            UpdateHostLasts(DayCache.Data, result);
            return result;
        }

        public T GetFromBosun<T>(Uri url)
        {
            T result;
            try
            {
                using (MiniProfiler.Current.CustomTiming("Bosun", url.ToString()))
                {
                    var wr = WebRequest.Create(url);
                    wr.Headers[HttpRequestHeader.AcceptEncoding] = "gzip";
                    using (var resp = wr.GetResponse())
                    {
                        var baseStream = resp.GetResponseStream();
                        if (baseStream == null) return default(T);
                        var stream = resp.Headers[HttpResponseHeader.ContentEncoding].Contains("gzip")
                            ? new GZipStream(baseStream, CompressionMode.Decompress)
                            : baseStream;

                        using (var reader = new StreamReader(stream, true))
                        {
                            try
                            {
                                using (MiniProfiler.Current.Step("Bosun: Deserializing From " + url))
                                    result = JSON.Deserialize<T>(reader, JilOptions);
                            }
                            catch (DeserializationException e)
                            {
                                e.AddLoggedData("JSON-SnippetAfter", e.SnippetAfterError)
                                    .AddLoggedData("JSON-Position", e.Position.ToString());
                                throw;
                            }
                        }
                    }
                }
            }
            catch (WebException e)
            {
                e.AddLoggedData("Boson-Url", url.ToString());
                try
                {
                    if (e.Response != null)
                    {
                        var stream = e.Response.GetResponseStream();
                        if (stream != null)
                        {
                            using (var sr = new StreamReader(stream))
                                e.AddLoggedData("Bosun-Response", sr.ReadToEnd());
                        }
                    }
                } catch {}
                throw;
            }
            return result;
        }

        public override List<Node> AllNodes
        {
            get { return HostCache.HasData() ? HostCache.Data.Values.ToList() : new List<Node>(); }
        }

        public override Node GetNode(string host)
        {
            if (!Current.Settings.Dashboard.Enabled || host.IsNullOrEmpty() || !HostCache.HasData()) return null;
            Node result;
            HostCache.Data.TryGetValue(host.ToLower(), out result);
            return result;
        }

        public override IEnumerable<Node> GetNodesByIP(IPAddress ip)
        {
            return AllNodes.Where(n => n.IPAddresses.Contains(ip));
        }
        
        public Action<Cache<T>> UpdateFromBosun<T>(string opName, Func<T> getFromBosun) where T : class
        {
            return UpdateCacheItem(description: "Bosun Fetch: " + opName,
                getData: getFromBosun,
                addExceptionData: e => e.AddLoggedData("Server", Name));
        }

        public string GetJsonQuery(string metric, int secondsAgo, string host = "*", bool counter = true)
        {
            var wrap = new
            {
                start = secondsAgo + "s-ago",
                queries = new[]
                {
                    new
                    {
                        metric,
                        aggregator = "sum",
                        tags = new {host},
                        rate = counter,
                        rateOptions = new
                        {
                            resetValue = 1,
                            counter = true
                        }
                    }
                }
            };

            return JSON.SerializeDynamic(wrap);
        }

        private string ConvertToBosun(DateTime? date, DateTime valueIfNull)
        {
            return (date.HasValue ? date.Value : valueIfNull).ToString("yyyy/MM/dd-HH:mm:ss");
        }

        public string GetJsonQuery(string metric, string host = "*", DateTime? start = null, DateTime? end = null, bool counter = true)
        {
            var wrap = new
            {
                start = ConvertToBosun(start, DateTime.UtcNow.AddYears(-1)),
                end = ConvertToBosun(end, DateTime.UtcNow),
                queries = new[]
                {
                    new
                    {
                        metric,
                        aggregator = "sum",
                        tags = new {host},
                        rate = counter,
                        rateOptions = new
                        {
                            resetValue = 1,
                            counter = true
                        }
                    }
                }
            };
            return JSON.SerializeDynamic(wrap);
        }

        private Uri GetMetricUrl(string metric, int secondsAgo, int? pointCount, string host = "*", bool counter = true)
        {
            var json = GetJsonQuery(metric, secondsAgo, host: host, counter: counter);
            return GetUri("/api/graph?json=" + json + (pointCount.HasValue ? "&autods=" + pointCount : "") + "&autorate=0");
        }

        private Uri GetMetricUrl(string metric, DateTime? start, DateTime? end, int? pointCount, string host = "*", bool counter = true)
        {
            var json = GetJsonQuery(metric, start: start, end: end, host: host, counter: counter);
            return GetUri("/api/graph?json=" + json + (pointCount.HasValue ? "&autods=" + pointCount : "") + "&autorate=0");
        }

        private static int DaySeconds = 24*60*60;
        
        private Cache<IntervalCache> _dayCache;
        public Cache<IntervalCache> DayCache
        {
            get
            {
                return _dayCache ?? (_dayCache = ProviderCache(() =>
                {
                    var result = new IntervalCache
                    {
                        SecondsAgo = DaySeconds,
                        Series = new ConcurrentDictionary<string, Dictionary<string, PointSeries>>()
                    };
                    Action<string> addMetric = m =>
                    {
                        var url = GetMetricUrl(m, 24*60*60, 1000, counter: DashboardMetric.IsCounter(m));
                        result.Series[m] = GetFromBosun<BosunMetricResponse>(url).Series.ToDictionary(s => s.Host);
                    };

                    // TODO: Parallel
                    addMetric(DashboardMetric.CPUUsed);
                    addMetric(DashboardMetric.MemoryUsed);
                    addMetric(DashboardMetric.MemoryTotal);
                    addMetric(DashboardMetric.NetBytes);

                    UpdateHostLasts(result, HostCache.Data);

                    return result;

                }, 60, logExceptions: true));
            }
        }

        /// <summary>
        ///  Temporary until these values are in /api/host
        /// </summary>
        public void UpdateHostLasts(IntervalCache intervalCache, Dictionary<string, Node> nodes)
        {
            if (intervalCache == null || nodes == null) return;

            try
            {
                Dictionary<string, PointSeries>
                    cpu = intervalCache.CPUUsed,
                    memory = intervalCache.MemoryUsed,
                    memoryTotal = intervalCache.MemoryTotal,
                    network = intervalCache.NetBytes;

                Func<string, Dictionary<string, PointSeries>, float?> getLast = (host, dict) =>
                {
                    PointSeries series;
                    if (dict.TryGetValue(host, out series))
                    {
                        if (series.Data != null && series.Data.Count > 0)
                            return series.Data[series.Data.Count - 1][1];
                    }
                    return null;
                };

                foreach (var n in nodes)
                {
                    n.Value.CPULoad = (int?) getLast(n.Key, cpu);
                    n.Value.MemoryUsed = (long?) getLast(n.Key, memory);
                    n.Value.TotalMemory = (long?) getLast(n.Key, memoryTotal);
                    n.Value.Networkbps = (long?) getLast(n.Key, network)*8;
                }
            }
            catch (Exception e)
            {
                Current.LogException(e);
            }
        }

        public class IntervalCache
        {
            public int SecondsAgo { get; set; }

            public Dictionary<string, PointSeries> CPUUsed { get { return Series[DashboardMetric.CPUUsed]; } }
            public Dictionary<string, PointSeries> MemoryUsed { get { return Series[DashboardMetric.MemoryUsed]; } }
            public Dictionary<string, PointSeries> MemoryTotal { get { return Series[DashboardMetric.MemoryTotal]; } }
            public Dictionary<string, PointSeries> NetBytes { get { return Series[DashboardMetric.NetBytes]; } }

            public ConcurrentDictionary<string, Dictionary<string, PointSeries>> Series { get; set; }
        }

        public override PointSeries GetSeries(string metric, string host, int secondsAgo, int? pointCount = null, params Tuple<string, string>[] tags)
        {
            if (secondsAgo == DaySeconds && tags.Length == 0)
            {
                if (DayCache.Data != null && DayCache.Data.Series.ContainsKey(metric))
                {
                    PointSeries hostResult;
                    if (DayCache.Data.Series[metric].TryGetValue(host, out hostResult)) return hostResult;
                }
                return new PointSeries(host);
            }
            var url = GetMetricUrl(metric, secondsAgo, pointCount);
            return GetFromBosun<BosunMetricResponse>(url).Series.FirstOrDefault() ?? new PointSeries(host);
        }

        public override PointSeries GetSeries(string metric, string host, DateTime? start, DateTime? end, int? pointCount = null, params Tuple<string, string>[] tags)
        {
            var url = GetMetricUrl(metric, start, end, pointCount, host, DashboardMetric.IsCounter(metric));
            return GetFromBosun<BosunMetricResponse>(url).Series.FirstOrDefault() ?? new PointSeries(host);
        }
    }

    public class BosunMetric
    {
        public BosunMetricType? Type { get; set; }
        public string Unit { get; set; }
        public List<BosunMetricDescription> Description { get; set; }
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

        public bool HasTags
        {
            get { return Tags == null || Tags.Count == 0; }
        }
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
        private static readonly char[] _splComma = {','};
        private static readonly char[] _splEq = {'='};
        private string _host;
        public string Host
        {
            get
            {
                if (_host == null)
                {
                    string h;
                    _host = TryGetHost(Name, out h) ? h : "Unknown";
                }
                return _host;
            }
            set { _host = value; }
        }

        public string Name { get; set; }
        public string Metric { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public List<float[]> Data { get; set; }

        public PointSeries() { }
        public PointSeries(string host)
        {
            _host = host;
            Data = new List<float[]>();
        }

        /// <summary>
        /// Extract the host value from the Name expression string
        /// 
        /// Example input/output:
        ///     "this_should_fail"                            --> null
        ///     "os.net.bytes{host=}"                         --> ""
        ///     "os.net.bytes{host=fancy_machine}"            --> "fancy_machine"
        ///     "os.net.bytes{host=fancy_machine,iface=eth0}" --> "fancy_machine"
        /// </summary>
        private static bool TryGetHost(string s, out string host)
        {
            host = null;
            int beg = s.IndexOf('{');
            int end = s.LastIndexOf('}');
            if (-1 == beg) { return false; }
            if (end < beg) { return false; }
            int subBeg = beg + 1;
            int subLen = end - 1 - beg;
            if (subLen < 1) { return false; }
            var pairs = s.Substring(subBeg, subLen).Split(_splComma);
            for (int i = 0; i < pairs.Length; i++)
            {
                var kv = pairs[i].Split(_splEq);
                if (kv.Length != 2) { continue; }
                if ("host" == kv[0])
                {
                    host = kv[1];
                    return true;
                }
            }
            return false;
        }
    }
}