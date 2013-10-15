using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using StackExchange.Opserver.Data.Dashboard.Providers;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Node : IMonitorStatus, ISearchableNode
    {
        public DashboardDataProvider DataProvider { get; set; }
        
        string ISearchableNode.DisplayName { get { return PrettyName; } }
        string ISearchableNode.Name { get { return PrettyName; } }
        string ISearchableNode.CategoryName { get { return Category != null ? Category.Name.Replace(" Servers", "") : "Unknown"; } }

        public int Id { get; internal set; }
        public string Name { get; internal set; }
        public DateTime? LastSync { get; internal set; }
        public string MachineType { get; internal set; }
        public string Ip { get; internal set; }
        public Int16? PollIntervalSeconds { get; internal set; }

        public DateTime LastBoot { get; internal set; }
        public NodeStatus Status { get; internal set; }

        public Int16? CPULoad { get; internal set; }
        public Single? TotalMemory { get; internal set; }
        public Single? MemoryUsed { get; internal set; }
        public int? VMHostID { get; internal set; }
        public bool IsVMHost { get; internal set; }
        public bool IsUnwatched { get; internal set; }
        public DateTime? UnwatchedFrom { get; internal set; }
        public DateTime? UnwatchedUntil { get; internal set; }

        public string Manufacturer { get; internal set; }
        public string Model { get; internal set; }
        public string ServiceTag { get; internal set; }
        
        public string PrettyName { get { return (Name ?? "").ToUpper(); } }
        public TimeSpan UpTime { get { return DateTime.UtcNow - LastBoot; } }
        public MonitorStatus MonitorStatus { get { return Status.ToMonitorStatus(); } }
        // TODO: Implement
        public string MonitorStatusReason { get { return null; } }

        public bool IsVM { get { return VMHostID.HasValue; } }
        public bool HasValidMemoryReading { get { return MemoryUsed.HasValue && MemoryUsed >= 0; } }
        public Node VMHost
        {
            get { return IsVM && VMHostID.HasValue ? DataProvider.GetNode(VMHostID.Value) : null; }
        }
        public List<Node> VMs
        {
            get { return IsVMHost ? DataProvider.AllNodes.Where(s => s.VMHostID == Id).ToList() : new List<Node>(); }
        }

        private DashboardCategory _category;
        public DashboardCategory Category
        {
            get { return _category ?? (_category = DashboardCategory.AllCategories.FirstOrDefault(c => c.PatternRegex.IsMatch(Name)) ?? DashboardCategory.Unknown); }
        }

        public string ManagementUrl
        {
            get { return DataProvider.GetManagementUrl(this); }
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
                    result.Append(MachineType)
                          .Append(delim)
                          .Append(PrettyName)
                          .Append(delim)
                          .Append(Status)
                          .Append(delim)
                          .Append(Manufacturer)
                          .Append(delim)
                          .Append(Model)
                          .Append(delim)
                          .Append(ServiceTag)
                          .Append(delim)
                          .Append(string.Join(",", IPs))
                          .Append(delim)
                          .Append(string.Join(",", Apps.Select(a => a.NiceName)));
                    if (IsVM)
                        result.Append(delim)
                              .Append(VMHost.PrettyName);
                    if (IsVMHost)
                        result.Append(delim)
                              .Append(string.Join(",", DataProvider.AllNodes.Where(s => s.VMHostID == Id)));

                    _searchString = result.ToString().ToLower();
                }
                return _searchString;
            }
        }

        public TimeSpan? PollInterval
        {
            get { return PollIntervalSeconds.HasValue ? TimeSpan.FromSeconds(PollIntervalSeconds.Value) : (TimeSpan?) null; }
        }
        
        // Interfaces, Volumes and Applications are pulled from cache
        public IEnumerable<Interface> Interfaces
        {
            get { return DataProvider.AllInterfaces.Where(i => i.NodeId == Id && i.Status != NodeStatus.Unknown); }
        }
        public IEnumerable<Volume> Volumes
        {
            get { return DataProvider.AllVolumes.Where(v => v.NodeId == Id && v.IsDisk && v.Size > 0); }
        }
        public IEnumerable<Application> Apps
        {
            get { return DataProvider.AllApplications.Where(a => a.NodeId == Id); }
        }
        public IEnumerable<IPAddress> IPs
        {
            get { return DataProvider.GetIPsForNode(this); }
        }

        public Single? PercentMemoryUsed
        {
            get { return MemoryUsed * 100 / TotalMemory; }
        }

        public Single TotalNetworkbps
        {
            get { return Interfaces.Sum(i => i.InBps.GetValueOrDefault(0) + i.OutBps.GetValueOrDefault(0)); }
        }

        public Single TotalPrimaryNetworkbps
        {
            get { return PrimaryInterfaces.Sum(i => i.InBps.GetValueOrDefault(0) + i.OutBps.GetValueOrDefault(0)); }
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
                        dbInterfaces = Interfaces.Where(i => s.PrimaryInterfacePatternRegex.IsMatch(i.FullName)).ToList();
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
                                              : Interfaces.OrderByDescending(i => i.InBps + i.OutBps)).ToList();
                }
                return _primaryInterfaces;
            }
        }
    }
}