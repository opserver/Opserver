using System;
using System.Text.RegularExpressions;
using StackExchange.Opserver.Data.Dashboard.Providers;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Volume : IMonitorStatus
    {
        // TODO: Not constants eh?
        public const int WarningPercentUsed = 90;
        public const int CriticalPercentUsed = 95;

        public string Id { get; internal set; }
        public string NodeId { get; internal set; }
        public DateTime? LastSync { get; internal set; }
        public int? Index { get; internal set; }
        public string Name { get; internal set; }
        public string Caption { get; internal set; }
        public string Description { get; internal set; }
        public string Type { get; internal set; }
        public NodeStatus Status { get; internal set; }

        public double? Size { get; internal set; }
        public double? Used { get; internal set; }
        public double? Available { get; internal set; }
        public float? PercentUsed { get; internal set; }

        public MonitorStatus MonitorStatus => Status.ToMonitorStatus();
        // TODO: Implement
        public string MonitorStatusReason => null;

        public bool IsDisk => Type == null || Type == "Fixed Disk";
        public bool IsRAM => Type == "RAM";
        public bool IsVirtualMemory => Type == "Virtual Memory";

        public string PrettyName => PrettyDescription;

        public string PrettyDescription
        {
            get
            {
                var m = Regex.Match(Description, "([a-zA-Z]):\\\\");
                return m.Success && m.Groups.Count > 1 ? m.Groups[1].Value : Description;
            }
        }

        private readonly Func<double?, string> _sizeFormat = s => s.HasValue ? s.Value.ToSize() : "";

        public string PrettySize => _sizeFormat(Size);
        public string PrettyUsed => _sizeFormat(Used);
        public string PrettyAvailable => _sizeFormat(Available);

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