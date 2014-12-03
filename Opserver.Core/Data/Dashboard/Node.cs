using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using StackExchange.Opserver.Data.Dashboard.Providers;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Node : IMonitorStatus, ISearchableNode
    {
        string ISearchableNode.DisplayName { get { return PrettyName; } }
        string ISearchableNode.Name { get { return PrettyName; } }
        string ISearchableNode.CategoryName { get { return Category != null ? Category.Name.Replace(" Servers", "") : "Unknown"; } }

        public string Name { get; internal set; }

        public string Host { get { return Name; } }

        public string Model { get; internal set; }
        public string ServiceTag { get; internal set; }
        public string Manufacturer { get; internal set; }
        public string SerialNumber { get; internal set; }

        public DateTime? LastBoot { get; set; }
        public DateTime? LastUpdated { get; internal set; }

        public OSInfo OS { get; set; }
        public CPUInfo CPU { get; set; }
        public MemoryInfo Memory { get; set; }
        public List<Disk> Disks { get; set; }

        [DataMember(Name = "Interfaces")]
        private Dictionary<string, Interface> _interfacesDict { get; set; }

        private List<Interface> _interfaces;

        public List<Interface> Interfaces
        {
            get
            {
                if (_interfaces == null)
                {
                    _interfaces = _interfacesDict != null ? _interfacesDict.ForEach(i => i.Value.Name = i.Key).Select(i => i.Value).ToList() : new List<Interface>();
                }
                return _interfaces;
            }
        }

        public IEnumerable<IPAddress> IPAddresses
        {
            get { return Interfaces.SelectMany(i => i.IPAddresses); }
        }
        
        public bool IsSilenced { get; set; }
        
        public int? CPULoad { get { return CPU.Used; } set { CPU.Used = value; } }

        public long? TotalMemory { get { return Memory.Total; } set { Memory.Total = value; } }
        public long? MemoryUsed { get { return Memory.Used; } set { Memory.Used = value; } }
        public long? Networkbps { get; set; }

        // TODO: VMs
        public string VMHost { get; internal set; }
        public bool IsVMHost { get; internal set; }
        
        public string PrettyName { get { return (Name ?? "").ToUpper(); } }
        // public TimeSpan UpTime { get { return DateTime.UtcNow - LastBoot; } }
        
        // TODO: Implement
        public MonitorStatus MonitorStatus { get { return MonitorStatus.Good; } }
        public string MonitorStatusReason { get { return null; } }

        public bool IsVM { get { return VMHost.HasValue(); } }

        public List<Node> VMs
        {
            get { return IsVMHost ? DashboardData.Current.AllNodes.Where(s => s.VMHost == Host).ToList() : new List<Node>(); }
        }

        private DashboardCategory _category;
        public DashboardCategory Category
        {
            get { return _category ?? (_category = DashboardCategory.AllCategories.FirstOrDefault(c => c.PatternRegex.IsMatch(Name)) ?? DashboardCategory.Unknown); }
        }

        public string ManagementUrl
        {
            get { return DashboardData.Current.GetManagementUrl(this); }
        }

        private string _searchString;
        public string SearchString
        {
            get
            {
                if (_searchString == null)
                {
                    var result = new StringBuilder();
                    const string delim = "-";
                    result.Append(PrettyName)
                          .Append(delim)
                          .Append(delim)
                          .Append(Manufacturer)
                          .Append(delim)
                          .Append(Model)
                          .Append(delim)
                          .Append(ServiceTag)
                          .Append(delim)
                          .Append(string.Join(",", IPAddresses));
                    if (IsVM)
                        result.Append(delim)
                              .Append(VMHost);
                    if (IsVMHost)
                        result.Append(delim)
                              .Append(string.Join(",", VMs.Select(h => h.Host)));

                    _searchString = result.ToString().ToLower();
                }
                return _searchString;
            }
        }
        
        public Single? PercentMemoryUsed
        {
            get { return MemoryUsed * 100 / TotalMemory; }
        }

        public long TotalNetworkbps
        {
            get { return Networkbps ?? Interfaces.Sum(i => i.LastTotalbps.GetValueOrDefault(0)); }
        }

        public long TotalPrimaryNetworkbps
        {
            get { return Networkbps ?? PrimaryInterfaces.Sum(i => i.LastTotalbps.GetValueOrDefault(0)); }
        }

        private DashboardSettings.NodeSettings _settings;
        public DashboardSettings.NodeSettings Settings { get { return _settings ?? (_settings = Current.Settings.Dashboard.GetNodeSettings(PrettyName, Category.Settings)); } }

        private List<Interface> _primaryInterfaces;
        public IEnumerable<Interface> PrimaryInterfaces
        {
            get
            {
                if (_primaryInterfaces == null)
                {
                    var s = Settings;
                    List<Interface> dbInterfaces;
                    if (s != null && s.PrimaryInterfacePatternRegex != null)
                    {
                        dbInterfaces = Interfaces.Where(i => s.PrimaryInterfacePatternRegex.IsMatch(i.Name)).ToList();
                    }
                    else
                    {
                        dbInterfaces = Interfaces.Where(i =>
                                                        i.Name.ToLower().EndsWith("team") ||
                                                        i.Name.ToLower().StartsWith("bond") ||
                                                        i.Name.Contains("Microsoft Network Adapter Multiplexor Driver"))
                                                 .ToList();
                    }
                    _primaryInterfaces = (dbInterfaces.Any()
                                              ? dbInterfaces.OrderBy(i => i.Name)
                                              : Interfaces.OrderByDescending(i => i.LastTotalbps)).ToList();
                }
                return _primaryInterfaces;
            }
        }

        public bool IsWindows { get { return OS != null && OS.Caption != null && OS.Caption.Contains("Windows"); } }

        public class CPUInfo
        {
            public int? Physical { get; set; }
            public int? Logical { get; set; }
            public int? Used { get; set; }
            public Dictionary<string, string> Processors { get; set; }
        }

        public class OSInfo
        {
            public string Version { get; set; }
            public string Caption { get; set; }
        }

        public class MemoryInfo
        {
            public long? Total { get; set; }
            public long? Used { get; set; }
            public Dictionary<string, string> Modules { get; set; }
        }


        public struct CPUUtilization
        {
            public long Epoch { get; internal set; }
            public float Load { get; internal set; }
        }

        public struct MemoryUtilization
        {
            public long Epoch { get; internal set; }
            public float Used { get; internal set; }
            public float Total { get; internal set; }
        }
    }
}