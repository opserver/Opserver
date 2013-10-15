using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLCluster : IEquatable<SQLCluster>, IMonitedService
    {
        public string Name { get { return ClusterSettings.Name; } }
        public int RefreshInterval { get { return ClusterSettings.RefreshIntervalSeconds; } }
        private SQLSettings.Cluster ClusterSettings { get; set; }

        public List<SQLNode> Nodes { get; private set; }
        
        public List<SQLNode.AvailabilityGroupInfo> AvailabilityGroups
        {
            get { return Nodes.SelectMany(n => n.AvailabilityGroups.SafeData(true).Where(ag => ag.IsPrimaryReplica)).ToList(); }
        }
        
        public IEnumerable<SQLNode.AvailabilityGroupInfo> GetAvailabilityGroups(string node, string agName)
        {
            Func<SQLNode.AvailabilityGroupInfo, bool> agMatch = ag => agName.IsNullOrEmpty() || ag.Name == agName;

            if (node.HasValue())
                return Nodes.Where(n => string.Equals(n.Name, node))
                            .SelectMany(n => n.AvailabilityGroups.SafeData(true).Where(agMatch));

            return Nodes.SelectMany(n => n.AvailabilityGroups.SafeData(true).Where(agMatch));
        }

        public MonitorStatus MonitorStatus
        {
            get { return Nodes.GetWorstStatus("SQLCluster-" + Name); }
        }
        public string MonitorStatusReason
        {
            get { return MonitorStatus == MonitorStatus.Good ? null : Nodes.GetReasonSummary(); }
        }

        public SQLNode.ClusterState ClusterStatus
        {
            get
            {
                var validNode = Nodes.FirstOrDefault(n => n.ClusterStatus.HasData() && n.ClusterStatus.Data.ClusterName.HasValue());
                return validNode != null ? validNode.ClusterStatus.Data : null;
            }
        }

        public QuorumTypes QuorumType
        {
            get { return ClusterStatus != null ? ClusterStatus.QuorumType : QuorumTypes.Unknown; }
        }
        public QuorumStates QuorumState
        {
            get { return ClusterStatus != null ? ClusterStatus.QuorumState : QuorumStates.Unknown; }
        }

        public SQLCluster(SQLSettings.Cluster cluster)
        {
            ClusterSettings = cluster;
            Nodes = cluster.Nodes
                           .Select(n => new SQLNode(this, n))
                           .Where(n => n.TryAddToGlobalPollers())
                           .ToList();
        }

        public bool Equals(SQLCluster other)
        {
            return other != null && string.Equals(Name, other.Name);
        }

        public SQLNode GetNode(string name)
        {
            return Nodes.FirstOrDefault(n => string.Equals(n.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}