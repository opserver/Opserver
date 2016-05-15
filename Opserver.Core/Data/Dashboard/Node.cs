using System;
using System.Collections.Generic;
using System.Linq;
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
        public List<Issue<Node>> Issues { get; set; }

        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public DateTime? LastSync { get; internal set; }
        public string MachineType { get; internal set; }
        public string MachineTypePretty => MachineType?.Replace("Microsoft Windows ", "");
        public string Ip { get; internal set; }
        public short? PollIntervalSeconds { get; internal set; }

        public DateTime? LastBoot { get; internal set; }
        public NodeStatus Status { get; internal set; }
        public string StatusDescription { get; internal set; }

        public short? CPULoad { get; internal set; }
        public float? TotalMemory { get; internal set; }
        public float? MemoryUsed { get; internal set; }
        public string VMHostID { get; internal set; }
        public bool IsVMHost { get; internal set; }
        public bool IsUnwatched { get; internal set; }

        public string Manufacturer { get; internal set; }
        public string Model { get; internal set; }
        public string ServiceTag { get; internal set; }
        public Version KernelVersion { get; internal set; }
        
        public string PrettyName => (Name ?? "").ToUpper();
        public TimeSpan? UpTime => DateTime.UtcNow - LastBoot;
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
                    var result = StringBuilderCache.Get();
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

                    _searchString = result.ToStringRecycle().ToLower();
                }
                return _searchString;
            }
        }

        public TimeSpan? PollInterval => PollIntervalSeconds.HasValue ? TimeSpan.FromSeconds(PollIntervalSeconds.Value) : (TimeSpan?) null;

        // Interfaces, Volumes and Applications are set by the provider
        public List<Interface> Interfaces { get; internal set; }
        public List<Volume> Volumes { get; internal set; }
        public List<Application> Apps { get; internal set; }

        public Interface GetInterface(string id) => Interfaces.FirstOrDefault(i => i.Id == id);
        public Volume GetVolume(string id) => Volumes.FirstOrDefault(v => v.Id == id);
        public Application GetApp(string id) => Apps.FirstOrDefault(a => a.Id == id);

        private static readonly List<IPNet> EmptyIPs = new List<IPNet>(); 

        public List<IPNet> IPs => Interfaces?.SelectMany(i => i.IPs).ToList() ?? EmptyIPs;

        public float? PercentMemoryUsed => MemoryUsed * 100 / TotalMemory;

        public float TotalNetworkbps => Interfaces.Sum(i => i.InBps.GetValueOrDefault(0) + i.OutBps.GetValueOrDefault(0));
        public float TotalPrimaryNetworkbps => PrimaryInterfaces.Sum(i => i.InBps.GetValueOrDefault(0) + i.OutBps.GetValueOrDefault(0));

        private DashboardSettings.NodeSettings _settings;
        public DashboardSettings.NodeSettings Settings => _settings ?? (_settings = Current.Settings.Dashboard.GetNodeSettings(PrettyName));

        private decimal? GetSetting(Func<INodeSettings, decimal?> func) => func(Settings) ?? func(Category?.Settings) ?? func(Current.Settings.Dashboard);
        public decimal? CPUWarningPercent => GetSetting(i => i.CPUWarningPercent);
        public decimal? CPUCriticalPercent => GetSetting(i => i.CPUCriticalPercent);
        public decimal? MemoryWarningPercent => GetSetting(i => i.MemoryCriticalPercent);
        public decimal? MemoryCriticalPercent => GetSetting(i => i.MemoryCriticalPercent);
        public decimal? DiskWarningPercent => GetSetting(i => i.DiskWarningPercent);
        public decimal? DiskCriticalPercent => GetSetting(i => i.DiskCriticalPercent);
        

        private List<Interface> _primaryInterfaces; 
        public List<Interface> PrimaryInterfaces
        {
            get
            {
                if (_primaryInterfaces == null || (_primaryInterfaces.Count == 0 && Interfaces?.Count > 0))
                {
                    var pattern = Settings?.PrimaryInterfacePatternRegex;
                    var dbInterfaces = Interfaces.Where(i => i.IsLikelyPrimary(pattern)).ToList();
                    _primaryInterfaces = (dbInterfaces.Any()
                        ? dbInterfaces.OrderBy(i => i.Name)
                        : Interfaces.OrderByDescending(i => i.InBps + i.OutBps)).ToList();
                }
                return _primaryInterfaces;
            }
        }

        /// <summary>
        /// Should be called after a node is created to set parent referneces
        /// This allows interfaces, volumes, etc. to poll through the provider
        /// </summary>
        public void SetReferences()
        {
            Interfaces?.ForEach(i => i.Node = this);
            Volumes?.ForEach(v => v.Node = this);
            Apps?.ForEach(a => a.Node = this);
        }
    }
}