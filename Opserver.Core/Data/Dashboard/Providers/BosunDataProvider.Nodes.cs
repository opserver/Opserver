using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public partial class BosunDataProvider
    {
        private Cache<List<Node>> _nodeCache;
        public Cache<List<Node>> NodeCache => _nodeCache ?? (_nodeCache = ProviderCache(GetAllNodesAsync, 60, 4 * 60 * 60));

        private Cache<Dictionary<string, List<string>>> _nodeMetricCache;

        public Cache<Dictionary<string, List<string>>> NodeMetricCache
            => _nodeMetricCache ?? (_nodeMetricCache = ProviderCache(
                async () =>
                {
                    var response = await GetFromBosunAsync<Dictionary<string, List<string>>>(GetUrl("api/metric/host"))
                        .ConfigureAwait(false);
                    return response.Result ?? new Dictionary<string, List<string>>();
                }, 10*60, 4*60*60));

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
                    if (Current.Settings.Dashboard.ExcludePatternRegex?.IsMatch(h.Name) ?? false)
                        continue;

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
                        CPULoad = (short?) h.CPU?.PercentUsed,
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
                                return IPNet.TryParse(ip, out result) ? result : null;
                            }).Where(ip => ip != null).ToList() ?? new List<IPNet>(),
                            LastSync = hi.Value.StatsLastUpdated,
                            InBps = hi.Value.Inbps,
                            OutBps = hi.Value.Outbps,
                            Speed = hi.Value.LinkSpeed*1000000,
                            Status = NodeStatus.Active, // TODO: Implement
                            TeamMembers =
                                h.Interfaces?.Where(i => i.Value.Master == hi.Value.Name).Select(i => i.Key).ToList()
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
                            PercentUsed = 100*(hd.Value.UsedBytes/hd.Value.TotalBytes),
                        }).ToList(),
                        //Apps = new List<Application>(),
                        //VMs = new List<Node>()
                    };

                    if (h.OpenIncidents?.Count > 0)
                    {
                        n.Issues = h.OpenIncidents.Select(i => new Issue<Node>(n)
                        {
                            Title = n.PrettyName,
                            Date = i.LastAbnormalTime.ToDateTime(),
                            Description = i.Subject,
                            MonitorStatus = GetStatusFromString(i.Status)
                        }).ToList();
                    }

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
                                    Name = c.Key.Replace("_", " "),
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

                    if (h.UptimeSeconds.HasValue) // TODO: Check if online - maybe against ICMP data last?
                    {
                        n.LastBoot = DateTime.UtcNow.AddSeconds(-h.UptimeSeconds.Value);
                    }
                    n.SetReferences();
                    nodes.Add(n);
                }

                return nodes;
            }
        }

        private MonitorStatus GetStatusFromString(string status)
        {
            switch (status)
            {
                case "normal":
                    return MonitorStatus.Good;
                case "warning":
                    return MonitorStatus.Warning; // critical?
                default:
                //case "unknown":
                    return MonitorStatus.Unknown;
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
                public decimal? UsedBytes { get; set; }
                public decimal? TotalBytes { get; set; }
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
                public bool Active { get; set; }
                public string AlertKey { get; set; }
                public string Status { get; set; }
                public string Subject { get; set; }
                public bool Silenced { get; set; }
                public long StatusTime { get; set; }
                public long LastAbnormalTime { get; set; }
                public bool NeedsAck { get; set; }
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
    }
}
