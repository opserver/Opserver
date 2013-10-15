using System;
using System.Collections.Generic;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLNode
    {
        private Cache<List<DatabaseReplicaClusterState>> _databaseReplicaClusterStates;
        public Cache<List<DatabaseReplicaClusterState>> DatabaseReplicaClusterStates
        {
            get
            {
                return _databaseReplicaClusterStates ??
                       (_databaseReplicaClusterStates =
                        SqlCacheList<DatabaseReplicaClusterState>(Cluster.RefreshInterval));
            }
        }

        public class DatabaseReplicaClusterState : ISQLVersionedObject
        {
            public Version MinVersion { get { return SQLServerVersions.SQL2012.RTM; } }

            public Guid ReplicaId { get; internal set; }
            public Guid GroupDatabaseId { get; internal set; }
            public string DatabaseName { get; internal set; }
            public bool IsFailoverReady { get; internal set; }
            public bool IsPendinSecondarySuspend { get; internal set; }
            public bool IsDatabaseJoined { get; internal set; }
            public decimal? RecoveryLSN { get; internal set; }
            public decimal? TruncationLSN { get; internal set; }

            internal const string FetchSQL = @"
select replica_id ReplicaId,
       group_database_id GroupDatabaseId,
       database_name DatabaseName,
       is_failover_ready IsFailoverReady,
       is_pending_secondary_suspend IsPendinSecondarySuspend,
       is_database_joined IsDatabaseJoined,
       recovery_lsn RecoveryLSN,
       truncation_lsn TruncationLSN
  from sys.dm_hadr_database_replica_cluster_states";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }
    }
}