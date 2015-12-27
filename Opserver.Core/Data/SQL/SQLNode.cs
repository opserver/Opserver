using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLNode : SQLInstance, IEquatable<SQLNode>
    {
        public SQLCluster Cluster { get; internal set; }

        public SQLNode(SQLCluster sqlCluster, SQLSettings.Instance node) : base(node)
        {
            Cluster = sqlCluster;
        }

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                foreach (var p in base.DataPollers)
                    yield return p;
                yield return AGClusterInfo;
                yield return AvailabilityGroups;
                //yield return TCPListeners;
            }
        }
        
        public bool IsAllAGsPrimary
        {
            get { return AvailabilityGroups.HasData() && AvailabilityGroups.Data.Where(ag => ag.HasDatabases).All(ag => ag.IsPrimaryReplica); }
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            foreach (var ms in base.GetMonitorStatus())
                yield return ms;
            var worstAG = AvailabilityGroups?.Data?
                .Where(ag => ag.LocalReplica != null)
                .Select(ag => ag.LocalReplica)
                .GetWorstStatus();
            if (worstAG.HasValue)
                yield return worstAG.Value;
        }

        public int ClusterVotes => AGClusterMember.Votes.GetValueOrDefault(0);
        public ClusterMemberTypes ClusterType => AGClusterMember.Type;

        public AGClusterMemberInfo AGClusterMember =>
            AGClusterInfo.Data?.Members.FirstOrDefault(c => c.IsLocal) ?? new AGClusterMemberInfo();

        public bool Equals(SQLNode other)
        {
            return other != null && Cluster.Equals(other.Cluster) && string.Equals(Name, other.Name);
        }
    }
}