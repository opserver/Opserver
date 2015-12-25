using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLNode
    {
        /// <summary>
        /// Contains the replication info about this availability group known to this node - will only be complete on the primary
        /// sys.availability_replicas: http://technet.microsoft.com/en-us/library/ff877883.aspx
        /// sys.dm_hadr_availability_replica_states: http://msdn.microsoft.com/en-us/library/ff878537.aspx
        /// sys.dm_hadr_availability_replica_cluster_states: http://msdn.microsoft.com/en-us/library/hh403396.aspx
        /// </summary>
        public class AGReplica : ISQLVersioned, IMonitorStatus
        {
            public Version MinVersion => SQLServerVersions.SQL2012.RTM;

            public string AvailabilityGroupName { get; internal set; }
            public Guid? GroupId { get; internal set; }
            /* Replicas */
            public Guid? ReplicaId { get; internal set; }
            public int? ReplicaMetadataId { get; internal set; }
            public string ReplicaServerName { get; internal set; }
            public string EndPointUrl { get; internal set; }
            public AvailabilityModes? AvailabilityMode { get; internal set; }
            public FailoverModes FailoverMode { get; internal set; }
            public int? SessionTimeout { get; internal set; }
            public PriRoleAllowConnections? PrimaryRoleAllowConnections { get; internal set; }
            public SecRoleAllowConnections? SecondaryRoleAllowConnections { get; internal set; }
            public DateTime? CreationDate { get; internal set; }
            public DateTime? ModifiedDate { get; internal set; }
            public int? BackupPriority { get; internal set; }
            public string ReadOnlyRoutingUrl { get; internal set; }
            /* Replica State */
            public bool? IsLocal { get; internal set; }
            public ReplicaRoles? Role { get; internal set; }
            public OperationStates? OperationalState { get; internal set; }
            public ConnectedStates? ConnectedState { get; internal set; }
            public RecoveryHealths? RecoveryHealth { get; internal set; }
            public SynchronizationHealths? SynchronizationHealth { get; internal set; }
            public int? LastConnectErrorNumber { get; internal set; }
            public string LastConnectErrorDescription { get; internal set; }
            public DateTime? LastConnectErrorTimeStamp { get; internal set; }
            /* Replica Cluster State */
            public JoinStates JoinState { get; internal set; }
            /* Replication Info */
            public int DBCount => Databases?.Count ?? 0;
            public long TotalLogSendQueueSize => Databases?.Sum(db => db.LogSendQueueSize) ?? 0;
            public long TotalLogSendRate => Databases?.Sum(db => db.LogSendRate) ?? 0;
            public long TotalRedoQueueSize => Databases?.Sum(db => db.RedoQueueSize) ?? 0;
            public long TotalRedoRate => Databases?.Sum(db => db.RedoRate) ?? 0;
            public long TotalFilestreamRate => Databases?.Sum(db => db.FileStreamSendRate) ?? 0;
            public decimal BytesSentPerSecond { get; internal set; }
            public long BytesSentTotal { get; internal set; }
            public decimal BytesReceivedPerSecond { get; internal set; }
            public long BytesReceivedTotal { get; internal set; }

            [IgnoreDataMember]
            public SQLNode ReplicaNode { get; internal set; }
            public List<AGDatabaseReplica> Databases { get; internal set; }

            public MonitorStatus MonitorStatus
            {
                get
                {
                    // Don't alert on empty AGs
                    if (!Databases.Any())
                    {
                        return MonitorStatus.Good;
                    }
                    if (SynchronizationHealth.HasValue)
                    {
                        switch (SynchronizationHealth.Value)
                        {
                            case SynchronizationHealths.NotHealthy:
                                return MonitorStatus.Critical;
                            case SynchronizationHealths.PartiallyHealthy:
                                return MonitorStatus.Warning;
                            //case SynchronizationHealths.Healthy:
                            default:
                                return MonitorStatus.Good;
                        }
                    }
                    return Databases.GetWorstStatus();
                }
            }
            public string MonitorStatusReason
            {
                get
                {
                    if (Databases.Any() && SynchronizationHealth.HasValue)
                    {
                        if (SynchronizationHealth == SynchronizationHealths.Healthy)
                            return null;
                        return "Sync health: " + SynchronizationHealth.Value.GetDescription();
                    }
                    return Databases.GetReasonSummary();
                }
            }

            // Why? Because MS doesn't consider DMV perf important, and it's 3-4x faster to temp table 
            // the results then join when many DBs are in an availability group
            public string GetFetchSQL(Version v) => @"
Select * Into #ar From sys.availability_replicas;
Select * Into #ars From sys.dm_hadr_availability_replica_states;
Select * Into #arcs From sys.dm_hadr_availability_replica_cluster_states;
 
Select ag.name AvailabilityGroupName,
       ag.group_id GroupId,
       ar.replica_id ReplicaId,
       ar.replica_metadata_id ReplicaMetadataId,
       ar.replica_server_name ReplicaServerName,
       ar.endpoint_url EndPointUrl,
       ar.availability_mode AvailabilityMode,
       ar.failover_mode FailoverMode,
       ar.session_timeout SessionTimeout,
       ar.primary_role_allow_connections PrimaryRoleAllowConnections,
       ar.secondary_role_allow_connections SecondaryRoleAllowConnections,
       ar.create_date CreationDate,
       ar.modify_date ModifiedDate,
       ar.backup_priority BackupPriority,
       ar.read_only_routing_url ReadOnlyRoutingUrl,
       ars.is_local IsLocal,
       ars.role Role,
       ars.operational_state OperationalState,
       ars.connected_state ConnectedState,
       ars.recovery_health RecoveryHealth,
       ars.synchronization_health SynchronizationHealth,
       ars.last_connect_error_number LastConnectErrorNumber,
       ars.last_connect_error_description LastConnectErrorDescription,
       ars.last_connect_error_timestamp LastConnectErrorTimestamp,
       arcs.join_state JoinState
  From sys.availability_groups ag
       Join #ar ar On ar.group_id = ag.group_id
       Join #ars ars On ar.group_id = ars.group_id And ar.replica_id = ars.replica_id
       Join #arcs arcs On ar.group_id = arcs.group_id And ar.replica_id = arcs.replica_id
 
Drop Table #ar;
Drop Table #ars;
Drop Table #arcs;
";
        }
    }
}
