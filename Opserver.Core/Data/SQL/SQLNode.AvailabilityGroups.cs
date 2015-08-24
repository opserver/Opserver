using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Jil;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLNode
    {
        private Cache<List<AvailabilityGroupInfo>> _availabilityGroups;
        public Cache<List<AvailabilityGroupInfo>> AvailabilityGroups
        {
            get
            {
                return _availabilityGroups ?? (_availabilityGroups = new Cache<List<AvailabilityGroupInfo>>
                    {
                        CacheForSeconds = Cluster.RefreshInterval,
                        UpdateCache = UpdateFromSql("AvailabilityGroups", async conn =>
                            {
                                var result = await conn.QueryAsync<AvailabilityGroupInfo>(GetFetchSQL<AvailabilityGroupInfo>());
                                result.ForEach(r => { r.Node = this; });
                                return result;
                            })
                    });
            }
        }

        private Cache<List<AvailabilityGroupReplicaInfo>> _availabilityGroupReplicas;
        public Cache<List<AvailabilityGroupReplicaInfo>> AvailabilityGroupReplicas
        {
            get
            {
                return _availabilityGroupReplicas ?? (_availabilityGroupReplicas = new Cache<List<AvailabilityGroupReplicaInfo>>
                {
                    CacheForSeconds = Cluster.RefreshInterval,
                    UpdateCache = UpdateFromSql("AvailabilityGroups", conn => AvailabilityGroupReplicaInfo.PopulateFromConnectionAsync(conn, this))
                });
            }
        }

        //public IEnumerable<DatabaseReplicaState> AvailabilityDatabases { get { return AvailabilityGroups.SafeData(true).SelectMany(ag => ag.Replicas.SelectMany(ag => )); } }

        /// <summary>
        /// Contains the core info about an availability group, not including any replica data
        /// sys.availability_groups: http://msdn.microsoft.com/en-us/library/ff878538.aspx
        /// sys.dm_hadr_availability_group_states: http://msdn.microsoft.com/en-us/library/ff878491.aspx
        /// </summary>
        public class AvailabilityGroupInfo : ISQLVersionedObject, IMonitedService
        {
            public Version MinVersion => SQLServerVersions.SQL2012.RTM;

            /* Availability Group Core */
            public string Name { get; internal set; }
            public Guid? GroupId { get; internal set; }
            public string ResourceId { get; internal set; }
            public string ResourceGroupId { get; internal set; }
            public int? FailureConditionLevel { get; internal set; }
            public int? HealthCheckTimeout { get; internal set; }
            public AutomatedBackupPreferences BackupPreference { get; internal set; }
            /* Group States */
            public string PrimaryReplica { get; internal set; }
            public bool IsPrimaryReplica { get; internal set; }
            public RecoveryHealths? PrimaryRecoveryHealth { get; internal set; }
            public RecoveryHealths? SecondaryRecoveryHealth { get; internal set; }
            public SynchronizationHealths? GroupSynchronizationHealth { get; internal set; }

            private bool? _hasDatabases;
            public bool HasDatabases
            {
                get
                {
                    if (!_hasDatabases.HasValue)
                    {
                        _hasDatabases = LocalReplica != null && RemoteReplicas != null
                                        && (LocalReplica.Databases.Count > 0
                                            || RemoteReplicas.Sum(r => r.Databases?.Count ?? 0) > 0);
                    }
                    return _hasDatabases.Value;
                }
            }

            [JilDirective(Ignore = true)]
            public SQLNode Node { get; internal set; }

            [JilDirective(Ignore = true)]
            public List<AvailabilityGroupReplicaInfo> Replicas
            {
                get { return Node.AvailabilityGroupReplicas.SafeData(true).Where(gr => GroupId == gr.GroupId).ToList(); }
            }
            [JilDirective(Ignore = true)]
            public List<AvailabilityGroupListener> Listeners
            {
                get { return Node.AvailabilityGroupListeners.SafeData(true).Where(gr => GroupId == gr.GroupId).ToList(); }
            }
            
            [JilDirective(Ignore = true)]
            public AvailabilityGroupReplicaInfo LocalReplica
            {
                get { return Replicas.FirstOrDefault(r => r.IsLocal.GetValueOrDefault()); }
            }
            [JilDirective(Ignore = true)]
            public IEnumerable<AvailabilityGroupReplicaInfo> RemoteReplicas
            {
                get { return Replicas.Where(r => !r.IsLocal.GetValueOrDefault()); }
            }

            public MonitorStatus MonitorStatus => Replicas.GetWorstStatus();

            public string MonitorStatusReason => Replicas.GetReasonSummary();

            internal const string FetchSQL = @"
Select ag.name Name,
       ag.group_id GroupId,
       ag.resource_id ResourceId,
       ag.resource_group_id ResourceGroupId,
       ag.failure_condition_level FailureConditionLevel,
       ag.health_check_timeout HealthCheckTimeout,
       ag.automated_backup_preference BackupPreference,
       ags.primary_replica PrimaryReplica,
       Cast(Case When ags.primary_replica = @@ServerName Then 1 Else 0 End as Bit) IsPrimaryReplica,
       ags.primary_recovery_health PrimaryRecoveryHealth,
       ags.secondary_recovery_health SecondaryRecoveryHealth,
       ags.synchronization_health GroupSynchronizationHealth
  From sys.availability_groups ag
       Join sys.dm_hadr_availability_group_states ags on ag.group_id = ags.group_id
";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }

        ///TODO: Break left join into another cache
        /// <summary>
        /// Contains the replication info about this availability group known to this node - will only be complete on the primary
        /// sys.availability_replicas: http://technet.microsoft.com/en-us/library/ff877883.aspx
        /// sys.dm_hadr_availability_replica_states: http://msdn.microsoft.com/en-us/library/ff878537.aspx
        /// sys.dm_hadr_availability_replica_cluster_states: http://msdn.microsoft.com/en-us/library/hh403396.aspx
        /// </summary>
        public class AvailabilityGroupReplicaInfo : ISQLVersionedObject, IMonitorStatus
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
            public int DBCount { get; internal set; }
            public long TotalLogSendQueueSize { get; internal set; }
            public long TotalLogSendRate { get; internal set; }
            public long TotalRedoQueueSize { get; internal set; }
            public long TotalRedoRate { get; internal set; }
            public long TotalFilestreamRate { get; internal set; }
            public decimal BytesSentPerSecond { get; internal set; }
            public long BytesSentTotal { get; internal set; }
            public decimal BytesReceivedPerSecond { get; internal set; }
            public long BytesReceivedTotal { get; internal set; }

            [JilDirective(Ignore = true)]
            public SQLNode ReplicaNode { get; internal set; }
            public List<DatabaseReplicaState> Databases { get; internal set; }

            public MonitorStatus MonitorStatus
            {
                get
                {
                    // Don't alert on empty AGs
                    if (!Databases.Any())
                    {
                        return MonitorStatus.Good;
                    }
                    if(SynchronizationHealth.HasValue)
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
            internal const string FetchSQL = @"
Select * Into #ar From sys.availability_replicas;
Select * Into #ars From sys.dm_hadr_availability_replica_states;
Select * Into #arcs From sys.dm_hadr_availability_replica_cluster_states;
Select group_id, 
       replica_id, 
       Count(*) DBCount, 
       Sum(log_send_queue_size) TotalLogSendQueueSize,
       Sum(log_send_rate) TotalLogSendRate,
       Sum(redo_queue_size) TotalRedoQueueSize,
       Sum(redo_rate) TotalRedoRate,
       Sum(filestream_send_rate) TotalFilestreamRate
  Into #dbs
  From sys.dm_hadr_database_replica_states dbs
Group By group_id, replica_id
 
 
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
       arcs.join_state JoinState,
       dbs.DBCount,
       dbs.TotalLogSendQueueSize,
       dbs.TotalLogSendRate,
       dbs.TotalRedoQueueSize,
       dbs.TotalRedoRate,
       dbs.TotalFilestreamRate
  From sys.availability_groups ag
       Join #ar ar On ar.group_id = ag.group_id
       Join #ars ars On ar.group_id = ars.group_id And ar.replica_id = ars.replica_id
       Join #arcs arcs On ar.group_id = arcs.group_id And ar.replica_id = arcs.replica_id
       Left Join #dbs dbs On ar.group_id = dbs.group_id And ar.replica_id = dbs.replica_id
 
Drop Table #ar;
Drop Table #ars;
Drop Table #arcs;
Drop Table #dbs;
";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }

            public static async Task<List<AvailabilityGroupReplicaInfo>> PopulateFromConnectionAsync(DbConnection conn, SQLNode node)
            {
                List<AvailabilityGroupReplicaInfo> groups;
                List<DatabaseReplicaState> databases;

                var sql = node.GetFetchSQL<AvailabilityGroupReplicaInfo>() + "\n\n" + node.GetFetchSQL<DatabaseReplicaState>();
                using (var multi = await conn.QueryMultipleAsync(sql))
                {
                    groups = (await multi.ReadAsync<AvailabilityGroupReplicaInfo>()).AsList();
                    databases = (await multi.ReadAsync<DatabaseReplicaState>()).AsList();
                }

                Func<string, string, PerfCounterRecord> getCounter = (cn, n) => node.GetPerfCounter("Availability Replica", cn, n);
                groups.ForEach(r =>
                {
                    r.Databases = databases.Where(gdb => r.GroupId == gdb.GroupId && r.ReplicaId == gdb.ReplicaId).ToList();

                    var instanceName = r.AvailabilityGroupName + ":" + r.ReplicaServerName;
                    var sc = getCounter("Bytes Sent to Transport/sec", instanceName);
                    if (sc != null)
                    {
                        r.BytesSentPerSecond = sc.CalculatedValue;
                        r.BytesSentTotal = sc.CurrentValue;
                    }
                    var rc = getCounter("Bytes Received from Replica/sec", instanceName);
                    if (rc != null)
                    {
                        r.BytesReceivedPerSecond = rc.CalculatedValue;
                        r.BytesReceivedTotal = rc.CurrentValue;
                    }
                });
                return groups;
            }
        }

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/ff877972.aspx
        /// </summary>
        public class DatabaseReplicaState : ISQLVersionedObject, IMonitorStatus
        {
            public Version MinVersion => SQLServerVersions.SQL2012.RTM;

            public int DatabaseId { get; internal set; }
            public Guid GroupId { get; internal set; }
            public Guid ReplicaId { get; internal set; }
            public Guid GroupDatabaseId { get; internal set; }
            public bool? IsLocal { get; internal set; }
            public SynchronizationStates? SynchronizationState { get; internal set; }
            public bool? IsCommitParticipant { get; internal set; }
            public SynchronizationHealths? SynchronizationHealth { get; internal set; }
            public DatabaseStates? DatabaseState { get; internal set; }
            public bool? IsSuspended { get; internal set; }
            public SuspendReasons? SuspendReason { get; internal set; }
            public decimal? RecoveryLSN { get; internal set; }
            public decimal? TruncationLSN { get; internal set; }
            public decimal? LastSentLSN { get; internal set; }
            public DateTime? LastSentTime { get; internal set; }
            public decimal? LastReceivedLSN { get; internal set; }
            public DateTime? LastReceivedTime { get; internal set; }
            public decimal? LastHardenedLSN { get; internal set; }
            public decimal? LastRedoneLSN { get; internal set; }
            public DateTime? LastRedoneTime { get; internal set; }
            public decimal? LastCommitLSN { get; internal set; }
            public DateTime? LastCommitTime { get; internal set; }
            public decimal? EndOfLogLSN { get; internal set; }
            public long? LogSendQueueSize { get; internal set; }
            public long? LogSendRate { get; internal set; }
            public long? RedoQueueSize { get; internal set; }
            public long? RedoRate { get; internal set; }
            public long? FileStreamSendRate { get; internal set; }
            public long? LowWatermarkForGhosts { get; internal set; }
            public string DatabaseName { get; internal set; }
            public long? LogKBytesUsed { get; internal set; }
            public long? LogKBytesTotal { get; internal set; }
            
            public DateTime? LogSendETA
            {
                get
                {
                    if (LogSendRate.GetValueOrDefault() <= 0 || LogSendQueueSize.GetValueOrDefault() <= 0) return null;
                    var secs = (double)(LogSendQueueSize / LogSendRate);
                    return DateTime.UtcNow.AddSeconds(secs);
                }
            }

            /// <summary>
            /// Returns the *real* log send rate, if there's nothing to send SQL still reports the last high rate, 
            /// when it's actually sending noting.  This will return null as it should in that case.
            /// </summary>
            public long? LogSendRateReal => LogSendQueueSize > 0 ? LogSendRate : null;

            public double? LogPercentUsed
            {
                get
                {
                    if (LogKBytesUsed.GetValueOrDefault() <= 0 || LogKBytesUsed.GetValueOrDefault() <= 0) return null;
                    return (double)(LogKBytesUsed / LogKBytesTotal);
                }
            }

            public MonitorStatus MonitorStatus
            {
                get
                {
                    switch (SynchronizationHealth)
                    {
                        case SynchronizationHealths.NotHealthy:
                        case SynchronizationHealths.PartiallyHealthy:
                            if (IsSuspended.GetValueOrDefault() && SuspendReason == SuspendReasons.UserAction) return MonitorStatus.Warning;
                            return MonitorStatus.Critical;
                    }
                    return MonitorStatus.Good;
                }
            }
            public string MonitorStatusReason
            {
                get
                {
                    switch (SynchronizationHealth)
                    {
                        case SynchronizationHealths.NotHealthy:
                            return DatabaseName + " - not syncing";
                        case SynchronizationHealths.PartiallyHealthy:
                            if (IsSuspended.GetValueOrDefault() && SuspendReason == SuspendReasons.UserAction) return DatabaseName + " - user suspended replication";
                            return DatabaseName + " - partially syncing";
                    }
                    return null;
                }
            }

            public string DatabaseStateDescription => DatabaseState.HasValue
                ? DatabaseState.GetDescription() + (IsSuspended.GetValueOrDefault() ? " (Suspended)" : "")
                : string.Empty;

            public string SuspendReasonDescription => SuspendReason.HasValue ? "Suspended by " + SuspendReason.GetDescription() : string.Empty;

            internal const string FetchSQL = @"
Select dbrs.database_id DatabaseId,
       dbrs.group_id GroupId,
       dbrs.replica_id ReplicaId,
       dbrs.group_database_id GroupDatabaseId,
       dbrs.is_local IsLocal,
       dbrs.synchronization_state SynchronizationState,
       dbrs.is_commit_participant IsCommitParticipant,
       dbrs.synchronization_health SynchronizationHealth,
       dbrs.database_state DatabaseState,
       dbrs.is_suspended IsSuspended,
       dbrs.suspend_reason SuspendReason,
       dbrs.recovery_lsn RecoveryLSN,
       dbrs.truncation_lsn TruncationLSN,
       dbrs.last_sent_lsn LastSentLSN,
       dbrs.last_sent_time LastSentTime,
       dbrs.last_received_lsn LastReceivedLSN,
       dbrs.last_received_time LastReceivedTime,
       dbrs.last_hardened_lsn LastHardenedLSN,
       dbrs.last_redone_lsn LastRedoneLSN,
       dbrs.last_redone_time LastRedoneTime,
       dbrs.last_commit_lsn LastCommitLSN,
       dbrs.last_commit_time LastCommitTime,
       dbrs.end_of_log_lsn EndOfLogLSN,
       dbrs.log_send_queue_size LogSendQueueSize,
       dbrs.log_send_rate LogSendRate,
       dbrs.redo_queue_size RedoQueueSize,
       dbrs.redo_rate RedoRate,
       dbrs.filestream_send_rate FileStreamSendRate,
       dbrs.low_water_mark_for_ghosts LowWatermarkForGhosts,
       DB_NAME(database_id) DatabaseName,
       lfu.cntr_value LogKBytesUsed,
	   lft.cntr_value LogKBytesTotal
  From sys.dm_hadr_database_replica_states dbrs
	   Left Join sys.dm_os_performance_counters lfu 
	     On DB_NAME(database_id) = lfu.instance_name
	     And lfu.counter_name = 'Log File(s) Used Size (KB)'
	     And dbrs.is_local = 1
       Left Join sys.dm_os_performance_counters lft 
	     On lfu.instance_name = lft.instance_name
	     And lft.counter_name = 'Log File(s) Size (KB)'";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }
    }
}