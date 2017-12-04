using System;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver.Data.Dashboard
{
    /// <summary>
    /// Service class 
    /// </summary>
    public partial class NodeService : IMonitorStatus
    {
        public Node Node { get; set; }

        public string Id { get; internal set; }
        public DateTime? LastSync { get; internal set; }
        public string Name { get; internal set; }
        public string Caption { get; internal set; }
        public string Description { get; internal set; }
        public NodeStatus Status { get; internal set; }

        public string DisplayName { get; internal set; }
        public bool Running { get; internal set; }
        public string StartMode { get; internal set; }
        public string StartName { get; internal set; }
        public string State { get; internal set; }

        public MonitorStatus MonitorStatus => Status.ToMonitorStatus();
        // TODO: Implement
        public string MonitorStatusReason => null;

        public enum Action
        {
            Stop,
            Start
        }
    }
}
