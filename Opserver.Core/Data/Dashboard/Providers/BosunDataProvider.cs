using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Jil;
using StackExchange.Profiling;
using static StackExchange.Opserver.Data.Dashboard.Providers.BosunMetric;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public partial class BosunDataProvider : DashboardDataProvider<BosunSettings>
    {
        public override bool HasData => NodeCache.HasData();
        public string Host => Settings.Host;
        public override int MinSecondsBetweenPolls => 5;
        public override string NodeType => "Bosun";

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return NodeCache;
                yield return DayCache;
            }
        }

        public BosunDataProvider(BosunSettings settings) : base(settings) { }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() { return null; }

        public override List<Node> AllNodes => NodeCache.Data ?? new List<Node>();

        private Cache<List<Node>> _nodeCache;
        public Cache<List<Node>> NodeCache => _nodeCache ?? (_nodeCache = ProviderCache(GetAllNodesAsync, 60, 4 * 60 * 60));

        private string GetUrl(string path)
        {
            // Note: Host is normalized with a trailing slash when settings are loaded
            return Host + path;
        }

        // ReSharper disable ClassNeverInstantiated.Local
        // ReSharper disable CollectionNeverUpdated.Local
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        private class BosunHost
        {
            public string Name { get; set; }
            public string Model { get; set; }
            public string Manufacturer { get; set; }
            public string SerialNumber { get; set; }
            public int? UptimeSeconds { get; set; }

            public CPUInfo CPU { get; set; }
            public MemoryInfo Memory { get; set; }
            public OSInfo OS { get; set; }
            public Dictionary<string, DiskInfo> Disks { get; set; }
            public Dictionary<string, InterfaceInfo> Interfaces { get; set; }
            public List<IncidentInfo> OpenIncidents { get; set; }
            public Dictionary<string, ICMPInfo> ICMPData { get; set; }
            public HardwareInfo Hardware { get; set; }

            public class CPUInfo
            {
                public float? PercentUsed { get; set; }
                public Dictionary<string, string> Processors { get; set; }
                public DateTime? StatsLastUpdated { get; set; }
            }

            public class MemoryInfo
            {
                public Dictionary<string, string> Modules { get; set; }
                public float? UsedBytes { get; set; }
                public float? TotalBytes { get; set; } 
            }

            public class OSInfo
            {
                public string Caption { get; set; }
                public string Version { get; set; }
            }

            public class DiskInfo
            {
                public float? UsedBytes { get; set; }
                public float? TotalBytes { get; set; }
                public DateTime StatsLastUpdated { get; set; }
            }

            public class InterfaceInfo
            {
                public string Name { get; set; }
                public string Description { get; set; }
                public string MAC { get; set; }
                public List<string> IPAddresses { get; set; }
                public string Master { get; set; }
                public DateTime? StatsLastUpdated { get; set; }

                public float? Inbps { get; set; }
                public float? Outbps { get; set; }
                public float? LinkSpeed { get; set; }
                // TODO
                public List<string> Members { get; set; }
                public string Type { get; set; }
            }

            public class IncidentInfo
            {
                public int IncidentID { get; set; } 
                public string AlertKey { get; set; }
                public string Status { get; set; }
                public string Subject { get; set; }
                public bool Silenced { get; set; }
            }

            public class ICMPInfo
            {
                public bool TimedOut { get; set; }
                public bool DNSResolved { get; set; }
                public float? RTTMS { get; set; }
            }

            public class HardwareInfo
            {
                public Dictionary<string, MemoryModuleInfo> Memory { get; set; }
                public Dictionary<string, ComponentInfo> ChassisComponents { get; set; }
                public StorageInfo Storage { get; set; }
                public Dictionary<string, TemperatureInfo> Temps { get; set; }
                public Dictionary<string, PowerSupplyInfo> PowerSupplies { get; set; }
                public BoardPowerInfo BoardPowerReading { get; set; }

                public class ComponentInfo
                {
                    public string Status { get; set; }
                }

                public class MemoryModuleInfo : ComponentInfo
                {
                    public string Size { get; internal set; }
                }

                public class StorageInfo
                {
                    public Dictionary<string, ControllerInfo> Controllers { get; set; } 
                    public Dictionary<string, PhysicalDiskInfo> PhysicalDisks { get; set; }
                    public Dictionary<string, VirtualDiskInfo> VirtualDisks { get; set; }
                    public Dictionary<string, ComponentInfo> Batteries { get; set; }

                    public class ControllerInfo : ComponentInfo
                    {
                        public string Name { get; set; }
                        public string SlotId { get; set; }
                        public string State { get; set; }
                        public string FirmwareVersion { get; set; }
                        public string DriverVersion { get; set; }
                    }

                    public class PhysicalDiskInfo : ComponentInfo
                    {
                        public string Name { get; set; }
                        public string Media { get; set; }
                        public string Capacity { get; set; }
                        public string VendorId { get; set; }
                        public string ProductId { get; set; }
                        public string Serial { get; set; }
                        public string Part { get; set; }
                        public string NegotatiedSpeed { get; set; }
                        public string CapableSpeed { get; set; }
                        public string SectorSize { get; set; }
                    }

                    public class VirtualDiskInfo : ComponentInfo
                    {
                        
                    }
                }

                public class TemperatureInfo : ComponentInfo
                {
                    public double Celsius { get; set; }
                }

                public class PowerSupplyInfo : ComponentInfo
                {
                    public double Amps { get; set; }
                    public double Volts { get; set; }
                }

                public class BoardPowerInfo
                {
                    public double Watts { get; set; }
                }
            }

        }
        // ReSharper restore CollectionNeverUpdated.Local
        // ReSharper restore ClassNeverInstantiated.Local
        // ReSharper restore UnusedAutoPropertyAccessor.Local

        public class BosunApiResult<T>
        {
            public T Result { get; internal set; }
            public string Error { get; internal set; }
            public bool Success => Error.IsNullOrEmpty();
        }

        public async Task<BosunApiResult<T>> GetFromBosunAsync<T>(string url)
        {
            using (MiniProfiler.Current.Step("Bosun Fetch"))
            using (MiniProfiler.Current.CustomTiming("bosun", url))
            using (var wc = new WebClient())
            {
                try
                {
                    using (var s = await wc.OpenReadTaskAsync(url).ConfigureAwait(false))
                    using (var sr = new StreamReader(s))
                    {
                        var result = JSON.Deserialize<T>(sr, Options.SecondsSinceUnixEpochExcludeNullsUtc);
                        return new BosunApiResult<T> { Result = result };
                    }
                }
                catch (DeserializationException de)
                {
                    Current.LogException(de);
                    return new BosunApiResult<T>
                    {
                        Error = $"Error deserializing response from bosun to {typeof(T).Name}: {de}. Details logged."
                    };
                }
                catch (Exception e)
                {
                    e.AddLoggedData("Url", url);
                    // TODO Log response in Data? Could be huge, likely truncate
                    Current.LogException(e);
                    return new BosunApiResult<T>
                    {
                        Error = $"Error fetching data from bosun: {e}. Details logged."
                    };
                }
            }
        }

        public async Task<List<Node>> GetAllNodesAsync()
        {
            using (MiniProfiler.Current.Step("Get Server Nodes"))
            { 
                var nodes = new List<Node>();

                var apiResponse = await GetFromBosunAsync<Dictionary<string, BosunHost>>(GetUrl("api/host")).ConfigureAwait(false);
                if (!apiResponse.Success) return nodes;

                var hostsDict = apiResponse.Result;

                foreach (var h in hostsDict.Values)
                {
                    Version kernelVersion;
                    // Note: we can't follow this pattern, we'll need to refresh existing nodes 
                    // not wholesale replace on poll
                    var n = new Node
                    {
                        Id = h.Name,
                        Name = h.Name,
                        Model = h.Model,
                        Ip = "scollector",
                        DataProvider = this,
                        Status = GetNodeStatus(h),
                        // TODO: Add Last Ping time to all providers
                        LastSync = h.CPU?.StatsLastUpdated,
                        CPULoad = (short?)h.CPU?.PercentUsed,
                        MemoryUsed = h.Memory?.UsedBytes,
                        TotalMemory = h.Memory?.TotalBytes,
                        Manufacturer = h.Manufacturer,
                        ServiceTag = h.SerialNumber,
                        MachineType = h.OS?.Caption,
                        KernelVersion = Version.TryParse(h.OS?.Version, out kernelVersion) ? kernelVersion : null,

                        Interfaces = h.Interfaces?.Select(hi => new Interface
                        {
                            Id = hi.Key,
                            NodeId = h.Name,
                            Name = hi.Value.Name.IsNullOrEmptyReturn($"Unknown: {hi.Key}"),
                            FullName = hi.Value.Name,
                            TypeDescription = hi.Value.Type,
                            Caption = hi.Value.Description,
                            PhysicalAddress = hi.Value.MAC,
                            IPs = hi.Value?.IPAddresses?.Select(ip =>
                            {
                                IPNet result;
                                return IPNet.TryParse(ip, out result) ? result.IPAddress : null;
                            }).Where(ip => ip != null).ToList(),
                            LastSync = hi.Value.StatsLastUpdated,
                            InBps = hi.Value.Inbps,
                            OutBps = hi.Value.Outbps,
                            Speed = hi.Value.LinkSpeed * 1000000,
                            TeamMembers = h.Interfaces?.Where(i => i.Value.Master == hi.Value.Name).Select(i => i.Key).ToList()
                        }).ToList(),
                        Volumes = h.Disks?.Select(hd => new Volume
                        {
                            Id = hd.Key,
                            Name = hd.Key,
                            NodeId = h.Name,
                            Caption = hd.Key,
                            Description = $"{hd.Key}",
                            LastSync = hd.Value.StatsLastUpdated,
                            Used = hd.Value.UsedBytes,
                            Size = hd.Value.TotalBytes,
                            Available = hd.Value.TotalBytes - hd.Value.UsedBytes,
                            PercentUsed = 100 * (hd.Value.UsedBytes / hd.Value.TotalBytes),
                        }).ToList(),
                        //Apps = new List<Application>(),
                        //VMs = new List<Node>()
                    };
                    var hs = new HardwareSummary();
                    if (h.CPU?.Processors != null)
                    {
                        foreach (var p in h.CPU.Processors)
                        {
                            hs.Processors.Add(new HardwareSummary.ProcessorInfo
                            {
                                Name = p.Key,
                                Description = p.Value
                            });
                        }
                    }

                    var hw = h.Hardware;
                    if (hw != null)
                    {
                        if (hw.ChassisComponents != null)
                        {
                            foreach (var c in hw.ChassisComponents)
                            {
                                hs.Components.Add(new HardwareSummary.ComponentInfo
                                {
                                    Name = c.Key.Replace("_"," "),
                                    Status = c.Value.Status
                                });
                            }
                        }
                        if (hw.Memory != null)
                        {
                            foreach (var m in hw.Memory)
                            {
                                hs.MemoryModules.Add(new HardwareSummary.MemoryModuleInfo
                                {
                                    Name = m.Key,
                                    Status = m.Value.Status,
                                    Size = m.Value.Size
                                });
                            }
                        }
                        if (hw.Storage != null)
                        {
                            var s = new HardwareSummary.StorageInfo();
                            if (hw.Storage.Controllers != null)
                            {
                                foreach (var c in hw.Storage.Controllers)
                                {
                                    s.Controllers.Add(new HardwareSummary.StorageInfo.ControllerInfo
                                    {
                                        Name = c.Value.Name,
                                        Status = c.Value.Status,
                                        State = c.Value.State,
                                        SlotId = c.Value.SlotId,
                                        FirmwareVersion = c.Value.FirmwareVersion,
                                        DriverVersion = c.Value.DriverVersion
                                    });
                                }
                            }
                            if (hw.Storage.PhysicalDisks != null)
                            {
                                foreach (var d in hw.Storage.PhysicalDisks)
                                {
                                    s.PhysicalDisks.Add(new HardwareSummary.StorageInfo.PhysicalDiskInfo
                                    {
                                        Name = d.Value.Name,
                                        CapableSpeed = d.Value.CapableSpeed,
                                        Capacity = d.Value.Capacity,
                                        Media = d.Value.Media,
                                        NegotatiedSpeed = d.Value.NegotatiedSpeed,
                                        Part = d.Value.Part,
                                        ProductId = d.Value.ProductId,
                                        SectorSize = d.Value.SectorSize,
                                        Serial = d.Value.Serial,
                                        Status = d.Value.Status,
                                        VendorId = d.Value.VendorId
                                    });
                                }
                            }
                            if (hw.Storage.VirtualDisks != null)
                            {
                                foreach (var d in hw.Storage.VirtualDisks)
                                {
                                    s.VirtualDisks.Add(new HardwareSummary.StorageInfo.VirtualDiskInfo
                                    {
                                        Name = d.Key,
                                        Status = d.Value.Status,
                                        // TODO: Add to Bosun
                                        // Size = d.Value.Size
                                    });
                                }
                            }
                            if (hw.Storage.Batteries != null)
                            {
                                foreach (var b in hw.Storage.Batteries)
                                {
                                    s.Batteries.Add(new HardwareSummary.ComponentInfo
                                    {
                                        Name = b.Key,
                                        Status = b.Value.Status
                                    });
                                }
                            }
                            hs.Storage = s;
                        }
                        if (hw.PowerSupplies != null)
                        {
                            foreach (var ps in hw.PowerSupplies)
                            {
                                hs.PowerSupplies.Add(new HardwareSummary.PowerSupplyInfo
                                {
                                    Name = ps.Key,
                                    Amps = ps.Value.Amps,
                                    Status = ps.Value.Status,
                                    Volts = ps.Value.Volts
                                });
                            }
                        }
                        if (hw.Temps != null)
                        {
                            foreach (var t in hw.Temps)
                            {
                                hs.Temps.Add(new HardwareSummary.TemperatureInfo
                                {
                                    Name = t.Key.Replace("_", " "),
                                    Status = t.Value.Status,
                                    Celsius = t.Value.Celsius
                                });
                            }
                        }
                        if (hw.BoardPowerReading != null)
                        {
                            hs.BoardPowerReading = new HardwareSummary.BoardPowerInfo
                            {
                                Watts = hw.BoardPowerReading.Watts
                            };
                        }
                        n.Hardware = hs;
                    }

                    n.Interfaces.ForEach(i => i.IsTeam = i.TeamMembers.Any());

                    if (h.UptimeSeconds.HasValue) // TODO: Check if online - maybe against ICMP data last?
                    {
                        n.LastBoot = DateTime.UtcNow.AddSeconds(-h.UptimeSeconds.Value);
                    }
                    n.SetReferences();
                    nodes.Add(n);
                }

                return nodes;

                // Nodes
                //    LastSync, 
                //    Cast(Status as int) Status,
                //    LastBoot,  
                //    IP_Address as Ip, 
                //    PollInterval as PollIntervalSeconds,
                //    Cast(vmh.NodeID as varchar(50)) as VMHostID, 
                //    Cast(IsNull(vh.HostID, 0) as Bit) IsVMHost,
                //    IsNull(UnManaged, 0) as IsUnwatched, // Silence

                // Interfaces
                //       InterfaceIndex [Index],
                //       LastSync,
                //       Comments,
                //       InterfaceAlias Alias,
                //       IfName,
                //       InterfaceTypeDescription TypeDescription,
                //       IsNull(UnManaged, 0) as IsUnwatched,
                //       UnManageFrom as UnwatchedFrom,
                //       UnManageUntil as UnwatchedUntil,
                //       Cast(Status as int) Status,
                //       InPps,
                //       OutPps,
                //       InterfaceMTU as MTU,
                //       InterfaceSpeed as Speed

                // Volumes
                //       LastSync,
                //       VolumeIndex as [Index],
                //       VolumeDescription as [Description],
                //       VolumeType as Type,

                // Applications
                //Select Cast(com.ApplicationID as varchar(50)) as Id, 
                //       Cast(NodeID as varchar(50)) as NodeId, 
                //       app.Name as AppName, 
                //       IsNull(app.Unmanaged, 0) as IsUnwatched,
                //       app.UnManageFrom as UnwatchedFrom,
                //       app.UnManageUntil as UnwatchedUntil,
                //       com.Name as ComponentName, 
                //       ccs.TimeStamp as LastUpdated,
                //       pe.PID as ProcessID, 
                //       ccs.ProcessName,
                //       ccs.LastTimeUp, 
                //       ccs.PercentCPU as CurrentPercentCPU,
                //       ccs.PercentMemory as CurrentPercentMemory,
                //       ccs.MemoryUsed as CurrentMemoryUsed,
                //       ccs.VirtualMemoryUsed as CurrentVirtualMemoryUsed,
                //       pe.AvgPercentCPU as PercentCPU, 
                //       pe.AvgPercentMemory as PercentMemory, 
                //       pe.AvgMemoryUsed as MemoryUsed, 
                //       pe.AvgVirtualMemoryUsed as VirtualMemoryUsed,
                //       pe.ErrorMessage
            }
        }

        private NodeStatus GetNodeStatus(BosunHost host)
        {
            if (host.OpenIncidents?.Count > 0)
                return NodeStatus.Warning;
            if (host.ICMPData?.Values.All(p => p.TimedOut) == true)
                return NodeStatus.Unreachable;
            return NodeStatus.Active;
        }

        public override string GetManagementUrl(Node node)
        {
            // TODO: UrlEncode
            return !Host.HasValue() ? null : $"http://{Host}/host?host={node.Id}&time=1d-ago";
        }

        public override Task<List<GraphPoint>> GetCPUUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return GetRecentAsync(node.Id, start, end, p => p?.CPU, Globals.CPU);
        }

        public override Task<List<GraphPoint>> GetMemoryUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return GetRecentAsync(node.Id, start, end, p => p?.Memory, Globals.MemoryUsed);
        }

        private async Task<List<GraphPoint>> GetRecentAsync(
            string id,
            DateTime? start,
            DateTime? end,
            Func<IntervalCache, Dictionary<string, PointSeries>> get,
            string metricName)
        {
            if (IsApproximatelyLast24Hrs(start, end))
            {
                PointSeries series = null;
                if (get(DayCache.Data)?.TryGetValue(id.NormalizeForCache(), out series) == true)
                    return series.PointData;
            }

            var apiResponse = await GetMetric(
                metricName,
                start.GetValueOrDefault(DateTime.UtcNow.AddYears(-1)),
                end,
                id).ConfigureAwait(false);
            return apiResponse?.Series?[0]?.PointData ?? new List<GraphPoint>();
        }

        public override async Task<List<DoubleGraphPoint>> GetNetworkUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            if (IsApproximatelyLast24Hrs(start, end))
            {
                List<PointSeries> series = null;
                var cache = DayCache.Data?.Network;
                if (cache?.TryGetValue(node.Id.NormalizeForCache(), out series) == true)
                {
                    var result = JoinNetwork(series);
                    if (result != null)
                        return result;
                }
            }

            var apiResponse = await GetMetric(
                Globals.NetBytes,
                start.GetValueOrDefault(DateTime.UtcNow.AddYears(-1)),
                end,
                node.Id,
                TagCombos.AllNetDirections).ConfigureAwait(false);

            return JoinNetwork(apiResponse?.Series) ?? new List<DoubleGraphPoint>();
        }

        public override async Task<List<GraphPoint>> GetUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var apiResponse = await GetMetric(
                Globals.DiskUsed,
                start.GetValueOrDefault(DateTime.UtcNow.AddYears(-1)),
                end,
                volume.NodeId,
                TagCombos.AllDisks).ConfigureAwait(false);

            return apiResponse?.Series?[0]?.PointData ?? new List<GraphPoint>();
        }

        public override async Task<List<DoubleGraphPoint>> GetUtilizationAsync(Interface nodeInteface, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var apiResponse = await GetMetric(
                InterfaceMetricName(nodeInteface),
                start.GetValueOrDefault(DateTime.UtcNow.AddYears(-1)),
                end,
                nodeInteface.NodeId,
                TagCombos.AllDirectionsForInterface(nodeInteface.Id)).ConfigureAwait(false);

            return JoinNetwork(apiResponse.Series) ?? new List<DoubleGraphPoint>();
        }

        /// <summary>
        /// Determines if the passed in dates are approximately the last 24 hours, 
        /// so that we can share the day cache more efficiently
        /// </summary>
        /// <param name="start">Start date of the range</param>
        /// <param name="end">Optional end date of the range</param>
        /// <param name="fuzzySeconds">How many seconds to allow on each side of *exactly* 24 hours ago to be a match</param>
        /// <returns></returns>
        public bool IsApproximatelyLast24Hrs(DateTime? start, DateTime? end, int fuzzySeconds = 90)
        {
            if (!start.HasValue) return false;
            if (Math.Abs((DateTime.UtcNow.AddDays(-1) - start.Value).TotalSeconds) <= fuzzySeconds)
            {
                return !end.HasValue || Math.Abs((DateTime.UtcNow - end.Value).TotalSeconds) <= fuzzySeconds;
            }
            return false;
        }

        private List<DoubleGraphPoint> JoinNetwork(List<PointSeries> allSeries)
        {
            var inData = allSeries?.FirstOrDefault(s => s.Tags[Tags.Direction] == TagValues.In)?.PointData;
            var outData = allSeries?.FirstOrDefault(s => s.Tags[Tags.Direction] == TagValues.Out)?.PointData;

            if (inData == null || outData == null)
                return null;

            return inData.Join(outData,
                i => i.DateEpoch,
                o => o.DateEpoch,
                (i, o) => new DoubleGraphPoint
                {
                    DateEpoch = i.DateEpoch,
                    Value = i.Value,
                    BottomValue = o.Value
                }).ToList();
        } 
    }
}
