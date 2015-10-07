using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Jil;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public class BosunDataProvider : DashboardDataProvider<BosunSettings>
    {
        public override bool HasData => NodeCache.HasData();
        public string Host => Settings.Host;
        public override int MinSecondsBetweenPolls => 5;
        public override string NodeType => "Bosun";

        public override IEnumerable<Cache> DataPollers
        {
            get { yield return NodeCache; }
        }

        public BosunDataProvider(BosunSettings settings) : base(settings) { }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() { return null; }

        public override List<Node> AllNodes => NodeCache.Data ?? new List<Node>();

        private Cache<List<Node>> _nodeCache;
        public Cache<List<Node>> NodeCache => _nodeCache ?? (_nodeCache = ProviderCache(GetAllNodes, 10));

        private string GetUrl(string path)
        {
            // Note: Host is normalized with a trailing slash when settings are loaded
            return Host + path;
        }

        // ReSharper disable ClassNeverInstantiated.Local
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

            public class CPUInfo
            {
                public float? PercentUsed { get; set; }
                public Dictionary<string, string> Processors { get; set; }
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
            }

            public class IncidentInfo
            {
                public int IncidentID { get; set; } 
                public string AlertKey { get; set; }
                public string Status { get; set; }
                public string Subject { get; set; }
                public bool Silenced { get; set; }
            }
        }
        // ReSharper restore ClassNeverInstantiated.Local


        public async Task<List<Node>> GetAllNodes()
        {
            using (MiniProfiler.Current.Step("Get Server Nodes"))
            using (var wc = new WebClient())
            {
                var nodes = new List<Node>();
                
                Dictionary<string, BosunHost> hostsDict;
                try
                {
                    using (var s = await wc.OpenReadTaskAsync(GetUrl("api/host")))
                    using (var sr = new StreamReader(s))
                    {
                        hostsDict = JSON.Deserialize<Dictionary<string, BosunHost>>(sr, Options.SecondsSinceUnixEpochUtc);
                    }
                }
                catch (DeserializationException de)
                {
                    Current.LogException(
                        de.AddLoggedData("Position", de.Position.ToString())
                          .AddLoggedData("Snippet After", de.SnippetAfterError));
                    return nodes;
                }

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
                        CPULoad = (short?) h.CPU?.PercentUsed,
                        MemoryUsed = h.Memory?.UsedBytes,
                        TotalMemory = h.Memory?.TotalBytes,
                        Manufacturer = h.Manufacturer,
                        ServiceTag = h.SerialNumber,
                        MachineType = h.OS?.Caption,
                        KernelVersion = Version.TryParse(h.OS?.Version, out kernelVersion) ? kernelVersion : null,
                        
                        Interfaces = h.Interfaces?.Select(hi => new Interface
                        {
                            Id =  h.Name + "-int-" + hi.Key,
                            NodeId = h.Name,
                            Name = hi.Value.Name.IsNullOrEmptyReturn($"Unknown: {hi.Key}"),
                            FullName = hi.Value.Name,
                            Caption = hi.Value.Description,
                            PhysicalAddress = hi.Value.MAC,
                            IPs = hi.Value?.IPAddresses?.Select(IPAddress.Parse).ToList(),
                            LastSync = hi.Value.StatsLastUpdated,
                            InBps = hi.Value.Inbps * 8,
                            OutBps = hi.Value.Outbps * 8,
                            Speed = hi.Value.LinkSpeed * 1000000
                        }).ToList(),
                        Volumes = h.Disks?.Select(hd => new Volume
                        {
                            Id = h.Name + "-vol-" + hd.Key,
                            Name = hd.Key,
                            NodeId = h.Name,
                            Caption = hd.Key,
                            Description = "Needs Description",
                            LastSync = hd.Value.StatsLastUpdated,
                            Used = hd.Value.UsedBytes,
                            Size = hd.Value.TotalBytes,
                            PercentUsed = 100 * (hd.Value.UsedBytes / hd.Value.TotalBytes),
                        }).ToList(),
                        //Apps = new List<Application>(),
                        //VMs = new List<Node>()
                    };

                    if (h.UptimeSeconds.HasValue) // TODO: Check if online - maybe against ICMP data last?
                    {
                        n.LastBoot = DateTime.UtcNow.AddSeconds(-h.UptimeSeconds.Value);
                    }

                    nodes.Add(n);
                }

                return nodes;
                
                //    LastSync, 
                //    Cast(Status as int) Status,
                //    LastBoot, 
                //    Coalesce(Cast(vm.CPULoad as smallint), n.CPULoad) as CPULoad, 
                //    MemoryUsed, 
                //    IP_Address as Ip, 
                //    PollInterval as PollIntervalSeconds,
                //    Cast(vmh.NodeID as varchar(50)) as VMHostID, 
                //    Cast(IsNull(vh.HostID, 0) as Bit) IsVMHost,
                //    IsNull(UnManaged, 0) as IsUnwatched, // Silence
                
                // Interfaces
                //Select Cast(InterfaceID as varchar(50)) as Id,
                //       Cast(NodeID as varchar(50)) as NodeId,
                //       InterfaceIndex [Index],
                //       LastSync,
                //       InterfaceName as Name,
                //       FullName,
                //       Caption,
                //       Comments,
                //       InterfaceAlias Alias,
                //       IfName,
                //       InterfaceTypeDescription TypeDescription,
                //       PhysicalAddress,
                //       IsNull(UnManaged, 0) as IsUnwatched,
                //       UnManageFrom as UnwatchedFrom,
                //       UnManageUntil as UnwatchedUntil,
                //       Cast(Status as int) Status,
                //       InBps,
                //       OutBps,
                //       InPps,
                //       OutPps,
                //       InPercentUtil,
                //       OutPercentUtil,
                //       InterfaceMTU as MTU,
                //       InterfaceSpeed as Speed

                // Volumes
                //Select Cast(VolumeID as varchar(50)) as Id,
                //       Cast(NodeID as varchar(50)) as NodeId,
                //       LastSync,
                //       VolumeIndex as [Index],
                //       FullName as Name,
                //       Caption,
                //       VolumeDescription as [Description],
                //       VolumeType as Type,
                //       VolumeSize as Size,
                //       VolumeSpaceUsed as Used,
                //       VolumeSpaceAvailable as Available,
                //       VolumePercentUsed as PercentUsed

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

        public override string GetManagementUrl(Node node)
        {
            // TODO: UrlEncode
            return !Host.HasValue() ? null : $"http://{Host}/host?host={node.Id}&time=1d-ago";
        }

        public override Task<List<Node.CPUUtilization>> GetCPUUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Task.FromResult(new List<Node.CPUUtilization>());
        }

        public override Task<List<Node.MemoryUtilization>> GetMemoryUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Task.FromResult(new List<Node.MemoryUtilization>());
        }

        public override Task<List<Volume.VolumeUtilization>> GetUtilization(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Task.FromResult(new List<Volume.VolumeUtilization>());
        }

        public override Task<List<Interface.InterfaceUtilization>> GetUtilization(Interface nodeInteface, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Task.FromResult(new List<Interface.InterfaceUtilization>());
        }
    }
}
