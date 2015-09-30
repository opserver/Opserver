using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Data.Dashboard;
using TeamCitySharp.DomainEntities;

namespace StackExchange.Opserver.Controllers
{
    public partial class GraphController
    {
        [OutputCache(Duration = 120, VaryByParam = "id;start;end;summary", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/cpu/json")]
        public async Task<ActionResult> CPUJson(string id, long start, long end, bool? summary = false)
        {
            var nodePoints = await DashboardData.GetCPUUtilization(id, start.ToDateTime(), end.ToDateTime(), 1000);
            if (nodePoints == null) return JsonNotFound();

            return Json(new
            {
                points = nodePoints.Select(p => new
                    {
                        date = p.DateTime.ToEpochTime(true), 
                        value = p.AvgLoad ?? 0
                    }),
                summary = summary.GetValueOrDefault(false) ? (await DashboardData.GetCPUUtilization(id, null, null, 2000)).Select(p => new
                    {
                        date = p.DateTime.ToEpochTime(true), 
                        value = p.AvgLoad ?? 0
                    }) : null,
                builds = !BuildStatus.HasCachePrimed ? null : GetBuilds(id, start, end).Select(b => new
                                                                {
                                                                    date = b.StartDate.ToEpochTime(true),
                                                                    text = GetFlagTooltip(b),
                                                                    link = b.WebUrl
                                                                })
            });
        }

        [OutputCache(Duration = 120, VaryByParam = "id;start;end;summary", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/memory/json")]
        public async Task<ActionResult> MemoryJson(string id, long start, long end, bool? summary = false)
        {

            var detailPoints = await DashboardData.GetMemoryUtilization(id, start.ToDateTime(), end.ToDateTime(), 1000);
            if (detailPoints == null) return JsonNotFound();

            return Json(new
            {
                points = detailPoints.Select(p => new
                    {
                        date = p.DateTime.ToEpochTime(true),
                        value = (int)(p.AvgMemoryUsed / 1024 / 1024 ?? 0)
                    }),
                summary = summary.GetValueOrDefault(false) ? (await DashboardData.GetMemoryUtilization(id, null, null, 1000)).Select(p => new
                    {
                        date = p.DateTime.ToEpochTime(true),
                        value = (int)(p.AvgMemoryUsed / 1024 / 1024 ?? 0)
                    }) : null,
                builds = !BuildStatus.HasCachePrimed ? null : GetBuilds(id, start, end).Select(b => new
                {
                    date = b.StartDate.ToEpochTime(true),
                    text = GetFlagTooltip(b),
                    link = b.WebUrl
                })
            });
        }

        [OutputCache(Duration = 120, VaryByParam = "id;start;end;summary", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/network/json")]
        public async Task<ActionResult> NetworkJson(string id, long start, long end, bool? summary = false)
        {
            var traffic = await DashboardData.GetInterfaceUtilization(id, start.ToDateTime(), end.ToDateTime(), 1000);
            if (traffic == null) return JsonNotFound();

            var anyTraffic = traffic.Any();

            return Json(new
                {
                    maximums = new
                        {
                            main_in = anyTraffic ? traffic.Max(i => (int)i.InAvgBps.GetValueOrDefault(0)) : 0,
                            main_out = anyTraffic ? traffic.Max(i => (int)i.OutAvgBps.GetValueOrDefault(0)) : 0
                        },
                    points = traffic.Select(i => new 
                        {
                            date = i.DateTime.ToEpochTime(true), 
                            main_in = (int)(i.InAvgBps.GetValueOrDefault()),
                            main_out = (int)(i.OutAvgBps.GetValueOrDefault())
                        }),
                    summary = summary.GetValueOrDefault()
                                  ? (await DashboardData.GetInterfaceUtilization(id, null, null, 2000)).Select(i => new
                                      {
                                          date = i.DateTime.ToEpochTime(true),
                                          main_in = (int)(i.InAvgBps.GetValueOrDefault()),
                                          main_out = (int)(i.OutAvgBps.GetValueOrDefault())
                                      })
                                  : null
                });
        }

        [OutputCache(Duration = 120, VaryByParam = "id;start;end", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/builds/json")]
        public ActionResult BuildsJson(string id, long start, long end)
        {
            return Json(new
            {
                builds = GetBuilds(id, start, end).Select(b => new
                {
                    date = b.StartDate.ToEpochTime(true),
                    text = GetFlagTooltip(b),
                    link = b.WebUrl
                })
            });
        }

        private static IEnumerable<Build> GetBuilds(string id, long startEpoch, long endEpoch)
        {
            if (!Current.Settings.TeamCity.Enabled) return Enumerable.Empty<Build>();

            // only show builds when zoomed in, say 5 days for starters?
            //TODO: Move this to a setting
            if((endEpoch - startEpoch) > TimeSpan.FromDays(30).TotalSeconds)
                return new List<Build>();

            var node = DashboardData.GetNodeById(id);
            DateTime start = startEpoch.ToDateTime(), end = endEpoch.ToDateTime();
            return BuildStatus.GetBuildsByServer(node.PrettyName).Where(b => b.StartDate >= start && b.StartDate <= end);
        }

        private static string GetFlagTooltip(Build b)
        {
            return $"{b.NiceProjectName()} - {b.NiceName()} #{b.Number}";
        }
    }
}