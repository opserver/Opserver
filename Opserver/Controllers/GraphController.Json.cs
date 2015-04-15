using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Data.Dashboard;
using TeamCitySharp.DomainEntities;

namespace StackExchange.Opserver.Controllers
{
    public partial class GraphController
    {
        [OutputCache(Duration = 120, VaryByParam = "host;start;end;summary", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/cpu/json")]
        public ActionResult CPUJson(string host, long start, long end, bool? summary = false)
        {
            return Json(new
            {
                points = DashboardData.Current.GetSeries(DashboardMetric.CPUUsed, host, start.ToDateTime(), end.ToDateTime(), pointCount: 1000).Data.Select(p => new
                    {
                        date = p[0], value = p[1]
                    }),
                summary = summary.GetValueOrDefault(false) ? DashboardData.Current.GetSeries(DashboardMetric.CPUUsed, host, null, null, pointCount: 2000).Data.Select(p => new
                    {
                        date = p[0], value = p[1]
                    }) : null
                    //,builds = !BuildStatus.HasCachePrimed ? null : GetBuilds(host, start, end).Select(b => new
                    //                                            {
                    //                                                date = b.StartDate.ToEpochTime(true),
                    //                                                text = GetFlagTooltip(b),
                    //                                                link = b.WebUrl
                    //                                            })
            });
        }

        [OutputCache(Duration = 120, VaryByParam = "host;start;end;summary", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/memory/json")]
        public ActionResult MemoryJson(string host, long start, long end, bool? summary = false)
        {
            return Json(new
            {
                points = DashboardData.Current.GetSeries(DashboardMetric.MemoryUsed, host, start.ToDateTime(), end.ToDateTime(), pointCount: 1000).Data.Select(p => new
                    {
                        date = p[0],
                        value = (long)(p[1] / 1024 / 1024)
                    }),
                summary = summary.GetValueOrDefault(false) ? DashboardData.Current.GetSeries(DashboardMetric.MemoryUsed, host, null, null, pointCount: 1000).Data.Select(p => new
                    {
                        date = p[0],
                        value = (long)(p[1] / 1024 / 1024)
                    }) : null
                //,builds = !BuildStatus.HasCachePrimed ? null : GetBuilds(host, start, end).Select(b => new
                //{
                //    date = b.StartDate.ToEpochTime(true),
                //    text = GetFlagTooltip(b),
                //    link = b.WebUrl
                //})
            });
        }

        [OutputCache(Duration = 120, VaryByParam = "host;iface;start;end;summary", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/network/json")]
        public ActionResult NetworkJson(string host, string iface, long start, long end, bool? summary = false)
        {
            return Json(new {});

            //var traffic = DashboardData.Current.GetSeries(host, iface, start.ToDateTime(), end.ToDateTime(), 1000).ToList();
            //var anyTraffic = traffic.Any();
            //return Json(new
            //    {
            //        maximums = new
            //            {
            //                main_in = anyTraffic ? traffic.Max(i => (int)i.Inbps) : 0,
            //                main_out = anyTraffic ? traffic.Max(i => (int)i.Outbps) : 0
            //            },
            //        points = traffic.Select(i => new 
            //            {
            //                date = i.Epoch,
            //                main_in = (int)(i.Inbps),
            //                main_out = (int)(i.Outbps)
            //            }),
            //        summary = summary.GetValueOrDefault()
            //                      ? DashboardData.Current.GetInterfaceUtilization(host, iface, null, null, 2000).Select(i => new
            //                          {
            //                              date = i.Epoch,
            //                              main_in = (int)(i.Inbps),
            //                              main_out = (int)(i.Outbps)
            //                          })
            //                      : null
            //    });
        }

        [OutputCache(Duration = 120, VaryByParam = "host;start;end", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/builds/json")]
        public ActionResult BuildsJson(string host, long start, long end)
        {
            return Json(new
            {
                builds = new string[] { }
                //GetBuilds(host, start, end).Select(b => new
                //{
                //    date = b.StartDate.ToEpochTime(true),
                //    text = GetFlagTooltip(b),
                //    link = b.WebUrl
                //})
            });
        }

        private static IEnumerable<Build> GetBuilds(string host, long startEpoch, long endEpoch)
        {
            if (!Current.Settings.TeamCity.Enabled) return Enumerable.Empty<Build>();

            // only show builds when zoomed in, say 5 days for starters?
            //TODO: Move this to a setting
            if((endEpoch - startEpoch) > TimeSpan.FromDays(30).TotalSeconds)
                return new List<Build>();

            DateTime start = startEpoch.ToDateTime(), end = endEpoch.ToDateTime();
            return BuildStatus.GetBuildsByServer(host).Where(b => b.StartDate >= start && b.StartDate <= end);
        }

        private static string GetFlagTooltip(Build b)
        {
            return string.Format("{0} - {1} #{2}", b.NiceProjectName(), b.NiceName(), b.Number);
        }
    }
}