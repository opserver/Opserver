using System;
using System.Threading.Tasks;

namespace Opserver.Data.Dashboard
{
    /// <summary>
    /// Service class
    /// </summary>
    public class NodeService : IMonitorStatus
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

        public Task<ServiceActionResult> Update(Action action) =>
            (Node.DataProvider as IServiceControlProvider)?.UpdateServiceAsync(Node, Id, action) ?? Task.FromResult(new ServiceActionResult(false, "An unexpected error has occurred"));

        public enum Action
        {
            Stop,
            Start
        }
    }

    public class ServiceActionResult
    {
        public ServiceActionResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
