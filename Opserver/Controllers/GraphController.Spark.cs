using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.UI.DataVisualization.Charting;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Opserver.Data.SQL;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Controllers
{
    public partial class GraphController
    {
        private static DateTime SparkStart => DateTime.UtcNow.AddHours(-24);
        private static int SparkPoints => Current.ViewSettings.SparklineChartWidth * 2;

        [OutputCache(Duration = 120, VaryByParam = "id", VaryByContentEncoding = "gzip;deflate", VaryByCustom="highDPI")]
        [Route("graph/cpu/spark"), AlsoAllow(Roles.InternalRequest)]
        public async Task<ActionResult> CPUSpark(string id)
        {
            var chart = GetSparkChart(max: 100);
            var dataPoints = await GetPoints(id, DashboardData.GetCPUUtilization);
            AddPoints(chart, dataPoints, p => p.Value.GetValueOrDefault(0));

            return chart.ToResult();
        }

        [OutputCache(Duration = 120, VaryByParam = "id", VaryByContentEncoding = "gzip;deflate", VaryByCustom = "highDPI")]
        [Route("graph/memory/spark"), AlsoAllow(Roles.InternalRequest)]
        public async Task<ActionResult> MemorySpark(string id)
        {
            var node = DashboardData.GetNodeById(id);
            if (node?.TotalMemory == null) return ContentNotFound($"Could not determine total memory for '{id}'");

            var chart = GetSparkChart(max: node.TotalMemory);
            var dataPoints = await GetPoints(id, DashboardData.GetMemoryUtilization);
            AddPoints(chart, dataPoints, p => p.Value.GetValueOrDefault(0));

            return chart.ToResult();
        }

        [OutputCache(Duration = 120, VaryByParam = "id", VaryByContentEncoding = "gzip;deflate", VaryByCustom = "highDPI")]
        [Route("graph/network/spark"), AlsoAllow(Roles.InternalRequest)]
        public async Task<ActionResult> NetworkSpark(string id)
        {
            var chart = GetSparkChart();
            var dataPoints = await GetPoints(id, DashboardData.GetNetworkUtilization);
            AddPoints(chart, dataPoints, p => (p.Value + p.BottomValue).GetValueOrDefault(0));

            return chart.ToResult();
        }

        [OutputCache(Duration = 120, VaryByParam = "id", VaryByContentEncoding = "gzip;deflate", VaryByCustom = "highDPI")]
        [Route("graph/interface/{direction}/spark")]
        public async Task<ActionResult> InterfaceOutSpark(string direction, string id)
        {
            var chart = GetSparkChart();
            var dataPoints = await GetPoints(id, DashboardData.GetInterfaceUtilization);
            
            Func<DoubleGraphPoint, double> getter = p => p.Value.GetValueOrDefault(0);
            if (direction == "out") getter = p => p.BottomValue.GetValueOrDefault(0);
            AddPoints(chart, dataPoints, getter);

            return chart.ToResult();
        }

        [OutputCache(Duration = 120, VaryByParam = "node", VaryByContentEncoding = "gzip;deflate", VaryByCustom = "highDPI")]
        [Route("graph/sql/cpu/spark")]
        public ActionResult SQLCPUSpark(string node)
        {
            var instance = SQLInstance.Get(node);
            if (instance == null) return ContentNotFound($"SQLNode not found with name = '{node}'");

            var chart = GetSparkChart(height: 20, width: 100, max: 100);
            var dataPoints = instance.CPUHistoryLastHour;
            
            var area = chart.ChartAreas.First();
            area.AxisX.Minimum = DateTime.UtcNow.AddHours(-1).ToOADate();
            area.AxisX.Maximum = DateTime.UtcNow.ToOADate();
            area.AxisX.LineColor = Color.Transparent;

            if (dataPoints.HasData())
            {
                var series = chart.Series.First();
                foreach (var cpu in dataPoints.Data)
                {
                    series.Points.Add(new DataPoint(cpu.EventTime.ToOADate(), cpu.ProcessUtilization));
                }
            }

            return chart.ToResult();
        }

        private static Task<List<T>> GetPoints<T>(string id, Func<string, DateTime?, DateTime?, int?, Task<List<T>>> fetch)
        {
            return fetch(id, SparkStart, null, SparkPoints);
        }

        private static void AddPoints<T>(Chart chart, IEnumerable<T> points, Func<T, double> getValue) where T : IGraphPoint
        {
            var series = chart.Series.First();
            foreach (var p in points)
            {
                series.Points.Add(new DataPoint(p.DateEpoch.ToOLEDate(), getValue(p)));
            }
        }

        private Chart GetSparkChart(
            int height = Current.ViewSettings.SparklineChartHeight, 
            int width = Current.ViewSettings.SparklineChartWidth, 
            double? max = null)
        {
            if (Current.IsHighDPI)
            {
                height *= 2;
                width *= 2;
            }
            var chart = GetChart(height, width);
            var area = GetSparkChartArea(max);
            var series = new Series("Main")
            {
                ChartType = SeriesChartType.Area,
                XValueType = ChartValueType.DateTime,
                Color = ColorTranslator.FromHtml("#c6d5e2"),
                EmptyPointStyle = {Color = Color.Transparent, BackSecondaryColor = Color.Transparent}
            };
            chart.Series.Add(series);
            chart.ChartAreas.Add(area);
            return chart;
        }

        private static ChartArea GetSparkChartArea(double? max = null)
        {
            var area = new ChartArea("area")
            {
                BackColor = Color.Transparent,
                Position = new ElementPosition(0, 0, 100, 100),
                InnerPlotPosition = new ElementPosition(0, 0, 100, 100),
                AxisY =
                {
                    MaximumAutoSize = 100,
                    LabelStyle = { Enabled = false },
                    MajorGrid = { Enabled = false },
                    MajorTickMark = { Enabled = false },
                    LineColor = Color.Transparent,
                    LineDashStyle = ChartDashStyle.Dot,
                },
                AxisX =
                {
                    MaximumAutoSize = 100,
                    LabelStyle = { Enabled = false },
                    Maximum = DateTime.UtcNow.ToOADate(),
                    Minimum = SparkStart.ToOADate(),
                    MajorGrid = { Enabled = false },
                    LineColor = ColorTranslator.FromHtml("#a3c0d7")
                }
            };

            if (max.HasValue)
                area.AxisY.Maximum = max.Value;

            return area;
        }
    }
}