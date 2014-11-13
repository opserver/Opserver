using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI.DataVisualization.Charting;
using System.Xml.Schema;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Opserver.Data.SQL;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Controllers
{
    public partial class GraphController
    {
        private const int SparkHours = 24;

        [OutputCache(Duration = 120, VaryByParam = "host", VaryByContentEncoding = "gzip;deflate", VaryByCustom="highDPI")]
        [Route("graph/cpu/spark"), AlsoAllow(Roles.InternalRequest)]
        public ActionResult CPUSpark(string host)
        {
            MiniProfiler.Stop(true);
            var chart = GetSparkChart();
            var dataPoints = DashboardData.Current.GetSeries("os.cpu",
                host,
                secondsAgo: SparkHours*60*60,
                pointCount: 400).Data;

            var area = GetSparkChartArea(100);
            var avgCPU = GetSparkSeries("Avg Load");
            chart.Series.Add(avgCPU);

            foreach (var mp in dataPoints)
            {
                avgCPU.Points.Add(new DataPoint(mp[0].ToOADate(), mp[1]));
            }

            chart.ChartAreas.Add(area);

            return chart.ToResult();
        }

        [OutputCache(Duration = 120, VaryByParam = "host", VaryByContentEncoding = "gzip;deflate", VaryByCustom = "highDPI")]
        [Route("graph/memory/spark"), AlsoAllow(Roles.InternalRequest)]
        public ActionResult MemorySpark(string host)
        {
            MiniProfiler.Stop(true);
            var chart = GetSparkChart();
            var node = DashboardData.Current.GetNode(host);
            if (node == null) return JsonNotFound();
            var dataPoints = DashboardData.Current.GetSeries("os.mem.used",
                host,
                secondsAgo: SparkHours*60*60,
                pointCount: 400).Data;

            // TODO: Max Fallback
            var maxMem = node.TotalMemory.GetValueOrDefault(0);
            var maxGB = (int)Math.Ceiling((double)maxMem / _gb);

            var area = GetSparkChartArea(maxMem + (maxGB / 8) * _gb);
            var used = GetSparkSeries("Used");
            chart.Series.Add(used);

            foreach (var mp in dataPoints)
            {
                used.Points.Add(new DataPoint(mp[0].ToOADate(), mp[1]));
            }
            chart.ChartAreas.Add(area);

            return chart.ToResult();
        }

        [OutputCache(Duration = 120, VaryByParam = "host", VaryByContentEncoding = "gzip;deflate", VaryByCustom = "highDPI")]
        [Route("graph/network/spark"), AlsoAllow(Roles.InternalRequest)]
        public ActionResult NetworkSpark(string host)
        {
            MiniProfiler.Stop(true);
            var chart = GetSparkChart();
            // TODO: Only show teams if we have teams
            var dataPoints = DashboardData.Current.GetSeries(DashboardMetric.NetBytes, host,
                    secondsAgo: SparkHours*60*60,
                    pointCount: 400).Data;

            var area = GetSparkChartArea();
            var series = GetSparkSeries("Total");
            series.ChartType = SeriesChartType.StackedArea;
            chart.Series.Add(series);

            foreach (var np in dataPoints)
            {
                series.Points.Add(new DataPoint(np[0].ToOADate(), np[1]));
            }
            chart.DataManipulator.Group("SUM", 2, IntervalType.Minutes, series);

            chart.ChartAreas.Add(area);

            return chart.ToResult();
        }

        [OutputCache(Duration = 120, VaryByParam = "host;iface", VaryByContentEncoding = "gzip;deflate", VaryByCustom = "highDPI")]
        [Route("graph/interface/{direction}/spark")]
        public ActionResult InterfaceOutSpark(string host, string iface, string direction, int id)
        {
            MiniProfiler.Stop(true);
            var chart = GetSparkChart();
            var dataPoints = DashboardData.Current.GetSeries(DashboardMetric.NetBytes, host,
                SparkHours*60*60,
                (int) chart.Width.Value,
                Tuple.Create(DashboardTag.Interface, iface), Tuple.Create(DashboardTag.Direction, direction)).Data;

            var area = GetSparkChartArea();
            var series = GetSparkSeries("Bytes");
            chart.Series.Add(series);

            foreach (var np in dataPoints)
            {
                series.Points.Add(new DataPoint(np[0].ToOADate(), np[1]));
            }
            chart.ChartAreas.Add(area);

            return chart.ToResult();
        }

        [OutputCache(Duration = 120, VaryByParam = "node", VaryByContentEncoding = "gzip;deflate", VaryByCustom = "highDPI")]
        [Route("graph/sql/cpu/spark")]
        public ActionResult SQLCPUSpark(string node)
        {
            MiniProfiler.Stop(true);
            var instance = SQLInstance.Get(node);
            if (instance == null) return ContentNotFound("SQLNode not found with name = '" + node + "'");

            var chart = GetSparkChart(20, 100);
            var dataPoints = instance.CPUHistoryLastHour;

            var area = GetSparkChartArea(noLine: true);
            area.AxisY.Maximum = 100;
            area.AxisX.Minimum = DateTime.UtcNow.AddHours(-1).ToEpochTime();
            area.AxisX.Maximum = DateTime.UtcNow.ToEpochTime();
            var series = GetSparkSeries("PercentCPU");
            chart.Series.Add(series);

            if (dataPoints.HasData())
            {
                foreach (var cpu in dataPoints.Data)
                {
                    series.Points.Add(new DataPoint(cpu.EventTime.ToEpochTime(), cpu.ProcessUtilization));
                }
            }
            chart.ChartAreas.Add(area);

            return chart.ToResult();
        }

        private static ChartArea GetSparkChartArea(double? max = null, int? daysAgo = null, bool noLine = false)
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
                    Minimum = DateTime.UtcNow.AddDays(-(daysAgo ?? 1)).ToOADate(),
                    MajorGrid = { Enabled = false },
                    LineColor = ColorTranslator.FromHtml("#a3c0d7")
                }
            };

            if (max.HasValue)
                area.AxisY.Maximum = max.Value;
            if (noLine)
                area.AxisX.LineColor = Color.Transparent;

            return area;
        }

        private static Series GetSparkSeries(string name, Color? color = null)
        {
            color = color ?? Color.SteelBlue;
            return new Series(name)
                       {
                           ChartType = SeriesChartType.Area,
                           XValueType = ChartValueType.DateTime,
                           Color = ColorTranslator.FromHtml("#c6d5e2"),
                           EmptyPointStyle = { Color = Color.Transparent, BackSecondaryColor = Color.Transparent }
                       };
        }
    }
}