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
        string ISearchableNode.DisplayName => PrettyName;
        string ISearchableNode.Name => PrettyName;
        string ISearchableNode.CategoryName => Category?.Name.Replace(" Servers", "") ?? "Unknown";
        
        public DashboardDataProvider DataProvider { get; set; }
        public bool IsRealTimePollable => MachineType?.Contains("Windows") == true;

        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public DateTime? LastSync { get; internal set; }
        public string MachineType { get; internal set; }
        public string Ip { get; internal set; }
        public short? PollIntervalSeconds { get; internal set; }

        public DateTime LastBoot { get; internal set; }
        public NodeStatus Status { get; internal set; }

        public short? CPULoad { get; internal set; }
        public float? TotalMemory { get; internal set; }
        public float? MemoryUsed { get; internal set; }
        public string VMHostID { get; internal set; }
        public bool IsVMHost { get; internal set; }
        public bool IsUnwatched { get; internal set; }
        public DateTime? UnwatchedFrom { get; internal set; }
        public DateTime? UnwatchedUntil { get; internal set; }

        public string Manufacturer { get; internal set; }
        public string Model { get; internal set; }
        public string ServiceTag { get; internal set; }
        public Version KernelVersion { get; internal set; }
        
        public string PrettyName => (Name ?? "").ToUpper();
        public TimeSpan UpTime => DateTime.UtcNow - LastBoot;
        public MonitorStatus MonitorStatus => Status.ToMonitorStatus();

        // TODO: Implement
        public string MonitorStatusReason => null;

        public bool IsVM => VMHostID.HasValue();
        public bool HasValidMemoryReading => MemoryUsed.HasValue && MemoryUsed >= 0;

        public Node VMHost { get; internal set; }

        public List<Node> VMs { get; internal set; }

        private DashboardCategory _category;
        public DashboardCategory Category
        {
            get { return _category ?? (_category = DashboardCategory.AllCategories.FirstOrDefault(c => c.PatternRegex.IsMatch(Name)) ?? DashboardCategory.Unknown); }
        }

        public string ManagementUrl { get; internal set; }

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
                          .Append(delim);
                    if (IPs != null)
                        result.Append(delim)
                              .Append(string.Join(",", IPs));
                    if (Apps != null)
                        result.Append(delim)
                              .Append(string.Join(",", Apps.Select(a => a.NiceName)));
                    if (IsVM && VMHost != null)
                        result.Append(delim)
                              .Append(VMHost.PrettyName);
                    if (IsVMHost && VMs != null)
                        result.Append(delim)
                              .Append(string.Join(",", VMs));

                    _searchString = result.ToString().ToLower();
                }
                return _searchString;
            }
        }

        public TimeSpan? PollInterval => PollIntervalSeconds.HasValue ? TimeSpan.FromSeconds(PollIntervalSeconds.Value) : (TimeSpan?) null;

        // Interfaces, Volumes and Applications are set by the provider
        public List<Interface> Interfaces { get; internal set; }
        public List<Volume> Volumes { get; internal set; }
        public List<Application> Apps { get; internal set; }

        public List<IPAddress> IPs { get; internal set; }

        public float? PercentMemoryUsed => MemoryUsed * 100 / TotalMemory;

        public float TotalNetworkbps
        {
            get { return Interfaces.Sum(i => i.InBps.GetValueOrDefault(0) + i.OutBps.GetValueOrDefault(0)); }
        }

        public float TotalPrimaryNetworkbps
        {
            get { return PrimaryInterfaces.Sum(i => i.InBps.GetValueOrDefault(0) + i.OutBps.GetValueOrDefault(0)); }
        }

        private DashboardSettings.NodeSettings _settings;
        public DashboardSettings.NodeSettings Settings => _settings ?? (_settings = Current.Settings.Dashboard.GetNodeSettings(PrettyName, Category.Settings));

        private List<Interface> _primaryInterfaces; 
        public IEnumerable<Interface> PrimaryInterfaces
        {
            get
            {
                if (_primaryInterfaces == null)
                {
                    var s = Settings;
                    List<Interface> dbInterfaces;
                    if (s?.PrimaryInterfacePatternRegex != null)
                    {
                        dbInterfaces = Interfaces.Where(i => s.PrimaryInterfacePatternRegex.IsMatch(i.FullName.IsNullOrEmptyReturn(i.Name))).ToList();
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