using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLNode : SQLInstance, IEquatable<SQLNode>
    {
        public SQLCluster Cluster { get; internal set; }

        public SQLNode(SQLCluster sqlCluster, SQLSettings.Instance node) : base(node.Name, node.ConnectionString, node.ObjectName)
        {
            Cluster = sqlCluster;
        }

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                foreach (var p in base.DataPollers)
                    yield return p;
                yield return AvailabilityGroups;
                yield return AvailabilityGroupReplicas;
                yield return AvailabilityGroupListeners;
                //yield return AvailabilityGroupLisenerIPAddresses;
                yield return ClusterMembers;
                yield return ClusterNetworks;
                yield return ClusterStatus;
                //yield return DatabaseReplicaClusterStates;
                //yield return TCPListeners;
            }
        }

        public bool IsAnAGPrimary
        {
            get { return AvailabilityGroups.HasData() && AvailabilityGroups.Data.Any(ag => ag.IsPrimaryReplica); }
        }

        public bool IsAllAGsPrimary
        {
            get { return AvailabilityGroups.HasData() && AvailabilityGroups.Data.Where(ag => ag.HasDatabases).All(ag => ag.IsPrimaryReplica); }
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            foreach (var ms in base.GetMonitorStatus())
                yield return ms;
            yield return AvailabilityGroups.SafeData(true)
                                           .Where(ag => ag.LocalReplica != null)
                                           .Select(ag => ag.LocalReplica)
                                           .GetWorstStatus();
        }

        public int ClusterVotes { get { return ClusterMember.Votes.GetValueOrDefault(0); } }
        public ClusterMemberTypes ClusterType { get { return ClusterMember.Type; } }

        public ClusterMemberInfo ClusterMember
        {
            get
            {
                return (ClusterMembers.HasData()
                            ? ClusterMembers.Data.FirstOrDefault(c => string.Equals(c.MemberName, Name, StringComparison.InvariantCultureIgnoreCase))
                            : null) ?? new ClusterMemberInfo();
            }
        }
        
        public bool Equals(SQLNode other)
        {
            return other != null && Cluster.Equals(other.Cluster) && string.Equals(Name, other.Name);
        }
    }
}