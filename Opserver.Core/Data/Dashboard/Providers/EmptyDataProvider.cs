using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public class EmptyDataProvider : DashboardDataProvider
    {
        public override bool HasData => false;
        public EmptyDataProvider(string uniqueKey) : base(uniqueKey) { }

        public override int MinSecondsBetweenPolls => 10;
        public override string NodeType => "None";
        public override IEnumerable<Cache> DataPollers => Enumerable.Empty<Cache>();
        protected override IEnumerable<MonitorStatus> GetMonitorStatus() => Enumerable.Empty<MonitorStatus>();
        protected override string GetMonitorStatusReason() => null;

        private static readonly List<Node> EmptyAllNodes = new List<Node>();

        public override IEnumerable<string> GetExceptions() => Enumerable.Empty<string>();

        public override List<Node> AllNodes => EmptyAllNodes;
        public override IEnumerable<Node> GetNodesByIP(IPAddress ip) => EmptyAllNodes;

        public override Task<List<GraphPoint>> GetCPUUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<GraphPoint>());
        public override Task<List<GraphPoint>> GetMemoryUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<GraphPoint>());
        public override Task<List<DoubleGraphPoint>> GetNetworkUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<DoubleGraphPoint>());
        public override Task<List<DoubleGraphPoint>> GetVolumePerformanceUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<DoubleGraphPoint>());
        public override Task<List<DoubleGraphPoint>> GetUtilizationAsync(Interface iface, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<DoubleGraphPoint>());
        public override Task<List<DoubleGraphPoint>> GetPerformanceUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<DoubleGraphPoint>());
        public override Task<List<GraphPoint>> GetUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<GraphPoint>());

        public override Task<Task<bool>> UpdateServiceAsync(Node node, string serviceName, Data.Dashboard.NodeService.Action action) => Task.FromResult(Task.FromResult(false));
    }
}
