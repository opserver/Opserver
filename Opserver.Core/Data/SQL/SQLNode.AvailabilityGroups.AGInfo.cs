using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLNode
    {
        /// <summary>
        /// Contains the core info about an availability group, not including any replica data
        /// sys.availability_groups: http://msdn.microsoft.com/en-us/library/ff878538.aspx
        /// sys.dm_hadr_availability_group_states: http://msdn.microsoft.com/en-us/library/ff878491.aspx
        /// </summary>
        public class AGInfo : ISQLVersioned, IMonitedService
        {
            public Version MinVersion => SQLServerVersions.SQL2012.RTM;

            [IgnoreDataMember]
            public SQLNode Node { get; internal set; }
            /* Availability Group Core */
            public string Name { get; internal set; }
            public string ClusterName { get; internal set; }
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
                        _hasDatabases = LocalReplica?.Databases.Count > 0
                                        || RemoteReplicas?.Sum(r => r.Databases?.Count ?? 0) > 0;
                    }
                    return _hasDatabases.Value;
                }
            }

            public List<AGReplica> Replicas { get; internal set; }
            public List<AGListener> Listeners { get; internal set; }

            public AGReplica LocalReplica =>
                Replicas.FirstOrDefault(r => r.IsLocal.GetValueOrDefault());

            public IEnumerable<AGReplica> RemoteReplicas =>
                Replicas.Where(r => !r.IsLocal.GetValueOrDefault());

            public MonitorStatus MonitorStatus => Replicas.GetWorstStatus();

            public string MonitorStatusReason => Replicas.GetReasonSummary();

            public string GetFetchSQL(Version v) => @"
Select ag.name Name,
       c.cluster_name ClusterName,
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
       Cross Join sys.dm_hadr_cluster c
";
        }
    }
}
