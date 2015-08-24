using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.UI.DataVisualization.Charting;
using StackExchange.Opserver.Data.HAProxy;

namespace StackExchange.Opserver.Controllers
{
    public partial class GraphController
    { 
        private static readonly Color AltRouteBackground = ColorTranslator.FromHtml("#fafafa");
        
        [OutputCache(Duration = 60 * 60, VaryByParam = "host;start;end;summary", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/haproxy/traffic/json")]
        public async Task<ActionResult> HAProxyTrafficJson(string host, long start, long end, bool? summary = false)
        {
            var traffic = await HAProxyTraffic.GetTrafficSummary(host, null, null);

            return Json(new
                {
                    summary = traffic.Select(t => new
                    {
                        date = t.CreationDate.ToEpochTime(true),
                        main_hits = t.Hits,
                        main_pages = t.PageHits
                    })
                    //points = traffic.Select(t => new
                    //    {
                    //        date = t.CreationDate.ToEpochTime(true),
                    //        main_hits = t.Hits,
                    //        main_pages = t.PageHits
                    //    }),
                    //summary = summary.GetValueOrDefault()
                    //              ? Data.HAProxy.HAProxyTraffic.GetTrafficSummary(host, null, null)
                    //                      .Select(t => new
                    //                          {
                    //                              date = t.CreationDate.ToEpochTime(true),
                    //                              main_hits = t.Hits,
                    //                              main_pages = t.PageHits
                    //                          })
                    //              : null
                });
        }

        [OutputCache(Duration = 20 * 60, VaryByParam = "route;days;host;height;width;alt", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/haproxy/route-hits")]
        public async Task<ActionResult> HAProxyRouteHits(string route, int days, string host, int? height = 70, int? width = 300, bool alt = false)
        {
            var dataPoints = await HAProxyTraffic.GetRouteData(route, days, host: host);

            var chart = GetChart(height, width);
            chart.BackColor = alt ? AltRouteBackground : Color.White;
            var area = GetRouteChartArea(alt);
            if (dataPoints.Count >= 2)
            {
                area.AxisX.Minimum= dataPoints.First().CreationDate.ToOADate();
                area.AxisX.Maximum = dataPoints.Last().CreationDate.ToOADate();
            }

            var hits = new Series("Total Hits")
            {
                ChartType = SeriesChartType.StackedArea,
                XValueType = ChartValueType.DateTime,
                Color = ColorTranslator.FromHtml("#c6d5e2")
            };
            chart.Series.Add(hits);

            foreach (var rt in dataPoints)
            {
                hits.Points.Add(new DataPoint(rt.CreationDate.ToOADate(), rt.Hits.GetValueOrDefault(0)));
            }

            chart.ChartAreas.Add(area);
            return chart.ToResult();
        }

        //[OutputCache(Duration = 20 * 60, VaryByParam = "route;days;host;height;width;alt", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/haproxy/route-performance")]
        public async Task<ActionResult> HaProxyRoutePerformance(string route, int days, string host, int? height = 70, int? width = 300, bool alt = false)
        {
            var dataPoints = await HAProxyTraffic.GetRouteData(route, days, host: host);

            var chart = GetChart(height, width);
            chart.BackColor = alt ? AltRouteBackground : Color.White;
            var area = GetRouteChartArea(alt);
            if (dataPoints.Count >= 2)
            {
                area.AxisX.Minimum = dataPoints.First().CreationDate.ToOADate();
                area.AxisX.Maximum = dataPoints.Last().CreationDate.ToOADate();
            }

            area.AxisY.StripLines.Add(new StripLine
            {
                BackColor = ColorTranslator.FromHtml("#22FFFFFF"),
                StripWidth = 25,
                Interval = 2 * 25,
                IntervalOffset = 0
            });

            Func<string, string, Series> getSeries = (name, color) =>
                {
                    var tColor = ColorTranslator.FromHtml(color);
                    //var bColor = Color.FromArgb(200, tColor);
                    var result = new Series(name)
                        {
                            ChartType = SeriesChartType.StackedArea,
                            XValueType = ChartValueType.DateTime,
                            //BackGradientStyle = GradientStyle.TopBottom,
                            Color = tColor,
                            //BackSecondaryColor = bColor
                        };
                    chart.Series.Add(result);
                    return result;
                };

            var tAsp = getSeries("ASP.Net", "#0E2A4C");
            var tSql = getSeries("SQL", "#143D65");
            var tRedis = getSeries("Redis", "#194D79");
            var tHTTP = getSeries("HTTP", "#1D5989");
            var tTagEngine = getSeries("Tag Engine", "#206396");
            var tOther = getSeries("Other", "#64B6D0");
            
            foreach (var rt in dataPoints)
            {
                tAsp.Points.Add(new DataPoint(rt.CreationDate.ToOADate(), (double)rt.AvgCalculatedAspNetDurationMs.GetValueOrDefault(0)));
                tSql.Points.Add(new DataPoint(rt.CreationDate.ToOADate(), (double)rt.AvgSqlDurationMs.GetValueOrDefault(0)));
                tRedis.Points.Add(new DataPoint(rt.CreationDate.ToOADate(), (double)rt.AvgRedisDurationMs.GetValueOrDefault(0)));
                tHTTP.Points.Add(new DataPoint(rt.CreationDate.ToOADate(), (double)rt.AvgHttpDurationMs.GetValueOrDefault(0)));
                tTagEngine.Points.Add(new DataPoint(rt.CreationDate.ToOADate(), (double)rt.AvgTagEngineDurationMs.GetValueOrDefault(0)));
                tOther.Points.Add(new DataPoint(rt.CreationDate.ToOADate(), (double)(rt.AvgCalculatedOtherDurationMs.GetValueOrDefault(0))));
            }

            chart.ChartAreas.Add(area);
            return chart.ToResult();
        }

        [Route("graph/haproxy/route-performance/json")]
        public async Task<ActionResult> HaProxyRoutePerformanceJson(string route, int days = 30, string host = null, bool? summary = false)
        {
            var dataPoints = await HAProxyTraffic.GetRouteData(route, summary.GetValueOrDefault() ? null : (int?)days, host: host);

            return Json(new
                {
                    //points = dataPoints.Where(dp => dp.CreationDate > DateTime.UtcNow.AddDays(-days)).Select(dp => new
                    //    {
                    //        date = dp.CreationDate.ToEpochTime(true),
                    //        other = dp.AvgCalculatedOtherDurationMs,
                    //        dot_net = dp.AvgCalculatedAspNetDurationMs,
                    //        sql = dp.AvgSqlDurationMs,
                    //        redis = dp.AvgRedisDurationMs,
                    //        http = dp.AvgHttpDurationMs,
                    //        tag_engine = dp.AvgTagEngineDurationMs,
                    //        hits = dp.Hits
                    //    }),
                    summary = dataPoints.Select(dp => new
                        {
                            date = dp.CreationDate.ToEpochTime(true),
                            other = dp.AvgCalculatedOtherDurationMs,
                            dot_net = dp.AvgCalculatedAspNetDurationMs,
                            sql = dp.AvgSqlDurationMs,
                            redis = dp.AvgRedisDurationMs,
                            http = dp.AvgHttpDurationMs,
                            tag_engine = dp.AvgTagEngineDurationMs,
                            hits = dp.Hits
                        })
                });
        }

        private static ChartArea GetRouteChartArea(bool alt)
        {
            var area = new ChartArea("area")
            {
                BackColor = alt ? AltRouteBackground : Color.White,
                Position = new ElementPosition(0, 0, 100, 100),
                InnerPlotPosition = new ElementPosition(0, 0, 100, 100),
                AxisY =
                {
                    Minimum = 0,
                    MaximumAutoSize = 100,
                    LabelStyle = { Enabled = true },
                    Interval = 10,
                    IntervalAutoMode = IntervalAutoMode.VariableCount,
                    MajorGrid = { Enabled = false },
                    MajorTickMark = { Enabled = false },
                    LineWidth = 0,
                    LineDashStyle = ChartDashStyle.Dot,
                },
                AxisX =
                {
                    MaximumAutoSize = 100,
                    LabelStyle = { Enabled = false },
                    LineWidth = 0,
                    MajorTickMark = { Enabled = false },
                    MajorGrid = { Enabled = false }
                }
            };
            
            return area;
        }
    }
}