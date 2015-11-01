using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                return (date ?? valueIfNull).ToString("yyyy/MM/dd-HH:mm:ss");
            }

            public void AddQuery(string metric, string host = "*", bool counter = true)
            {
                queries.Add(new
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
                });
            }

            //{  
            //   "start":"1h-ago",
            //   "queries":[
            //      {  
            //         "aggregator":"sum",
            //         "metric":"os.cpu",
            //         "rate":false,
            //         "rateOptions":{  
            //         },
            //         "derivative":"auto",
            //         "ds":"",
            //         "dstime":"",
            //         "tags":{  
            //            "host":"*"
            //         },
            //         "downsample":""
            //      }
            //   ]
            //}
        }

        public async Task<BosunMetricResponse> RunTSDBQuery(TSDBQuery query, int? pointCount = null)
        {
            var json = JSON.SerializeDynamic(query, Options.ExcludeNullsUtc);
            var url = GetUrl($"api/graph?json={json}{(pointCount.HasValue ? "&autods=" + pointCount : "")}&autorate=0");
            var apiResult = await GetFromBosun<BosunMetricResponse>(url);
            return apiResult.Result;
        }

        private Cache<IntervalCache> _dayCache;
        public Cache<IntervalCache> DayCache
        {
            get
            {
                return _dayCache ?? (_dayCache = ProviderCache(async () =>
                {
                    var result = new IntervalCache(TimeSpan.FromDays(1));
                    Func<string, Task> addMetric = async metricName =>
                    {
                        var query = new TSDBQuery(result.StartTime);
                        query.AddQuery(metricName, counter: BosunMetric.IsCounter(metricName));
                        var apiResult = await RunTSDBQuery(query, 1000);
                        if (apiResult != null)
                            result.Series[metricName] = apiResult.Series.ToDictionary(s => s.Host);
                    };
                    
                    var c = addMetric(BosunMetric.Globals.CPUUsed);
                    var m = addMetric(BosunMetric.Globals.MemoryUsed);
                    var n = addMetric(BosunMetric.Globals.NetBytes);
                    await Task.WhenAll(c, m, n); // parallel baby!

                    return result;
                }, 60, 3600));
            }
        }
        
        public class IntervalCache
        {
            public TimeSpan TimeSpan { get; set; }
            public DateTime StartTime { get; set; }

            public Dictionary<string, PointSeries> CPU => Series[BosunMetric.Globals.CPUUsed];
            public Dictionary<string, PointSeries> Memory => Series[BosunMetric.Globals.MemoryUsed];
            public Dictionary<string, PointSeries> Network => Series[BosunMetric.Globals.NetBytes];

            public ConcurrentDictionary<string, Dictionary<string, PointSeries>> Series { get; set; }

            public IntervalCache(TimeSpan timespan)
            {
                TimeSpan = timespan;
                StartTime = DateTime.UtcNow - timespan;
                Series = new ConcurrentDictionary<string, Dictionary<string, PointSeries>>();
            }
        }
    }
    
    public class BosunMetric
    {
        public BosunMetricType? Type { get; set; }
        public string Unit { get; set; }
        public List<BosunMetricDescription> Description { get; set; }

        public class Globals
        {
            public const string CPUUsed = "os.cpu";
            public const string MemoryUsed = "os.mem.used";
            public const string NetBytes = "os.net.bytes";
            public const string DiskUsed = "os.disk.fs.space_used";
        }

        public static bool IsCounter(string metric)
        {
            switch (metric)
            {
                case Globals.CPUUsed:
                    return true;
                default:
                    return false;
            }
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
                    var match = HostRegex.Match(Name);
                    _host = match.Success ? match.Groups[1].Value : "Unknown";
                }
                return _host;
            }
            set { _host = value; }
        }

        public string Name { get; set; }
        public string Metric { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public List<float[]> Data { get; set; }

        private List<GraphPoint> _pointData;
        public List<GraphPoint> PointData => (_pointData ?? (_pointData = Data.Select(p => new GraphPoint()
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
