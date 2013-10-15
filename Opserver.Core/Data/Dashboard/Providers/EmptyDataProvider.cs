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
        private static readonly List<Interface> _allInterfaces = new List<Interface>();
        private static readonly List<Volume> _allVolumes = new List<Volume>();
        private static readonly List<Application> _allApplications = new List<Application>();
        private static readonly List<IPAddress> _nodeIPs = new List<IPAddress>();

        public override IEnumerable<string> GetExceptions() { yield break; }

        public override List<Node> AllNodes { get { return _allNodes; } }
        public override IEnumerable<IPAddress> GetIPsForNode(Node node) { return _nodeIPs; }
        public override IEnumerable<Node> GetNodesByIP(IPAddress ip) { return _allNodes; }

        public override IEnumerable<Node.CPUUtilization> GetCPUUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null) { yield break; }
        public override IEnumerable<Node.MemoryUtilization> GetMemoryUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null) { yield break; }

        public override List<Interface> AllInterfaces { get { return _allInterfaces; } }
        public override IEnumerable<Interface.InterfaceUtilization> GetUtilization(Interface volume, DateTime? start, DateTime? end, int? pointCount = null) { yield break; }

        public override List<Volume> AllVolumes { get { return _allVolumes; } }
        public override IEnumerable<Volume.VolumeUtilization> GetUtilization(Volume volume, DateTime? start, DateTime? end, int? pointCount = null) { yield break; }

        public override List<Application> AllApplications { get { return _allApplications; } }
    }
}
