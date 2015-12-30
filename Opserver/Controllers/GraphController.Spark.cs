using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Opserver.Data.SQL;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Controllers
{
    public partial class GraphController
    {
        private const int SparkHours = 24;
        private static DateTime SparkStart => DateTime.UtcNow.AddHours(-SparkHours);
        private const int SparkPoints = 500;
        // TODO: Change to based on theme
        private static string Color => "#008cba";
        private static string AxisColor => "#f6f6f6";

        [OutputCache(Duration = 120, VaryByParam = "id", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/cpu/spark"), AlsoAllow(Roles.InternalRequest)]
        public async Task<ActionResult> CPUSparkSvg(string id)
        {
            var node = DashboardData.GetNodeById(id);
            if (node == null) return ContentNotFound();
            var points = await node.GetCPUUtilization(SparkStart, null, SparkPoints);
            return SparkSVG(points, 100, p => p.Value.GetValueOrDefault());
        }

        [OutputCache(Duration = 120, VaryByParam = "id", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/memory/spark"), AlsoAllow(Roles.InternalRequest)]
        public async Task<ActionResult> MemorySpark(string id)
        {
            var node = DashboardData.GetNodeById(id);
            if (node?.TotalMemory == null) return ContentNotFound($"Could not determine total memory for '{id}'");
            var points = await node.GetMemoryUtilization(SparkStart, null, SparkPoints);
            return SparkSVG(points, Convert.ToInt64(node.TotalMemory.GetValueOrDefault()), p => p.Value.GetValueOrDefault());
        }

        [OutputCache(Duration = 120, VaryByParam = "id", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/network/spark"), AlsoAllow(Roles.InternalRequest)]
        public async Task<ActionResult> NetworkSpark(string id)
        {
            var node = DashboardData.GetNodeById(id);
            if (node == null) return ContentNotFound();
            var points = await node.GetNetworkUtilization(SparkStart, null, SparkPoints);
            return SparkSVG(points, Convert.ToInt64(points.Max(p => p.Value + p.BottomValue).GetValueOrDefault()), p => (p.Value + p.BottomValue).GetValueOrDefault());
        }

        [OutputCache(Duration = 120, VaryByParam = "id;iid", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/interface/{direction}/spark"), AlsoAllow(Roles.InternalRequest)]
        public async Task<ActionResult> InterfaceSpark(string direction, string id, string iid)
        {
            var iface = DashboardData.GetNodeById(id)?.GetInterface(iid);
            if (iface == null) return ContentNotFound();
            var points = await iface.GetUtilization(SparkStart, null, SparkPoints);

            Func<DoubleGraphPoint, double> getter = p => p.Value.GetValueOrDefault(0);
            if (direction == "out") getter = p => p.BottomValue.GetValueOrDefault(0);

            return SparkSVG(points, Convert.ToInt64(points.Max(getter)), getter);
        }

        [OutputCache(Duration = 120, VaryByParam = "node", VaryByContentEncoding = "gzip;deflate")]
        [Route("graph/sql/cpu/spark")]
        public ActionResult SQLCPUSpark(string node)
        {
            var instance = SQLInstance.Get(node);
            if (instance == null) return ContentNotFound($"SQLNode not found with name = '{node}'");
            var start = DateTime.UtcNow.AddHours(-1);
            var points = instance.ResourceHistory.Data?.Where(p => p.EventTime >= start)
                         ?? Enumerable.Empty<SQLInstance.ResourceEvent>();

            return SparkSVG(points, 100, p => p.ProcessUtilization, start);
        }

        private FileResult SparkSVG<T>(IEnumerable<T> points, long max, Func<T, double> getVal, DateTime? start = null) where T : IGraphPoint
        {
            const int height = 50,
                      width = SparkPoints;
            long nowEpoch = DateTime.UtcNow.ToEpochTime(),
                 startEpoch = (start ?? SparkStart).ToEpochTime(),
                 divisor = max/50,
                 range = (nowEpoch - startEpoch)/width;

            var sb = new StringBuilder().AppendFormat(@"
<svg version=""1.1"" baseProfile=""full"" width=""{0}"" height=""{1}"" xmlns=""http://www.w3.org/2000/svg"">
  <line x1=""0"" y1=""{1}"" x2=""{0}"" y2=""{1}"" stroke=""{3}"" stroke-width=""1"" />
  <g fill=""{2}"" stroke=""none"">
    <path d=""M0 50 ", width.ToString(), height.ToString(), Color, AxisColor);
            foreach (var p in points)
            {
                sb.Append("L")
                  .Append((p.DateEpoch - startEpoch) / range).Append(" ")
                  .Append((height - getVal(p) / divisor).ToString("n1")).Append(" ");
            }
            sb.AppendFormat(@"L{0} {1} z""/>
   </g>
</svg>", width.ToString(), height.ToString());

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return new FileContentResult(bytes, "image/svg+xml");
        }
    }
}