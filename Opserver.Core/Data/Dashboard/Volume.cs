using System;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Volume : IMonitorStatus
    {
        public Node Node { get; set; }
        
        public string Id { get; internal set; }
        public string NodeId { get; internal set; }
        public DateTime? LastSync { get; internal set; }
        public int? Index { get; internal set; }
        public string Name { get; internal set; }
        public string Caption { get; internal set; }
        public string Description { get; internal set; }
        public string Type { get; internal set; }
        public NodeStatus Status { get; internal set; }

        public decimal? Size { get; internal set; }
        public decimal? Used { get; internal set; }
        public decimal? Available { get; internal set; }
        public decimal? PercentUsed { get; internal set; }
        public decimal? PercentFree => 100 - PercentUsed;

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

        private readonly Func<decimal?, string> _sizeFormat = s => s.HasValue ? s.Value.ToSize() : "";

        public string PrettySize => _sizeFormat(Size);
        public string PrettyUsed => _sizeFormat(Used);
        public string PrettyAvailable => _sizeFormat(Available);

        public MonitorStatus SpaceStatus
        {
            get
            {
                if (!PercentUsed.HasValue)
                    return MonitorStatus.Unknown;
                if (Node.DiskCriticalPercent.HasValue && PercentUsed > Node.DiskCriticalPercent.Value)
                    return MonitorStatus.Critical;
                if (Node.DiskWarningPercent.HasValue && PercentUsed > Node.DiskWarningPercent.Value)
                    return MonitorStatus.Warning;
                return MonitorStatus.Good;
            }
        }
    }
}