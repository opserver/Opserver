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
        public static MonitorStatus ToMonitorStatus(this NodeStatus status)
        {
            switch (status)
            {
                case NodeStatus.Unmanaged:
                    return MonitorStatus.Maintenance;
                case NodeStatus.Active:
                case NodeStatus.External:
                case NodeStatus.Up:
                case NodeStatus.Shutdown:
                    return MonitorStatus.Good;
                case NodeStatus.Down:
                case NodeStatus.Critical:
                    return MonitorStatus.Critical;
                case NodeStatus.Unreachable:
                case NodeStatus.Warning:
                case NodeStatus.PartlyAvailable:
                case NodeStatus.Unconfirmed:
                    return MonitorStatus.Warning;
                //case NodeStatus.Inactive:
                //case NodeStatus.Unplugged:
                default:
                    return MonitorStatus.Unknown;
            }
        }
    }
}