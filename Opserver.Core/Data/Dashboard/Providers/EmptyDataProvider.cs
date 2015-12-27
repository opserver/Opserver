using System;
using System.Collections.Generic;
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
        public override IEnumerable<Cache> DataPollers { get { yield break; } }
        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() => null;

        private static readonly List<Node> EmptyAllNodes = new List<Node>();

        public override IEnumerable<string> GetExceptions() { yield break; }

        public override List<Node> AllNodes => EmptyAllNodes;
        public override IEnumerable<Node> GetNodesByIP(IPAddress ip) => EmptyAllNodes;

        public override Task<List<GraphPoint>> GetCPUUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<GraphPoint>());
        public override Task<List<GraphPoint>> GetMemoryUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<GraphPoint>());
        public override Task<List<DoubleGraphPoint>> GetNetworkUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<DoubleGraphPoint>());
        public override Task<List<DoubleGraphPoint>> GetUtilizationAsync(Interface volume, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<DoubleGraphPoint>());
        public override Task<List<GraphPoint>> GetUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<GraphPoint>());
    }
}
