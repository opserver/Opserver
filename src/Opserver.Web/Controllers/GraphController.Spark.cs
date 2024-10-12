using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Opserver.Data.Dashboard;
using Opserver.Data.SQL;
using Opserver.Helpers;
using Opserver.Models;

namespace Opserver.Controllers
{
    public partial class GraphController
    {
        private const int SparkGraphDuration = 600; // Seconds
        private const int SparkHeight = 50;
        private const int SparkPoints = 500;
        private const int SparkHours = 24;
        private static DateTime SparkStart => DateTime.UtcNow.AddHours(-SparkHours);
        // TODO: Change to based on theme
        private static string Color => "#008cba";
        private static string AxisColor => "#f6f6f6";

        private SQLModule Sql { get; }
        private DashboardModule Dashboard { get; }
        private IMemoryCache MemCache { get; }

        public GraphController(
            SQLModule sqlModule,
            DashboardModule dashboardModule,
            IMemoryCache cache,
            IOptions<OpserverSettings> settings) : base(settings)
        {
            Sql = sqlModule;
            Dashboard = dashboardModule;
            MemCache = cache;
        }

        [OnlyAllow(DashboardRoles.Viewer)]
        [ResponseCache(Duration = SparkGraphDuration, VaryByQueryKeys = new string[] { "id" }, Location = ResponseCacheLocation.Client)]
        [Route("graph/cpu/spark"), AlsoAllow(Roles.InternalRequest)]
        public async Task<ActionResult> CPUSparkSvg(string id)
        {
            var node = Dashboard.GetNodeById(id);
            if (node == null) return ContentNotFound();
            var points = await node.GetCPUUtilization(SparkStart, null, SparkPoints);

            return points.Count == 0
                ? EmptySparkSVG()
                : SparkSVG(points, 100, p => p.Value.GetValueOrDefault());
        }

        [OnlyAllow(DashboardRoles.Viewer)]
        [ResponseCache(Duration = SparkGraphDuration, Location = ResponseCacheLocation.Client)]
        [Route("graph/cpu/spark/all"), AlsoAllow(Roles.InternalRequest)]
        public Task<ActionResult> CPUSparkSvgAll()
        {
            return SparkSvgAll(
                "CPU",
                getPoints: n => n.GetCPUUtilization(SparkStart, null, SparkPoints),
                getMax: (_, __) => 100,
                getVal: p => p.Value.GetValueOrDefault());
        }

        [OnlyAllow(DashboardRoles.Viewer)]
        [ResponseCache(Duration = SparkGraphDuration, VaryByQueryKeys = new string[] { "id" }, Location = ResponseCacheLocation.Client)]
        [Route("graph/memory/spark"), AlsoAllow(Roles.InternalRequest)]
        public async Task<ActionResult> MemorySpark(string id)
        {
            var node = Dashboard.GetNodeById(id);
            if (node?.TotalMemory == null) return ContentNotFound($"Could not determine total memory for '{id}'");
            var points = await node.GetMemoryUtilization(SparkStart, null, SparkPoints);

            return points.Count == 0
                ? EmptySparkSVG()
                : SparkSVG(points, Convert.ToInt64(node.TotalMemory.GetValueOrDefault()), p => p.Value.GetValueOrDefault());
        }

        [OnlyAllow(DashboardRoles.Viewer)]
        [ResponseCache(Duration = SparkGraphDuration, Location = ResponseCacheLocation.Client)]
        [Route("graph/memory/spark/all"), AlsoAllow(Roles.InternalRequest)]
        public Task<ActionResult> MemorySparkSvgAll()
        {
            return SparkSvgAll(
                "Memory",
                getPoints: n => n.GetMemoryUtilization(SparkStart, null, SparkPoints),
                getMax: (n, _) => Convert.ToInt64(n.TotalMemory.GetValueOrDefault()),
                getVal: p => p.Value.GetValueOrDefault());
        }

        [OnlyAllow(DashboardRoles.Viewer)]
        [ResponseCache(Duration = SparkGraphDuration, VaryByQueryKeys = new string[] { "id" }, Location = ResponseCacheLocation.Client)]
        [Route("graph/network/spark"), AlsoAllow(Roles.InternalRequest)]
        public async Task<ActionResult> NetworkSpark(string id)
        {
            var node = Dashboard.GetNodeById(id);
            if (node == null) return ContentNotFound();
            var points = await node.GetNetworkUtilization(SparkStart, null, SparkPoints);

            return points.Count == 0
                ? EmptySparkSVG()
                : SparkSVG(points, Convert.ToInt64(points.Max(p => p.Value + p.BottomValue).GetValueOrDefault()), p => (p.Value + p.BottomValue).GetValueOrDefault());
        }

        [OnlyAllow(DashboardRoles.Viewer)]
        [ResponseCache(Duration = SparkGraphDuration, Location = ResponseCacheLocation.Client)]
        [Route("graph/network/spark/all"), AlsoAllow(Roles.InternalRequest)]
        public Task<ActionResult> NetworkSparkSvgAll()
        {
            return SparkSvgAll(
                "Network",
                getPoints: n => n.GetNetworkUtilization(SparkStart, null, SparkPoints),
                getMax: (_, points) => Convert.ToInt64(points.Max(p => p.Value + p.BottomValue).GetValueOrDefault()),
                getVal: p => (p.Value + p.BottomValue).GetValueOrDefault());
        }

        [OnlyAllow(DashboardRoles.Viewer)]
        [ResponseCache(Duration = 120, VaryByQueryKeys = new string[] { "id", "iid" }, Location = ResponseCacheLocation.Client)]
        [Route("graph/interface/{direction}/spark"), AlsoAllow(Roles.InternalRequest)]
        public async Task<ActionResult> InterfaceSpark(string direction, string id, string iid)
        {
            var iface = Dashboard.GetNodeById(id)?.GetInterface(iid);
            if (iface == null) return ContentNotFound();
            var points = await iface.GetUtilization(SparkStart, null, SparkPoints);

            if (points.Count == 0) return EmptySparkSVG();

            Func<DoubleGraphPoint, double> getter = p => p.Value.GetValueOrDefault(0);
            if (direction == "out") getter = p => p.BottomValue.GetValueOrDefault(0);

            return SparkSVG(points, Convert.ToInt64(points.Max(getter)), p => getter(p));
        }

        [OnlyAllow(DashboardRoles.Viewer)]
        [ResponseCache(Duration = 120, VaryByQueryKeys = new string[] { "id" }, Location = ResponseCacheLocation.Client)]
        [Route("graph/volumePerformance/spark"), AlsoAllow(Roles.InternalRequest)]
        public async Task<ActionResult> VolumeSpark(string id)
        {
            var node = Dashboard.GetNodeById(id);
            if (node == null) return ContentNotFound();
            var points = await node.GetVolumePerformanceUtilization(SparkStart, null, SparkPoints);

            return points.Count == 0
                ? EmptySparkSVG()
                : SparkSVG(points, Convert.ToInt64(points.Max(p => p.Value + p.BottomValue).GetValueOrDefault()), p => (p.Value + p.BottomValue).GetValueOrDefault());
        }

        [OnlyAllow(DashboardRoles.Viewer)]
        [ResponseCache(Duration = 120, VaryByQueryKeys = new string[] { "id", "iid" }, Location = ResponseCacheLocation.Client)]
        [Route("graph/volumePerformance/{direction}/spark"), AlsoAllow(Roles.InternalRequest)]
        public async Task<ActionResult> VolumeSpark(string direction, string id, string iid)
        {
            var volume = Dashboard.GetNodeById(id)?.GetVolume(iid);
            if (volume == null) return ContentNotFound();
            var points = await volume.GetPerformanceUtilization(SparkStart, null, SparkPoints);

            if (points.Count == 0) return EmptySparkSVG();

            Func<DoubleGraphPoint, double> getter = p => p.Value.GetValueOrDefault(0);
            if (direction == "write") getter = p => p.BottomValue.GetValueOrDefault(0);

            return SparkSVG(points, Convert.ToInt64(points.Max(getter)), p => getter(p));
        }

        [OnlyAllow(DashboardRoles.Viewer)]
        [ResponseCache(Duration = SparkGraphDuration, Location = ResponseCacheLocation.Client)]
        [Route("graph/volumePerformance/spark/all"), AlsoAllow(Roles.InternalRequest)]
        public Task<ActionResult> VolumeSparkSvgAll()
        {
            return SparkSvgAll(
                "Volume",
                getPoints: n => n.GetVolumePerformanceUtilization(SparkStart, null, SparkPoints),
                getMax: (_, points) => Convert.ToInt64(points.Max(p => p.Value + p.BottomValue).GetValueOrDefault()),
                getVal: p => (p.Value + p.BottomValue).GetValueOrDefault());
        }

        [OnlyAllow(SQLRoles.Viewer)]
        [ResponseCache(Duration = SparkGraphDuration, VaryByQueryKeys = new string[] { "node" }, Location = ResponseCacheLocation.Client)]
        [Route("graph/sql/cpu/spark")]
        public ActionResult SQLCPUSpark(string node)
        {
            var instance = Sql.GetInstance(node);
            if (instance == null) return ContentNotFound($"SQLNode not found with name = '{node}'");
            var start = DateTime.UtcNow.AddHours(-1);
            var points = instance.ResourceHistory.Data?.Where(p => p.EventTime >= start).ToList();

            if (points == null || points.Count == 0) return EmptySparkSVG();

            return SparkSVG(points, 100, p => p.ProcessUtilization, start);
        }

        [OnlyAllow(SQLRoles.Viewer)]
        [ResponseCache(Duration = SparkGraphDuration, VaryByQueryKeys = new string[] { "node" }, Location = ResponseCacheLocation.Client)]
        [Route("graph/sql/dtu/spark")]
        public ActionResult SQLDTUSpark(string node)
        {
            var instance = Sql.GetInstance(node);
            if (instance == null) return ContentNotFound($"SQLNode not found with name = '{node}'");
            var start = DateTime.UtcNow.AddHours(-1);
            var points = instance.AzureResourceHistory.Data?.Where(p => p.EventTime >= start).ToList();

            if (points == null || points.Count == 0) return EmptySparkSVG();

            return SparkSVG(points, 100, p => p.AvgDTUPercent, start);
        }

        public async Task<ActionResult> SparkSvgAll<T>(string key, Func<Node, Task<List<T>>> getPoints, Func<Node, List<T>, long> getMax, Func<T, double> getVal) where T : IGraphPoint
        {
            const int width = SparkPoints;

            // Filter to nodes we'd show, to trim on size
            var nodes = Dashboard.AllNodes.Where(n => n.Category != DashboardCategory.Unknown || Dashboard.Settings.ShowOther).ToList();
            // TODO: Better hashcode
            var bytes = await MemCache.GetSetAsync<byte[]>("AllNodesSVG-" + key + "-" + nodes.Count, async (_, __) =>
            {
                var overallHeight = nodes.Count * SparkHeight;

                long nowEpoch = DateTime.UtcNow.ToEpochTime(),
                    startEpoch = SparkStart.ToEpochTime();
                var range = (nowEpoch - startEpoch) / (float)width;

                var sb = StringBuilderCache.Get()
                    .AppendFormat("<svg version=\"1.1\" baseProfile=\"full\" width=\"{0}\" height=\"{1}\" xmlns=\"http://www.w3.org/2000/svg\" preserveAspectRatio=\"none\">\n", width.ToString(), overallHeight.ToString())
                    .AppendLine("\t<style>")
                    .AppendFormat("\t\tline {{ stroke:{0}; stroke-width:1 }}\n", AxisColor)
                    .AppendFormat("\t\tpath {{ fill:{0}; stroke:none; }}\n", Color)
                    .AppendLine("\t</style>");

                var pointsLookup = new ConcurrentDictionary<string, List<T>>();
                var maxLookup = new ConcurrentDictionary<string, long>();
                var lookups = new List<Task>(nodes.Count);
                foreach (var node in nodes)
                {
                    lookups.Add(getPoints(node).ContinueWith(t =>
                    {
                        if (!t.IsFaulted && !t.IsCanceled)
                        {
                            pointsLookup[node.Id] = t.Result;
                            maxLookup[node.Id] = getMax(node, t.Result);
                        }
                    }));
                }
                await Task.WhenAll(lookups);

                int currentYTop = 0;
                foreach (var pl in pointsLookup.OrderBy(pl => pl.Key))
                {
                    sb.AppendFormat("\t<view id=\"{0}\" viewBox=\"0 {1} {2} {3}\"/>\n", pl.Key, currentYTop, width, SparkHeight)
                      .AppendFormat("\t<g transform=\"translate(0, {0})\">\n", currentYTop)
                      .AppendFormat("\t\t<line x1=\"0\" y1=\"{0}\" x2=\"{1}\" y2=\"{0}\"/>\n", SparkHeight.ToString(), width.ToString())
                      .AppendFormat("\t\t<path d=\"M0 {0} L", SparkHeight);

                    var first = true;
                    long divisor = maxLookup[pl.Key] / SparkHeight;
                    foreach (var p in pl.Value)
                    {
                        var pos = (p.DateEpoch - startEpoch) / range;
                        if (first && pos > 0)
                        {
                            // TODO: Indicate a missing, ungraphed time portion?
                            sb.Append((pos - 1).ToString("f1", CultureInfo.InvariantCulture))
                              .Append(" ")
                              .Append(SparkHeight)
                              .Append(" ");
                            first = false;
                        }
                        sb.Append(pos.ToString("f1", CultureInfo.InvariantCulture)).Append(" ")
                          .Append((SparkHeight - (getVal(p) / divisor)).ToString("f1", CultureInfo.InvariantCulture)).Append(" ");
                    }
                    sb.Append(width)
                      .Append(" ")
                      .Append(SparkHeight)
                      .Append(" z\"/>\n")
                      .Append("\t</g>\n");

                    currentYTop += SparkHeight;
                }

                sb.Append("</svg>");
                return Encoding.UTF8.GetBytes(sb.ToStringRecycle());
            }, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(20));

            return new FileContentResult(bytes, "image/svg+xml");
        }

        private static FileResult SparkSVG<T>(IEnumerable<T> points, long max, Func<T, double> getVal, DateTime? start = null) where T : IGraphPoint
        {
            const int height = SparkHeight,
                      width = SparkPoints;
            long nowEpoch = DateTime.UtcNow.ToEpochTime(),
                startEpoch = (start ?? SparkStart).ToEpochTime(),
                divisor = max/50;
            var range = (nowEpoch - startEpoch)/(float)width;
            var first = true;

            var sb = StringBuilderCache.Get().AppendFormat(@"<svg version=""1.1"" baseProfile=""full"" width=""{0}"" height=""{1}"" xmlns=""http://www.w3.org/2000/svg"" preserveAspectRatio=""none"">
  <line x1=""0"" y1=""{1}"" x2=""{0}"" y2=""{1}"" stroke=""{3}"" stroke-width=""1"" />
  <path fill=""{2}"" stroke=""none"" d=""M0 50 L", width.ToString(), height.ToString(), Color, AxisColor);
            foreach (var p in points)
            {
                var pos = (p.DateEpoch - startEpoch)/range;
                if (first && pos > 0)
                {
                    // TODO: Indicate a missing, ungraphed time portion?
                    sb.Append((pos - 1).ToString("f1", CultureInfo.InvariantCulture))
                      .Append(" ")
                      .Append(height)
                      .Append(" ");
                    first = false;
                }
                sb.Append(pos.ToString("f1", CultureInfo.InvariantCulture)).Append(" ")
                  .Append((height - (getVal(p) / divisor)).ToString("f1", CultureInfo.InvariantCulture)).Append(" ");
            }
            sb.Append(width)
              .Append(" ")
              .Append(height)
              .Append(@" z""/>
</svg>");
            var bytes = Encoding.UTF8.GetBytes(sb.ToStringRecycle());
            return new FileContentResult(bytes, "image/svg+xml");
        }

        // No need to compute this more than once
        private static readonly byte[] EmptySvgBytes = Encoding.UTF8.GetBytes(string.Format(
            @"<svg version=""1.1"" baseProfile=""full"" width=""{1}"" height=""{0}"" xmlns=""http://www.w3.org/2000/svg"" preserveAspectRatio=""none"">
  <line x1=""0"" y1=""{0}"" x2=""{1}"" y2=""{0}"" stroke=""#f6f6f6"" stroke-width=""1"" />
</svg>", SparkHeight.ToString(), SparkPoints.ToString()));

        private static FileResult EmptySparkSVG() => new FileContentResult(EmptySvgBytes, "image/svg+xml");
    }
}
