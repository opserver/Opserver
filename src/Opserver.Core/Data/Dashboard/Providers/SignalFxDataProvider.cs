using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Opserver.Data.Dashboard.Providers
{
    public partial class SignalFxDataProvider : DashboardDataProvider<SignalFxSettings>
    {
        private readonly List<GraphPoint> _emptyPoints = new List<GraphPoint>(0);
        private readonly List<DoubleGraphPoint> _emptyDoublePoints = new List<DoubleGraphPoint>(0);
        private readonly ILogger _logger;

        public SignalFxDataProvider(DashboardModule module, SignalFxSettings settings, ILogger logger) : base(module, settings)
        {
            _logger = logger;
        }

        public override bool HasData => MetricDayCache.ContainsData && HostCache.ContainsData;
        public override int MinSecondsBetweenPolls => 5;
        public override string NodeType => "SignalFx";

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return MetricDayCache;
                //yield return MetricIntervalCache;
                yield return HostCache;
            }
        }

        public override List<Node> AllNodes
        {
            get
            {
                if (!HostCache.ContainsData || !MetricDayCache.ContainsData)
                {
                    return new List<Node>(0);
                }

                var hosts = HostCache.Data;
                var metrics = MetricDayCache.Data;
                var nodes = new List<Node>(hosts.Count);
                foreach (var host in hosts.Values)
                {
                    var cpuMetrics = metrics.GetValueOrDefault(new TimeSeriesKey(host.Name, CpuMetric));
                    var memoryMetrics = metrics.GetValueOrDefault(new TimeSeriesKey(host.Name, MemoryMetric));
                    var rxMetrics = metrics.GetValueOrDefault(new TimeSeriesKey(host.Name, InterfaceRxMetric));
                    var txMetrics = metrics.GetValueOrDefault(new TimeSeriesKey(host.Name, InterfaceTxMetric));
                    if (cpuMetrics.Values.IsDefault || memoryMetrics.Values.IsDefault || rxMetrics.Values.IsDefault || txMetrics.Values.IsDefault)
                    {
                        continue;
                    }

                    var physicalCpus = host.GetPropertyAsInt32("host_physical_cpus") ?? 0;
                    nodes.Add(new Node
                    {
                        Id = host.Name,
                        Name = host.Name,
                        DataProvider = this,
                        LastSync = host.LastUpdated,
                        CPULoad = cpuMetrics.Values.OrderByDescending(x => x.DateEpoch).Select(x => (short)x.Value).FirstOrDefault(),
                        MemoryUsed = memoryMetrics.Values.OrderByDescending(x => x.DateEpoch).Select(x => (float)x.Value).FirstOrDefault(),
                        TotalMemory = host.GetPropertyAsFloat("host_mem_total") * 1024,
                        MachineOSVersion = string.Join(
                            ' ',
                            host.Properties.GetValueOrDefault("host_linux_version", host.Properties.GetValueOrDefault("host_os_name")),
                            host.Properties.GetValueOrDefault("host_kernel_release", "")
                        ),
                        Hardware = new HardwareSummary
                        {
                            Processors = Enumerable.Range(0, physicalCpus)
                            .Select(
                                x => new HardwareSummary.ProcessorInfo
                                {
                                    Name = host.Properties.GetValueOrDefault("host_cpu_model", ""),
                                })
                                .ToList(),
                        },
                        Interfaces = new List<Interface>(),
                        Volumes = new List<Volume>(),
                        // TODO: grab incidents from SignalFx
                        Status = NodeStatus.Active,
                    }) ; ;
                }

                return nodes;
            }
        }

        public override Task<List<GraphPoint>> GetCPUUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var key = new TimeSeriesKey(node.Id, CpuMetric);
            if (MetricDayCache.Data.TryGetValue(key, out var timeSeries))
            {
                return Task.FromResult(timeSeries.Values.ToList());
            }

            return Task.FromResult(_emptyPoints);
        }

        public override Task<List<GraphPoint>> GetMemoryUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var key = new TimeSeriesKey(node.Id, MemoryMetric);
            if (MetricDayCache.Data.TryGetValue(key, out var timeSeries))
            {
                return Task.FromResult(timeSeries.Values.ToList());
            }

            return Task.FromResult(_emptyPoints);
        }

        public override Task<List<DoubleGraphPoint>> GetNetworkUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var rxKey = new TimeSeriesKey(node.Id, InterfaceRxMetric);
            var txKey = new TimeSeriesKey(node.Id, InterfaceTxMetric);
            var rx = ImmutableArray<GraphPoint>.Empty;
            var tx = ImmutableArray<GraphPoint>.Empty;
            if (MetricDayCache.Data.TryGetValue(rxKey, out var timeSeries))
            {
                rx = timeSeries.Values;
            }

            if (MetricDayCache.Data.TryGetValue(txKey, out timeSeries))
            {
                tx = timeSeries.Values;
            }

            var results = rx.Join(tx,
                i => i.DateEpoch,
                o => o.DateEpoch,
                (i, o) => new DoubleGraphPoint
                {
                    DateEpoch = i.DateEpoch,
                    Value = i.Value,
                    BottomValue = o.Value
                }).ToList();

            return Task.FromResult(results);
        }

        public override Task<List<DoubleGraphPoint>> GetPerformanceUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Task.FromResult(_emptyDoublePoints);
        }

        public override Task<List<DoubleGraphPoint>> GetUtilizationAsync(Interface iface, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Task.FromResult(_emptyDoublePoints);
        }

        public override Task<List<GraphPoint>> GetUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Task.FromResult(_emptyPoints);
        }

        public override Task<List<DoubleGraphPoint>> GetVolumePerformanceUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Task.FromResult(_emptyDoublePoints);
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus() => Enumerable.Empty<MonitorStatus>();
        protected override string GetMonitorStatusReason() => null;

        private class JsonEpochConverter : JsonConverter<DateTime>
        {
            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetInt64().FromEpochTime(fromMilliseconds: true);

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) => writer.WriteNumberValue(value.ToEpochTime(toMilliseconds: true));
        }
    }
}
