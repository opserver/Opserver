using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
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
        private const int DaySeconds = 24 * 60 * 60;
        private Uri _hostUri;
        public override bool HasData { get { return true; } }
        public override int MinSecondsBetweenPolls { get { return 5; } }
        public override string NodeType { get { return "Bosun"; } }
        private object _hu_update_lock = new Object();

        public BosunDataProvider(string uniqueKey) : base(uniqueKey) { }
        public BosunDataProvider(DashboardSettings.ProviderSettings provider) : base(provider)
        {
            _hostUri = Host.StartsWith("http") ? new Uri(Host) : new Uri("http://" + Host);
        }

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return HostCache;
                yield return DayCache;
            }
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

        public override List<HTTPUnitResult> GetHTTPUnitResults()
        {
            var rs = new List<HTTPUnitResult>();
            var testCases = HTTPUnitCache.Data.Data;
            foreach (var tc in testCases)
            {
                foreach (var urlHost in tc.Value)
                {
                    var r = new HTTPUnitResult();
                    r.TestCase = tc.Key;
                    r.UrlHost = urlHost.Key;
                    var v = urlHost.Value;
                    TryGetLatest(v, DashboardMetric.HUError, out r.Error);
                    TryGetLatest(v, DashboardMetric.HUGotExpectedCode, out r.GotCode);
                    TryGetLatest(v, DashboardMetric.HUGotExpectedRegex, out r.GotRegex);
                    TryGetLatest(v, DashboardMetric.HUGotExpectedText, out r.GotText);
                    TryGetLatest(v, DashboardMetric.HUSocketConnected, out r.Connected);
                    TryGetLatest(v, DashboardMetric.HUTimeTotal, out r.TimeTotal);
                    rs.Add(r);
                }
            }
            return rs;
        }

        private static bool TryGetLatest<T>(
            Dictionary<string, PointSeries> d,
            string key,
            out T val)
        {
            PointSeries p;
            if (d.TryGetValue(key, out p))
            {
                if (p.Data != null && p.Data.Count > 0)
                {
                    float f = p.Data[p.Data.Count - 1][1];
                    val = (T)Convert.ChangeType(f, typeof(T));
                    return true;
                }
            }
            val = default(T);
            return false;
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() { return null; }

        private Uri GetUri(string path)
        {
            return new Uri(_hostUri, path);
        }

        private Cache<Dictionary<string, Node>> _hostCache;
        private Cache<Dictionary<string, Node>> HostCache
        {
            get { return _hostCache ?? (_hostCache = ProviderCache(FetchHosts, 60, logExceptions: true)); }
        }

        private static readonly Options JilOptions = new Options(dateFormat: DateTimeFormat.ISO8601);

        private Dictionary<string, Node> FetchHosts()
        {
            var result = GetFromBosun<Dictionary<string, Node>>(GetUri("/api/host"));
            UpdateHostLasts(DayCache.Data, result);
            return result;
        }

        private T GetFromBosun<T>(Uri url)
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


        private Action<Cache<T>> UpdateFromBosun<T>(string opName, Func<T> getFromBosun) where T : class
        {
            return UpdateCacheItem(description: "Bosun Fetch: " + opName,
                getData: getFromBosun,
                addExceptionData: e => e.AddLoggedData("Server", Name));
        }

        private string ConvertToBosun(DateTime? date, DateTime valueIfNull)
        {
            return (date.HasValue ? date.Value : valueIfNull).ToString("yyyy/MM/dd-HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private Uri GetMetricUrl(
            string metric,
            string aggregator,
            Dictionary<string, string> tags,
            DateTime? start,
            DateTime? end,
            int? pointCount,
            bool counter = true)
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
                        aggregator = aggregator,
                        tags = tags,
                        rate = counter,
                        rateOptions = new
                        {
                            resetValue = 1,
                            counter = true
                        }
                    }
                }
            };
            string json = JSON.SerializeDynamic(wrap);
            return GetUri("/api/graph?json=" + json + (pointCount.HasValue ? "&autods=" + pointCount : "") + "&autorate=0");
        }

        /// <summary>
        /// TODO: Merge with GetMetricUrl later
        /// </summary>
        private Uri GetMetricUrlDs(
            string metric,
            string aggregator,
            string ds,
            string dstime,
            Dictionary<string, string> tags,
            DateTime? start,
            DateTime? end)
        {
            var wrap = new
            {
                start = ConvertToBosun(start, DateTime.UtcNow.AddDays(-1)),
                end = ConvertToBosun(end, DateTime.UtcNow),
                queries = new[]
                {
                    new
                    {
                        aggregator,
                        metric,
                        derivative = "gauge",
                        ds,
                        dstime,
                        tags,
                        downsample = dstime + "-" + ds
                    }
                }
            };
            string json = JSON.SerializeDynamic(wrap);
            return GetUri("/api/graph?json=" + json + "&autorate=0");
        }



        private Cache<IntervalCache> _dayCache;
        private Cache<IntervalCache> DayCache
        {
            get
            {
                if (_dayCache != null)
                {
                    return _dayCache;
                }
                _dayCache = ProviderCache(FetchNodeMetrics, 60, logExceptions: true);
                return _dayCache;
            }
        }

        private Cache<HUCacheData> _HUCache;
        private Cache<HUCacheData> HTTPUnitCache
        {
            get
            {
                if (_HUCache != null)
                {
                    if (_HUCache.IsExpired && !_HUCache.IsPolling) // TODO: investigate why needed
                    {
                        _HUCache.Poll(true);
                    }
                    return _HUCache;
                }
                _HUCache = ProviderCache(FetchHTTPUnitMetrics, 60, logExceptions: true);
                return _HUCache;
            }
        }

        private void FetchMetric(
            IntervalCache ic,
            string aggregator,
            Dictionary<string, string> tags,
            string metric)
        {
            var dayAgo = DateTime.UtcNow.AddDays(-1);
            bool isCounter = DashboardMetric.IsCounter(metric);
            var mu = GetMetricUrl(metric, aggregator, tags, dayAgo, null, 1000, counter: isCounter);
            var r = GetFromBosun<BosunMetricResponse>(mu);
            ic.Series[metric] = r.Series.ToDictionary(s => s.Host);
        }

        private void FetchHUMetric(
            HUCacheData c,
            string aggregator,
            string ds,
            string dstime,
            string metric)
        {
            var tags = new Dictionary<string, string>
            { 
                { "hc_test_case", "*" },
                { "url_host",     "*" }
            };
            var dayAgo = DateTime.UtcNow.AddHours(-2);
            var mu = GetMetricUrlDs(metric, aggregator, ds, dstime, tags, dayAgo, null);
            var r = GetFromBosun<BosunMetricResponse>(mu);
            lock (_hu_update_lock)
            {
                UpdateHUCache(c, metric, r.Series);
            }
        }

        /// <summary>
        /// Finds point series for each HTTPUnit test case and stores in
        /// dictionaries as TestCaseName --> UrlHost --> MetricName --> PointSerie
        /// to make it easy to get all data for a single test case.
        /// </summary>
        /// <param name="ic"></param>
        /// <param name="metric"></param>
        /// <param name="ps"></param>
        private static void UpdateHUCache(
            HUCacheData c,
            string metric,
            List<PointSeries> ps)
        {
            var cases = c.Data;
            foreach (var p in ps)
            {
                string caseName = p.GetTagValue("hc_test_case");
                string urlHost =  p.GetTagValue("url_host");
                Dictionary<string, Dictionary<string, PointSeries>> urlHosts;
                Dictionary<string, PointSeries> metrics;
                if (!cases.TryGetValue(caseName, out urlHosts))
                {
                    urlHosts = new Dictionary<string, Dictionary<string, PointSeries>>();
                }
                if (!urlHosts.TryGetValue(urlHost, out metrics))
                {
                    metrics = new Dictionary<string, PointSeries>();
                }
                metrics[metric] = p;
                urlHosts[urlHost] = metrics;
                cases[caseName] = urlHosts;
            }
        }

        private IntervalCache FetchNodeMetrics()
        {
            IntervalCache ic = MakeDayCache();
            var tags = new Dictionary<string, string> {{ "host", "*" }};
            // TODO: fetch in parallel
            FetchMetric(ic, "sum", tags, DashboardMetric.CPUUsed);
            FetchMetric(ic, "sum", tags, DashboardMetric.MemoryUsed);
            FetchMetric(ic, "sum", tags, DashboardMetric.MemoryTotal);
            FetchMetric(ic, "sum", tags, DashboardMetric.NetBytes);
            UpdateHostLasts(ic, HostCache.Data);
            return ic;
        }

        private HUCacheData FetchHTTPUnitMetrics()
        {
            var c = new HUCacheData();
            // TODO: fetch in parallel
            FetchHUMetric(c, "min", "max", "1m", DashboardMetric.HUError);
            FetchHUMetric(c, "min", "max", "1m", DashboardMetric.HUGotExpectedCode);
            FetchHUMetric(c, "min", "max", "1m", DashboardMetric.HUGotExpectedRegex);
            FetchHUMetric(c, "min", "max", "1m", DashboardMetric.HUGotExpectedText);
            FetchHUMetric(c, "min", "max", "1m", DashboardMetric.HUSocketConnected);
            FetchHUMetric(c, "avg", "min", "1m", DashboardMetric.HUTimeTotal);
            return c;
        }

        private static IntervalCache MakeDayCache()
        {
            return new IntervalCache
            {
                SecondsAgo = DaySeconds,
                Series = new ConcurrentDictionary<string, Dictionary<string, PointSeries>>()
            };
        }

        /// <summary>
        ///  Temporary until these values are in /api/host
        /// </summary>
        private void UpdateHostLasts(IntervalCache ic, Dictionary<string, Node> nodes)
        {
            if (ic == null || nodes == null) return;

            try
            {
                Dictionary<string, PointSeries>
                    cpu         = ic.Series[DashboardMetric.CPUUsed],
                    memory      = ic.Series[DashboardMetric.MemoryUsed],
                    memoryTotal = ic.Series[DashboardMetric.MemoryTotal],
                    network     = ic.Series[DashboardMetric.NetBytes];

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

        private class HUCacheData
        {
            public Dictionary<string, Dictionary<string, Dictionary<string, PointSeries>>> Data
            {
                get; set;
            }

            public HUCacheData()
            {
                Data = new Dictionary<string, Dictionary<string, Dictionary<string, PointSeries>>>();
            }
        }

        private class IntervalCache
        {
            public int SecondsAgo { get; set; }
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
            var t = new Dictionary<string, string> { { "host", host } };
            var url = GetMetricUrl(metric, "sum", t, DateTime.UtcNow.AddDays(-1), null, pointCount);
            return GetFromBosun<BosunMetricResponse>(url).Series.FirstOrDefault() ?? new PointSeries(host);
        }

        public override PointSeries GetSeries(string metric, string host, DateTime? start, DateTime? end, int? pointCount = null, params Tuple<string, string>[] tags)
        {
            var t = new Dictionary<string, string> { { "host", host } };
            var url = GetMetricUrl(metric, "sum", t, start, end, pointCount, DashboardMetric.IsCounter(metric));
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
                    _host = TryGetTagValue(Name, "host", out h) ? h : "Unknown";
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

        public string GetTagValue(string key)
        {
            string o;
            if (TryGetTagValue(Name, key, out o))
            {
                return o;
            }
            throw new KeyNotFoundException("key: " + key + ", in: " + Name);
        }

        /// <summary>
        /// Extract the tag value from the Name expression string
        /// 
        /// Example input/output, k = "host":
        ///     "this_should_fail"                            --> null
        ///     "os.net.bytes{host=}"                         --> ""
        ///     "os.net.bytes{host=fancy_machine}"            --> "fancy_machine"
        ///     "os.net.bytes{host=fancy_machine,iface=eth0}" --> "fancy_machine"
        /// </summary>
        private static bool TryGetTagValue(string s, string k, out string o)
        {
            o = null;
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
                if (k == kv[0])
                {
                    o = kv[1];
                    return true;
                }
            }
            return false;
        }
    }
}