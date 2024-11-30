namespace Opserver.Data
{
    public enum NodeStatus
    {
        Unknown = 0,
        Up = 1,
        Down = 2,
        Warning = 3,
        Shutdown = 4,
        Testing = 5,
        Dormant = 6,
        LowerLayerDown = 8,
        Unmanaged = 9,
        Unplugged = 10,
        External = 11,
        Unreachable = 12,
        Critical = 14,
        PartlyAvailable = 15,
        Misconfigured = 16,
        Undefined = 17,
        Unconfirmed = 19,
        Active = 22,
        Inactive = 24,
        MonitoringDisabled = 26,
        Disabled = 27
    }

    public static class ServerStatusExtensions
    {
        public static MonitorStatus ToMonitorStatus(this NodeStatus status) => status switch
        {
            NodeStatus.Unmanaged => MonitorStatus.Maintenance,
            NodeStatus.Active or NodeStatus.External or NodeStatus.Up or NodeStatus.Shutdown => MonitorStatus.Good,
            NodeStatus.Down or NodeStatus.Critical => MonitorStatus.Critical,
            NodeStatus.Unreachable or NodeStatus.Warning or NodeStatus.PartlyAvailable or NodeStatus.Unconfirmed => MonitorStatus.Warning,
            //case NodeStatus.Inactive:
            //case NodeStatus.Unplugged:
            _ => MonitorStatus.Unknown,
        };
    }
}
