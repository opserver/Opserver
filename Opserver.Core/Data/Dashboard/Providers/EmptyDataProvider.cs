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
        public override IEnumerable<Cache> DataPollers { get { yield break; } }
        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() { return null; }

        private static readonly List<Node> _allNodes = new List<Node>();
        private static readonly List<Interface> _allInterfaces = new List<Interface>();
        private static readonly List<Volume> _allVolumes = new List<Volume>();
        private static readonly List<Application> _allApplications = new List<Application>();
        private static readonly List<IPAddress> _nodeIPs = new List<IPAddress>();

        public override IEnumerable<string> GetExceptions() { yield break; }

        public override List<Node> AllNodes => _allNodes;
        public override IEnumerable<IPAddress> GetIPsForNode(Node node) { return _nodeIPs; }
        public override IEnumerable<Node> GetNodesByIP(IPAddress ip) { return _allNodes; }

        public override Task<IEnumerable<Node.CPUUtilization>> GetCPUUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(Enumerable.Empty<Node.CPUUtilization>());
        public override Task<IEnumerable<Node.MemoryUtilization>> GetMemoryUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(Enumerable.Empty<Node.MemoryUtilization>());

        public override List<Interface> AllInterfaces => _allInterfaces;
        public override Task<IEnumerable<Interface.InterfaceUtilization>> GetUtilization(Interface volume, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(Enumerable.Empty<Interface.InterfaceUtilization>());

        public override List<Volume> AllVolumes => _allVolumes;
        public override Task<IEnumerable<Volume.VolumeUtilization>> GetUtilization(Volume volume, DateTime? start, DateTime? end, int? pointCount = null) => Task.FromResult(Enumerable.Empty<Volume.VolumeUtilization>());

        public override List<Application> AllApplications => _allApplications;
    }
}
