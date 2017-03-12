using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using StackExchange.Opserver.Data.HAProxy;

namespace StackExchange.Opserver.Controllers
{
    public partial class GraphController
    {
        [OutputCache(Duration = 60 * 60, VaryByParam = "host;start;end;summary", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/haproxy/traffic/json")]
        public async Task<ActionResult> HAProxyTrafficJson(string host, bool? summary = false)
        {
            var traffic = await HAProxyTraffic.GetTrafficSummaryAsync(host, null, null).ConfigureAwait(false);

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
                    //              ? Data.HAProxy.HAProxyTraffic.GetTrafficSummaryAsync(host, null, null)
                    //                      .Select(t => new
                    //                          {
                    //                              date = t.CreationDate.ToEpochTime(true),
                    //                              main_hits = t.Hits,
                    //                              main_pages = t.PageHits
                    //                          })
                    //              : null
                });
        }

        [OutputCache(Duration = 20 * 60, VaryByParam = "route;days;host", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/haproxy/route-hits")]
        public async Task<ActionResult> HAProxyRouteHits(string route, int days, string host)
        {
            var dataPoints = await HAProxyTraffic.GetRouteDataAsync(route, days, host: host).ConfigureAwait(false);

            return Json(dataPoints);
        }

        [Route("graph/haproxy/route-performance")]
        public async Task<ActionResult> HaProxyRoutePerformance(string route, int days, string host)
        {
            var dataPoints = await HAProxyTraffic.GetRouteDataAsync(route, days, host: host).ConfigureAwait(false);

            return Json(dataPoints);
        }

        [Route("graph/haproxy/route-performance/json")]
        public async Task<ActionResult> HaProxyRoutePerformanceJson(string route, int days = 30, string host = null, bool? summary = false)
        {
            var dataPoints = await HAProxyTraffic.GetRouteDataAsync(route, summary.GetValueOrDefault() ? null : (int?)days, host: host).ConfigureAwait(false);

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
    }
}