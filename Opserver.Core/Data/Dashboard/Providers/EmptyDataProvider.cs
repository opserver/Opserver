using System;
using System.Collections.Generic;
using System.Net;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public class EmptyDataProvider : DashboardDataProvider
    {
        public override bool HasData { get { return false; } }
        public EmptyDataProvider(string uniqueKey) : base(uniqueKey) { }

        public override int MinSecondsBetweenPolls { get { return 10; } }
        public override string NodeType { get { return "None"; } }
        public override IEnumerable<Cache> DataPollers { get { yield break; } }
        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() { return null; }

        private static readonly List<Node> _allNodes = new List<Node>();

        public override IEnumerable<string> GetExceptions() { yield break; }

        public override List<Node> AllNodes { get { return _allNodes; } }
        public override Node GetNode(string host) { return null; }

        public override IEnumerable<Node> GetNodesByIP(IPAddress ip) { return _allNodes; }

        public override PointSeries GetSeries(string metric, string host, int secondsAgo, int? pointCount = null, params Tuple<string, string>[] tags)
        {
            return new PointSeries(host);
        }

        public override PointSeries GetSeries(string metric, string host, DateTime? start, DateTime? end, int? pointCount = null, params Tuple<string, string>[] tags)
        {
            return new PointSeries(host);
        }
    }
}
