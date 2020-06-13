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
                    var cpuMetrics = metrics.GetValueOrDefault(new TimeSeriesKey(host.Name, CpuMetric));
                    var memoryMetrics = metrics.GetValueOrDefault(new TimeSeriesKey(host.Name, MemoryMetric));
                    if (cpuMetrics.Values.IsDefault || memoryMetrics.Values.IsDefault)
                    {
                        continue;
                    }

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
                                    var rxKey = new TimeSeriesKey(host.Name, InterfaceRxMetric, tags);
                                    var txKey = new TimeSeriesKey(host.Name, InterfaceTxMetric, tags);
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
                                    var usageKey = new TimeSeriesKey(host.Name, DiskUsageMetric, tags);
                                    var usageMetrics = metrics.GetValueOrDefault(usageKey, new TimeSeries(usageKey, ImmutableArray<GraphPoint>.Empty));
                                    return new Volume
                                    {
                                        Id = v,
                                        Name = v,
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

        public override Task<List<GraphPoint>> GetCPUUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            if (IsApproximatelyLast24Hrs(start, end))
            {
                var key = new TimeSeriesKey(node.Id, CpuMetric);
                if (MetricDayCache.Data.TryGetValue(key, out var timeSeries))
                {
                    return Task.FromResult(timeSeries.Values.ToList());
                }
            }

            return Task.FromResult(_emptyPoints);
        }

        public override Task<List<GraphPoint>> GetMemoryUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            if (IsApproximatelyLast24Hrs(start, end))
            {
                var key = new TimeSeriesKey(node.Id, MemoryMetric);
                if (MetricDayCache.Data.TryGetValue(key, out var timeSeries))
                {
                    return Task.FromResult(timeSeries.Values.ToList());
                }
            }

            return Task.FromResult(_emptyPoints);
        }

        public override Task<List<DoubleGraphPoint>> GetNetworkUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            if (IsApproximatelyLast24Hrs(start, end))
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

            return Task.FromResult(_emptyDoublePoints);
        }

        public override Task<List<DoubleGraphPoint>> GetPerformanceUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Task.FromResult(_emptyDoublePoints);
        }

        public override Task<List<DoubleGraphPoint>> GetUtilizationAsync(Interface iface, DateTime? start, DateTime? end, int? pointCount = null)
        {
            if (IsApproximatelyLast24Hrs(start, end))
            {
                var tags = new[]
                {
                    ("interface", iface.Id)
                }.ToImmutableDictionary(x => x.Item1, x => x.Item2);
                var rxKey = new TimeSeriesKey(iface.Node.Id, InterfaceRxMetric, tags);
                var txKey = new TimeSeriesKey(iface.Node.Id, InterfaceTxMetric, tags);
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

            return Task.FromResult(_emptyDoublePoints);
        }

        public override Task<List<GraphPoint>> GetUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            if (IsApproximatelyLast24Hrs(start, end))
            {
                var tags = new[]
                {
                    ("device", volume.Id)
                }.ToImmutableDictionary(x => x.Item1, x => x.Item2);
                var usageKey = new TimeSeriesKey(volume.Node.Id, InterfaceRxMetric, tags);
                var usage = ImmutableArray<GraphPoint>.Empty;
                if (MetricDayCache.Data.TryGetValue(usageKey, out var timeSeries))
                {
                    usage = timeSeries.Values;
                }

                return Task.FromResult(usage.ToList());
            }

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
