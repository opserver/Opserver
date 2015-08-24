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
        public async Task<ActionResult> CPUJson(int id, long start, long end, bool? summary = false)
        {
            var node = DashboardData.GetNodeById(id);
            if (node == null) return JsonNotFound();

            return Json(new
            {
                points = (await node.GetCPUUtilization(start.ToDateTime(), end.ToDateTime(), 1000)).Select(p => new
                    {
                        date = p.DateTime.ToEpochTime(true), 
                        value = p.AvgLoad ?? 0
                    }),
                summary = summary.GetValueOrDefault(false) ? (await node.GetCPUUtilization(null, null, 2000)).Select(p => new
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
        public async Task<ActionResult> MemoryJson(int id, long start, long end, bool? summary = false)
        {
            var node = DashboardData.GetNodeById(id);
            if (node == null) return JsonNotFound();

            return Json(new
            {
                points = (await node.GetMemoryUtilization(start.ToDateTime(), end.ToDateTime(), 1000)).Select(p => new
                    {
                        date = p.DateTime.ToEpochTime(true),
                        value = (int)(p.AvgMemoryUsed / 1024 / 1024 ?? 0)
                    }),
                summary = summary.GetValueOrDefault(false) ? (await node.GetMemoryUtilization(null, null, 1000)).Select(p => new
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
        public async Task<ActionResult> NetworkJson(int id, long start, long end, bool? summary = false)
        {
            var ni = DashboardData.GetInterfaceById(id);
            if (ni == null) return JsonNotFound();

            var traffic = (await ni.GetUtilization(start.ToDateTime(), end.ToDateTime(), 1000)).ToList();
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
                                  ? (await ni.GetUtilization(null, null, 2000)).Select(i => new
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
        public ActionResult BuildsJson(int id, long start, long end)
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

        private static IEnumerable<Build> GetBuilds(int id, long startEpoch, long endEpoch)
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