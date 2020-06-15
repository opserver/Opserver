using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
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
                    var cpuKey = new TimeSeriesKey(host.Name, Cpu.Metric);
                    var memoryKey = new TimeSeriesKey(host.Name, Memory.Metric);
                    var cpuMetrics = metrics.GetValueOrDefault(cpuKey, new TimeSeries(cpuKey, ImmutableArray<GraphPoint>.Empty));
                    var memoryMetrics = metrics.GetValueOrDefault(memoryKey, new TimeSeries(memoryKey, ImmutableArray<GraphPoint>.Empty));
                    var physicalCpus = host.GetPropertyAsInt32("host_physical_cpus") ?? 0;
                    var node = new Node
                    {
                        Id = host.Name,
                        Name = host.Name,
                        DataProvider = this,
                        LastSync = DateTime.UtcNow,
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
                        Interfaces = host.Interfaces
                            .Select(
                                i =>
                                {
                                    var tags = new[] {
                                        ("interface", i)
                                    }.ToImmutableDictionary(x => x.Item1, x => x.Item2);
                                    var rxKey = new TimeSeriesKey(host.Name, InterfaceRx.Metric, tags);
                                    var txKey = new TimeSeriesKey(host.Name, InterfaceTx.Metric, tags);
                                    var rxMetrics = metrics.GetValueOrDefault(rxKey, new TimeSeries(rxKey, ImmutableArray<GraphPoint>.Empty));
                                    var txMetrics = metrics.GetValueOrDefault(txKey, new TimeSeries(txKey, ImmutableArray<GraphPoint>.Empty));
                                    return new Interface
                                    {
                                        Id = i,
                                        Name = i,
                                        IPs = new List<IPNet>(0),
                                        TeamMembers = new List<string>(0),
                                        InBps = rxMetrics.Values.OrderByDescending(x => x.DateEpoch).Select(x => (float)x.Value).FirstOrDefault(),
                                        OutBps = txMetrics.Values.OrderByDescending(x => x.DateEpoch).Select(x => (float)x.Value).FirstOrDefault(),
                                    };
                                }
                        ).ToList(),
                        Volumes = host.Volumes
                            .Select(
                                v =>
                                {
                                    var tags = new[] {
                                        ("device", v)
                                    }.ToImmutableDictionary(x => x.Item1, x => x.Item2);
                                    var usageKey = new TimeSeriesKey(host.Name, DiskUsage.Metric, tags);
                                    var usageMetrics = metrics.GetValueOrDefault(usageKey, new TimeSeries(usageKey, ImmutableArray<GraphPoint>.Empty));
                                    return new Volume
                                    {
                                        Id = v,
                                        Name = v,
                                        Description = v,
                                        PercentUsed = usageMetrics.Values.OrderByDescending(x => x.DateEpoch).Select(x => (decimal)x.Value).FirstOrDefault(),
                                    };
                                }
                        ).ToList(),
                        // TODO: grab incidents from SignalFx
                        Status = NodeStatus.Active,
                    };
                    node.AfterInitialize();
                    nodes.Add(node);
                }

                return nodes;
            }
        }

        public override async Task<List<GraphPoint>> GetCPUUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            if (IsApproximatelyLast24Hrs(start, end))
            {
                var key = new TimeSeriesKey(node.Id, Cpu.Metric);
                if (MetricDayCache.Data.TryGetValue(key, out var timeSeries))
                {
                    return timeSeries.Values.ToList();
                }
            }

            var metrics = await GetMetricsAsync(
                node, start, end, Cpu
            );
            return metrics.SelectMany(x => x.Values).ToList();
        }

        public override async Task<List<GraphPoint>> GetMemoryUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            if (IsApproximatelyLast24Hrs(start, end))
            {
                var key = new TimeSeriesKey(node.Id, Memory.Metric);
                if (MetricDayCache.Data.TryGetValue(key, out var timeSeries))
                {
                    return timeSeries.Values.ToList();
                }
            }

            var metrics = await GetMetricsAsync(
                node, start, end, Memory
            );
            return metrics.SelectMany(x => x.Values).ToList();
        }

        public override async Task<List<DoubleGraphPoint>> GetNetworkUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var rxKey = new TimeSeriesKey(node.Id, InterfaceRx.Metric);
            var txKey = new TimeSeriesKey(node.Id, InterfaceTx.Metric);
            var metricsByKey = ImmutableDictionary<TimeSeriesKey, TimeSeries>.Empty;
            if (IsApproximatelyLast24Hrs(start, end))
            {
                metricsByKey = MetricDayCache.Data;
            }
            else
            {
                var metrics = await GetMetricsAsync(
                    node, start, end, InterfaceRxByHost, InterfaceTxByHost
                );

                metricsByKey = metrics.GroupBy(x => x.Key).ToImmutableDictionary(x => x.Key, x => x.First());
            }

            var rx = ImmutableArray<GraphPoint>.Empty;
            var tx = ImmutableArray<GraphPoint>.Empty;
            if (metricsByKey.TryGetValue(rxKey, out var timeSeries))
            {
                rx = timeSeries.Values;
            }

            if (metricsByKey.TryGetValue(txKey, out timeSeries))
            {
                tx = timeSeries.Values;
            }

            return rx.Join(tx,
                i => i.DateEpoch,
                o => o.DateEpoch,
                (i, o) => new DoubleGraphPoint
                {
                    DateEpoch = i.DateEpoch,
                    Value = i.Value,
                    BottomValue = o.Value
                }).ToList();
        }

        public override Task<List<DoubleGraphPoint>> GetPerformanceUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Task.FromResult(_emptyDoublePoints);
        }

        public override async Task<List<DoubleGraphPoint>> GetUtilizationAsync(Interface iface, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var tags = new[]
            {
                ("interface", iface.Id)
            }.ToImmutableDictionary(x => x.Item1, x => x.Item2);
            var rxKey = new TimeSeriesKey(iface.Node.Id, InterfaceRx.Metric, tags);
            var txKey = new TimeSeriesKey(iface.Node.Id, InterfaceTx.Metric, tags);
            var metricsByKey = ImmutableDictionary<TimeSeriesKey, TimeSeries>.Empty;
            if (IsApproximatelyLast24Hrs(start, end))
            {
                metricsByKey = MetricDayCache.Data;
            }
            else
            {
                var metrics = await GetMetricsAsync(
                    iface.Node, start, end, InterfaceRx, InterfaceTx
                );

                metricsByKey = metrics.GroupBy(x => x.Key).ToImmutableDictionary(x => x.Key, x => x.First());
            }

            var rx = ImmutableArray<GraphPoint>.Empty;
            var tx = ImmutableArray<GraphPoint>.Empty;
            if (metricsByKey.TryGetValue(rxKey, out var timeSeries))
            {
                rx = timeSeries.Values;
            }

            if (metricsByKey.TryGetValue(txKey, out timeSeries))
            {
                tx = timeSeries.Values;
            }

            return rx.Join(tx,
                i => i.DateEpoch,
                o => o.DateEpoch,
                (i, o) => new DoubleGraphPoint
                {
                    DateEpoch = i.DateEpoch,
                    Value = i.Value,
                    BottomValue = o.Value
                }).ToList();
        }

        public override async Task<List<GraphPoint>> GetUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var tags = new[]
            {
                ("device", volume.Id)
            }.ToImmutableDictionary(x => x.Item1, x => x.Item2);
            var usageKey = new TimeSeriesKey(volume.Node.Id, DiskUsage.Metric, tags);
            var metricsByKey = ImmutableDictionary<TimeSeriesKey, TimeSeries>.Empty;
            if (IsApproximatelyLast24Hrs(start, end))
            {
                metricsByKey = MetricDayCache.Data;
            }
            else
            {
                var metrics = await GetMetricsAsync(
                    volume.Node, start, end, DiskUsage
                );

                metricsByKey = metrics.GroupBy(x => x.Key).ToImmutableDictionary(x => x.Key, x => x.First());
            }

            if (metricsByKey.TryGetValue(usageKey, out var timeSeries))
            {
                return timeSeries.Values.ToList();
            }

            return _emptyPoints;
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
