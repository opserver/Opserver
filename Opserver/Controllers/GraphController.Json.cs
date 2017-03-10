using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using StackExchange.Opserver.Data.Dashboard;

namespace StackExchange.Opserver.Controllers
{
    public partial class GraphController
    {
        public static DateTime DefaultStart => DateTime.UtcNow.AddDays(-1);
        public static DateTime DefaultEnd => DateTime.UtcNow;

        [OutputCache(Duration = 120, VaryByParam = "id;start;end;summary", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/cpu/json")]
        public async Task<ActionResult> CPUJson(string id, long? start = null, long? end = null, bool? summary = false)
        {
            var node = DashboardModule.GetNodeById(id);
            if (node == null) return JsonNotFound();
            var data = await CPUData(node, start, end, summary);
            if (data == null) return JsonNotFound();

            return Json(data);
        }

        public static async Task<object> CPUData(Node node, long? start = null, long? end = null, bool? summary = false)
        {
            var points = await node.GetCPUUtilization(start?.ToDateTime() ?? DefaultStart, end?.ToDateTime() ?? DefaultEnd, 1000);
            if (points == null) return null;
            return new
            {
                points = points.Select(p => new
                {
                    date = p.DateEpoch,
                    value = p.Value ?? 0
                }),
                summary = summary.GetValueOrDefault(false) ? (await node.GetCPUUtilization(null, null, 2000)).Select(p => new
                {
                    date = p.DateEpoch,
                    value = p.Value ?? 0
                }) : null
            };
        }

        [OutputCache(Duration = 120, VaryByParam = "id;start;end;summary", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/memory/json")]
        public async Task<ActionResult> MemoryJson(string id, long? start = null, long? end = null, bool? summary = false)
        {
            var node = DashboardModule.GetNodeById(id);
            if (node == null) return JsonNotFound();
            var data = await MemoryData(node, start, end, summary);
            if (data == null) return JsonNotFound();

            return Json(data);
        }

        public static async Task<object> MemoryData(Node node, long? start = null, long? end = null, bool? summary = false)
        {
            var points = await node.GetMemoryUtilization(start?.ToDateTime() ?? DefaultStart, end?.ToDateTime() ?? DefaultEnd, 1000);
            if (points == null) return null;

            return new
            {
                points = points.Select(p => new
                {
                    date = p.DateEpoch,
                    value = (int)(p.Value / 1024 / 1024 ?? 0)
                }),
                summary = summary.GetValueOrDefault(false) ? (await node.GetMemoryUtilization(null, null, 1000)).Select(p => new
                {
                    date = p.DateEpoch,
                    value = (int)(p.Value / 1024 / 1024 ?? 0)
                }) : null
            };
        }

        [OutputCache(Duration = 120, VaryByParam = "id;iid;start;end;summary", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/network/json")]
        public async Task<ActionResult> NetworkJson(string id, string iid, long? start = null, long? end = null, bool? summary = false)
        {
            var iface = DashboardModule.GetNodeById(id)?.GetInterface(iid);
            if (iface == null) return JsonNotFound();
            var data = await NetworkData(iface, start, end, summary);
            if (data == null) return JsonNotFound();

            return Json(data);
        }

        public static async Task<object> NetworkData(Node n, long? start = null, long? end = null, bool? summary = false)
        {
            var traffic = await n.GetNetworkUtilization(start?.ToDateTime() ?? DefaultStart, end?.ToDateTime() ?? DefaultEnd, 1000);
            if (traffic == null) return null;

            var anyTraffic = traffic.Count > 0;

            return new
            {
                maximums = new
                {
                    main_in = anyTraffic ? traffic.Max(i => (long)i.Value.GetValueOrDefault(0)) : 0,
                    main_out = anyTraffic ? traffic.Max(i => (long)i.BottomValue.GetValueOrDefault(0)) : 0
                },
                points = traffic.Select(i => new
                {
                    date = i.DateEpoch,
                    main_in = (long)i.Value.GetValueOrDefault(),
                    main_out = (long)i.BottomValue.GetValueOrDefault()
                }),
                summary = summary.GetValueOrDefault()
                    ? (await n.GetNetworkUtilization(null, null, 2000)).Select(i => new
                    {
                        date = i.DateEpoch,
                        main_in = (long)i.Value.GetValueOrDefault(),
                        main_out = (long)i.BottomValue.GetValueOrDefault()
                    })
                    : null
            };
        }

        public static async Task<object> NetworkData(Interface iface, long? start = null, long? end = null, bool? summary = false)
        {
            var traffic = await iface.GetUtilization(start?.ToDateTime() ?? DefaultStart, end?.ToDateTime() ?? DefaultEnd, 1000);
            if (traffic == null) return null;

            var anyTraffic = traffic.Count > 0;

            return new
            {
                maximums = new
                {
                    main_in = anyTraffic ? traffic.Max(i => (long)i.Value.GetValueOrDefault(0)) : 0,
                    main_out = anyTraffic ? traffic.Max(i => (long)i.BottomValue.GetValueOrDefault(0)) : 0
                },
                points = traffic.Select(i => new
                {
                    date = i.DateEpoch,
                    main_in = (long)i.Value.GetValueOrDefault(),
                    main_out = (long)i.BottomValue.GetValueOrDefault()
                }),
                summary = summary.GetValueOrDefault()
                    ? (await iface.GetUtilization(null, null, 2000)).Select(i => new
                    {
                        date = i.DateEpoch,
                        main_in = (long)i.Value.GetValueOrDefault(),
                        main_out = (long)i.BottomValue.GetValueOrDefault()
                    })
                    : null
            };
        }

        [OutputCache(Duration = 120, VaryByParam = "id;iid;start;end;summary", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/volumePerformance/json")]
        public async Task<ActionResult> VolumePerformanceJson(string id, string iid, long? start = null, long? end = null, bool? summary = false)
        {
            var iface = DashboardModule.GetNodeById(id)?.GetVolume(iid);
            if (iface == null) return JsonNotFound();
            var data = await VolumePerformanceData(iface, start, end, summary);
            if (data == null) return JsonNotFound();

            return Json(data);
        }

        public static async Task<object> VolumePerformanceData(Node n, long? start = null, long? end = null, bool? summary = false)
        {
            var traffic = await n.GetVolumePerformanceUtilization(start?.ToDateTime() ?? DefaultStart, end?.ToDateTime() ?? DefaultEnd, 1000);
            if (traffic == null) return null;

            var anyTraffic = traffic.Count > 0;

            return new
            {
                maximums = new
                {
                    main_read = anyTraffic ? traffic.Max(i => (long)i.Value.GetValueOrDefault(0)) : 0,
                    main_write = anyTraffic ? traffic.Max(i => (long)i.BottomValue.GetValueOrDefault(0)) : 0
                },
                points = traffic.Select(i => new
                {
                    date = i.DateEpoch,
                    main_read = (long)i.Value.GetValueOrDefault(),
                    main_write = (long)i.BottomValue.GetValueOrDefault()
                }),
                summary = summary.GetValueOrDefault()
                    ? (await n.GetVolumePerformanceUtilization(null, null, 2000)).Select(i => new
                    {
                        date = i.DateEpoch,
                        main_read = (long)i.Value.GetValueOrDefault(),
                        main_write = (long)i.BottomValue.GetValueOrDefault()
                    })
                    : null
            };
        }

        public static async Task<object> VolumePerformanceData(Volume volume, long? start = null, long? end = null, bool? summary = false)
        {
            var traffic = await volume.GetPerformanceUtilization(start?.ToDateTime() ?? DefaultStart, end?.ToDateTime() ?? DefaultEnd, 1000);
            if (traffic == null) return null;

            var anyTraffic = traffic.Count > 0;

            return new
            {
                maximums = new
                {
                    main_read = anyTraffic ? traffic.Max(i => (long)i.Value.GetValueOrDefault(0)) : 0,
                    main_write = anyTraffic ? traffic.Max(i => (long)i.BottomValue.GetValueOrDefault(0)) : 0
                },
                points = traffic.Select(i => new
                {
                    date = i.DateEpoch,
                    main_read = (long)i.Value.GetValueOrDefault(),
                    main_write = (long)i.BottomValue.GetValueOrDefault()
                }),
                summary = summary.GetValueOrDefault()
                    ? (await volume.GetPerformanceUtilization(null, null, 2000)).Select(i => new
                    {
                        date = i.DateEpoch,
                        main_read = (long)i.Value.GetValueOrDefault(),
                        main_write = (long)i.BottomValue.GetValueOrDefault()
                    })
                    : null
            };
        }

        //[OutputCache(Duration = 120, VaryByParam = "id;start;end", VaryByContentEncoding = "gzip;deflate")]
        //[Route("graph/builds/json")]
        //public ActionResult BuildsJson(string id, long start, long end)
        //{
        //    return Json(new
        //    {
        //        builds = GetBuilds(id, start, end).Select(b => new
        //        {
        //            date = b.StartDate.ToEpochTime(true),
        //            text = GetFlagTooltip(b),
        //            link = b.WebUrl
        //        })
        //    });
        //}
    }
}