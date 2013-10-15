using System;
using System.Text.RegularExpressions;
using StackExchange.Opserver.Data.Dashboard.Providers;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Volume : IMonitorStatus
    {
        internal DashboardDataProvider DataProvider { get; set; }

        // TODO: Not constants eh?
        public const int WarningPercentUsed = 90;
        public const int CriticalPercentUsed = 95;

        public int Id { get; internal set; }
        public int NodeId { get; internal set; }
        public DateTime? LastSync { get; internal set; }
        public int? Index { get; internal set; }
        public string Name { get; internal set; }
        public string Caption { get; internal set; }
        public string Description { get; internal set; }
        public string Type { get; internal set; }
        public NodeStatus Status { get; internal set; }

        public Double? Size { get; internal set; }
        public Double? Used { get; internal set; }
        public Double? Available { get; internal set; }
        public Single? PercentUsed { get; internal set; }

        public MonitorStatus MonitorStatus
        {
            get { return Status.ToMonitorStatus(); }
        }
        // TODO: Implement
        public string MonitorStatusReason { get { return null; } }

        public bool IsDisk { get { return Type == "Fixed Disk"; } }
        public bool IsRAM { get { return Type == "RAM"; } }
        public bool IsVirtualMemory { get { return Type == "Virtual Memory"; } }

        public string PrettyName
        {
            get { return PrettyDescription; }
        }

        public string PrettyDescription
        {
            get
            {
                var m = Regex.Match(Description, "([a-zA-Z]):\\\\");
                return m.Success && m.Groups.Count > 1 ? m.Groups[1].Value : Description;
            }
        }

        private readonly Func<Double?, string> _sizeFormat = s => s.HasValue ? s.Value.ToSize() : "";

        public string PrettySize { get { return _sizeFormat(Size); } }
        public string PrettyUsed { get { return _sizeFormat(Used); } }
        public string PrettyAvailable { get { return _sizeFormat(Available); } }

        public string SpaceStatusClass
        {
            get
            {
                if (PercentUsed > CriticalPercentUsed)
                    return MonitorStatus.Critical.GetDescription();
                if (PercentUsed > WarningPercentUsed)
                    return MonitorStatus.Warning.GetDescription();
                return MonitorStatus.Good.GetDescription();
            }
        }
    }
}