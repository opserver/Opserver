using System;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver.Data.Dashboard
{
    /// <summary>
    /// WMI Win32_Service class  https://msdn.microsoft.com/en-us/library/aa394418(v=vs.85).aspx
    /// </summary>
    public partial class Win32Service : IMonitorStatus
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



        public bool AcceptPause { get; internal set; }
        public bool AcceptStop { get; internal set; }
        public string DisplayName { get; internal set; }
        public uint ProcessId { get; internal set; }
        public bool Started { get; internal set; }
        public string StartMode { get; internal set; }
        public string StartName { get; internal set; }
        public string State { get; internal set; }



        public MonitorStatus MonitorStatus => Status.ToMonitorStatus();
        // TODO: Implement
        public string MonitorStatusReason => null;

    }
}
