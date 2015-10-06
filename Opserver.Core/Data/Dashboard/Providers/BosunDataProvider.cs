using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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

        private class BosunHost
        {
            public string Name { get; set; }
            public string Model { get; set; }
            public string Manufacturer { get; set; }
            public string SerialNumber { get; set; }

            public MemoryInfo Memory { get; set; }
            public OSInfo OS { get; set; }

            public class MemoryInfo
            {
                public Dictionary<string, string> Modules { get; set; }
                public long? Total { get; set; } 
            }

            public class OSInfo
            {
                public string Caption { get; set; }
                public string Version { get; set; }
            }
        }


        public async Task<List<Node>> GetAllNodes()
        {
            using (MiniProfiler.Current.Step("Get Server Nodes"))
            using (var wc = new WebClient())
            {
                var nodes = new List<Node>();

                // TODO: Convert to stream, just testing here
                var hostsJson = await wc.DownloadStringTaskAsync(GetUrl("api/host"));
                var hostsDict = Jil.JSON.Deserialize<Dictionary<string, BosunHost>>(hostsJson);

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
                        CPULoad = 0, // FAKE!
                        TotalMemory = h.Memory?.Total,
                        Manufacturer = h.Manufacturer,
                        ServiceTag = h.SerialNumber,
                        MachineType = h.OS?.Caption,
                        KernelVersion = Version.TryParse(h.OS?.Version, out kernelVersion) ? kernelVersion : null,
                        // TODO: Rip out all of this and give ?. love to null refs that result
                        Interfaces = new List<Interface>(),
                        Volumes = new List<Volume>(),
                        Apps = new List<Application>(),
                        IPs = new List<IPAddress>(),
                        VMs = new List<Node>()
                    };
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
