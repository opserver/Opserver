using System;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLNode
    {
        private Cache<ClusterState> _clusterStatus;
        public Cache<ClusterState> ClusterStatus
        {
            get { return _clusterStatus ?? (_clusterStatus = SqlCacheSingle<ClusterState>(Cluster.RefreshInterval)); }
        }

        public class ClusterState : ISQLVersionedObject
        {
            public Version MinVersion { get { return SQLServerVersions.SQL2012.RTM; } }

            public string ClusterName { get; internal set; }
            public QuorumTypes QuorumType { get; internal set; }
            public QuorumStates QuorumState { get; internal set; }
            public int? Votes { get; internal set; }

            internal const string FetchSQL = @"
Select cluster_name ClusterName,
       quorum_type QuorumType,
       quorum_state QuorumState
  From sys.dm_hadr_cluster";
            
            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }
    }
}