using System;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLNode
    {
        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/ff877972.aspx
        /// </summary>
        public class AGDatabaseReplica : ISQLVersioned, IMonitorStatus
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
                    if (LogKBytesUsed.GetValueOrDefault() <= 0 || LogKBytesTotal.GetValueOrDefault() <= 0) return null;
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

            public string GetFetchSQL(Version v) => @"
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
        }
    }
}
