using System;
using System.Collections.Generic;
using System.Linq;
using Opserver.Data.Dashboard.Providers;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Opserver.Data.Dashboard
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
        public string MachineOSVersion { get; internal set; }
        private string _machineTypePretty;
        public string MachineTypePretty => _machineTypePretty ??= GetPrettyMachineType();
        public string Ip { get; internal set; }
        public short? PollIntervalSeconds { get; internal set; }

        public DateTime? LastBoot { get; internal set; }
        public NodeStatus Status { get; internal set; }
        public NodeStatus? ChildStatus { get; internal set; }
        public string StatusDescription { get; internal set; }
        private HardwareType? _hardwareType;
        public HardwareType HardwareType => _hardwareType ??= GetHardwareType();

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

        public bool IsVM => VMHostID.HasValue() || (Manufacturer?.Contains("VMware") ?? false);
        public bool HasValidMemoryReading => MemoryUsed.HasValue && MemoryUsed >= 0;

        public Node VMHost { get; internal set; }

        public List<Node> VMs { get; internal set; }

        private DashboardCategory _category;
        public DashboardCategory Category =>
            _category ??= DataProvider.Module.AllCategories.Find(c => c.PatternRegex.IsMatch(Name)) ?? DashboardCategory.Unknown;
        private string GetPrettyMachineType()
        {
            if (MachineType?.StartsWith("Linux") ?? false) return MachineOSVersion.IsNullOrEmptyReturn("Linux");
            return MachineType?.Replace("Microsoft Windows ", "");
        }

        private HardwareType GetHardwareType()
        {
            if (IsVM) return HardwareType.VirtualMachine;
            return HardwareType.Physical;

            // TODO: Detect network gear in a reliable way
            //return HardwareType.Unknown;
        }

        public string ManagementUrl { get; internal set; }

        private string _searchString, _networkTextSummary, _applicationCPUTextSummary, _applicationMemoryTextSummary;

        public void ClearSummaries()
        {
            _searchString = _networkTextSummary = _applicationCPUTextSummary = _applicationMemoryTextSummary = null;
        }

        public string SearchString
        {
            get
            {
                if (_searchString == null)
                {
                    var result = StringBuilderCache.Get();

                    result.Append(MachineType)
                          .Pipend(PrettyName)
                          .Pipend(Status.ToString())
                          .Pipend(Manufacturer)
                          .Pipend(Model)
                          .Pipend(ServiceTag);

                    if (Hardware?.Processors != null)
                    {
                        foreach (var p in Hardware.Processors)
                        {
                            result.Pipend(p.Name)
                                  .Pipend(p.Description);
                        }
                    }
                    if (Hardware?.Storage?.Controllers != null)
                    {
                        foreach (var c in Hardware.Storage.Controllers)
                        {
                            result.Pipend(c.Name)
                                  .Pipend(c.FirmwareVersion)
                                  .Pipend(c.DriverVersion);
                        }
                    }
                    if (Hardware?.Storage?.PhysicalDisks != null)
                    {
                        foreach (var d in Hardware.Storage.PhysicalDisks)
                        {
                            result.Pipend(d.Media)
                                  .Pipend(d.ProductId)
                                  .Pipend(d.Serial)
                                  .Pipend(d.Part);
                        }
                    }
                    if (Interfaces != null)
                    {
                        foreach (var i in Interfaces)
                        {
                            result.Pipend(i.Name)
                                  .Pipend(i.Caption)
                                  .Pipend(i.PhysicalAddress)
                                  .Pipend(i.TypeDescription);

                            foreach (var ip in i.IPs)
                            {
                                result.Pipend(ip.ToString());
                            }
                        }
                    }
                    if (Apps != null)
                    {
                        foreach (var app in Apps)
                        {
                            result.Pipend(app.NiceName);
                        }
                    }
                    if (IsVM && VMHost != null)
                    {
                        result.Pipend(VMHost.PrettyName);
                    }
                    if (IsVMHost && VMs != null)
                    {
                        foreach (var vm in VMs)
                        {
                            result.Pipend(vm?.Name);
                        }
                    }
                    _searchString = result.ToStringRecycle();
                }
                return _searchString;
            }
        }

        public string NetworkTextSummary
        {
            get
            {
                if (_networkTextSummary != null) return _networkTextSummary;

                var sb = StringBuilderCache.Get();
                sb.Append("Total Traffic: ").Append(TotalPrimaryNetworkbps.ToSize("b")).AppendLine("/s");
                sb.AppendFormat("Interfaces ({0} total):", Interfaces.Count.ToString()).AppendLine();
                foreach (var i in PrimaryInterfaces.Take(5).OrderByDescending(i => i.InBps + i.OutBps))
                {
                    sb.AppendFormat("{0}: {1}/s\n(In: {2}/s, Out: {3}/s)\n", i.PrettyName,
                        (i.InBps.GetValueOrDefault(0) + i.OutBps.GetValueOrDefault(0)).ToSize("b"),
                        i.InBps.GetValueOrDefault(0).ToSize("b"), i.OutBps.GetValueOrDefault(0).ToSize("b"));
                }
                return _networkTextSummary = sb.ToStringRecycle();
            }
        }

        public string ApplicationCPUTextSummary
        {
            get
            {
                if (_applicationCPUTextSummary != null) return _applicationCPUTextSummary;

                if (Apps?.Any() != true) return _applicationCPUTextSummary = "";

                var sb = StringBuilderCache.Get();
                sb.AppendFormat("Total App Pool CPU: {0:0.##} %\n", Apps.Sum(a => a.PercentCPU.GetValueOrDefault(0)).ToString(CultureInfo.CurrentCulture));
                sb.AppendLine("App Pools:");
                foreach (var a in Apps.OrderBy(a => a.NiceName))
                {
                    sb.AppendFormat("  {0}: {1:0.##} %\n", a.NiceName, a.PercentCPU?.ToString(CultureInfo.CurrentCulture));
                }
                return _applicationCPUTextSummary = sb.ToStringRecycle();
            }
        }

        public string ApplicationMemoryTextSummary
        {
            get
            {
                if (_applicationMemoryTextSummary != null) return _applicationMemoryTextSummary;

                if (Apps?.Any() != true) return _applicationMemoryTextSummary = "";

                var sb = StringBuilderCache.Get();
                sb.AppendFormat("Total App Pool Memory: {0}\n", Apps.Sum(a => a.MemoryUsed.GetValueOrDefault(0)).ToSize());
                sb.AppendLine("App Pools:");
                foreach (var a in Apps.OrderBy(a => a.NiceName))
                {
                    sb.AppendFormat("  {0}: {1}\n", a.NiceName, a.MemoryUsed.GetValueOrDefault(0).ToSize());
                }
                return _applicationMemoryTextSummary = sb.ToStringRecycle();
            }
        }

        public TimeSpan? PollInterval => PollIntervalSeconds.HasValue ? TimeSpan.FromSeconds(PollIntervalSeconds.Value) : (TimeSpan?) null;

        // Interfaces, Volumes, Applications, and Services are set by the provider
        public List<Interface> Interfaces { get; internal set; }
        public List<Volume> Volumes { get; internal set; }
        public List<Application> Apps { get; internal set; }
        public List<NodeService> Services { get; internal set; }

        public Interface GetInterface(string id)
        {
            foreach (var i in Interfaces)
            {
                if (i.Id == id) return i;
            }
            return null;
        }

        public Volume GetVolume(string id)
        {
            foreach (var v in Volumes)
            {
                if (v.Id == id) return v;
            }
            return null;
        }

        public Application GetApp(string id)
        {
            foreach (var a in Apps)
            {
                if (a.Id == id) return a;
            }
            return null;
        }

        public NodeService GetService(string id)
        {
            foreach (var s in Services)
            {
                if (s.Id == id) return s;
            }
            return null;
        }

        private static readonly List<IPNet> EmptyIPs = new List<IPNet>();

        public List<IPNet> IPs => Interfaces?.SelectMany(i => i.IPs).ToList() ?? EmptyIPs;

        public float? PercentMemoryUsed => MemoryUsed * 100 / TotalMemory;

        public float TotalNetworkbps => Interfaces?.Sum(i => i.InBps.GetValueOrDefault(0) + i.OutBps.GetValueOrDefault(0)) ?? 0;
        public float TotalPrimaryNetworkbps => PrimaryInterfaces.Sum(i => i.InBps.GetValueOrDefault(0) + i.OutBps.GetValueOrDefault(0));
        public float TotalVolumePerformancebps => Volumes?.Sum(i => i.ReadBps.GetValueOrDefault(0) + i.WriteBps.GetValueOrDefault(0)) ?? 0;

        private DashboardSettings.NodeSettings _settings;
        public DashboardSettings.NodeSettings Settings => _settings ??= DataProvider.Module.Settings.GetNodeSettings(PrettyName);

        private decimal? GetSetting(Func<INodeSettings, decimal?> func) => func(Settings) ?? func(Category?.Settings) ?? func(DataProvider.Module.Settings);
        private Regex GetSetting(Func<INodeSettings, Regex> func) => func(Settings) ?? func(Category?.Settings) ?? func(DataProvider.Module.Settings);
        public decimal? CPUWarningPercent => GetSetting(i => i.CPUWarningPercent);
        public decimal? CPUCriticalPercent => GetSetting(i => i.CPUCriticalPercent);
        public decimal? MemoryWarningPercent => GetSetting(i => i.MemoryCriticalPercent);
        public decimal? MemoryCriticalPercent => GetSetting(i => i.MemoryCriticalPercent);
        public decimal? DiskWarningPercent => GetSetting(i => i.DiskWarningPercent);
        public decimal? DiskCriticalPercent => GetSetting(i => i.DiskCriticalPercent);
        public Regex ServicesPatternRegEx => GetSetting(i => i.ServicesPatternRegEx);

        private List<Interface> _primaryInterfaces;
        public List<Interface> PrimaryInterfaces
        {
            get
            {
                if (_primaryInterfaces == null || (_primaryInterfaces.Count == 0 && Interfaces?.Count > 0))
                {
                    var pattern = Settings?.PrimaryInterfacePatternRegex ?? Category?.Settings?.PrimaryInterfacePatternRegex;
                    var dbInterfaces = Interfaces.Where(i => i.IsLikelyPrimary(pattern)).ToList();
                    _primaryInterfaces = (dbInterfaces.Count > 0
                        ? dbInterfaces.OrderBy(i => i.Name)
                        : Interfaces.OrderByDescending(i => i.InBps + i.OutBps)).ToList();
                }
                return _primaryInterfaces;
            }
        }

        /// <summary>
        /// Should be called after a node is created to set parent referneces
        /// and removed ignored interfaces, volumes, etc.
        /// 
        /// This allows interfaces, volumes, etc. to poll through the provider
        /// </summary>
        public void AfterInitialize()
        {
            if (Interfaces != null)
            {
                var ignoredInterfaceRegex = GetSetting(s => s.IgnoredInterfaceRegEx);
                if (ignoredInterfaceRegex != null)
                {
                    Interfaces.RemoveAll(i => ignoredInterfaceRegex.IsMatch(i.Id));
                }

                Interfaces.ForEach(i => i.Node = this);
            }

            Volumes?.ForEach(v => v.Node = this);
            Apps?.ForEach(a => a.Node = this);
            Services?.ForEach(s => s.Node = this);
        }
    }
}
