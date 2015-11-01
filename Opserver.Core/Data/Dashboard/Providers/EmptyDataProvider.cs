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

        private static readonly List<Node> _allNodes = new List<Node>();

        public override IEnumerable<string> GetExceptions() { yield break; }

        public override List<Node> AllNodes => _allNodes;
        public override IEnumerable<Node> GetNodesByIP(IPAddress ip) => _allNodes;

        public override Task<List<GraphPoint>> GetCPUUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<GraphPoint>());
        public override Task<List<GraphPoint>> GetMemoryUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<GraphPoint>());
        public override Task<List<DoubleGraphPoint>> GetUtilization(Interface volume, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<DoubleGraphPoint>());
        public override Task<List<GraphPoint>> GetUtilization(Volume volume, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(new List<GraphPoint>());
    }
}
