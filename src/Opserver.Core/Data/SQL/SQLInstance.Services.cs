using System;
using System.Collections.Generic;
using EnumsNET;

namespace Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<SQLServiceInfo>> _services;
        public Cache<List<SQLServiceInfo>> Services => _services ??= SqlCacheList<SQLServiceInfo>(5.Minutes());

        public class SQLServiceInfo : ISQLVersioned, IMonitorStatus
        {
            Version IMinVersioned.MinVersion => SQLServerVersions.SQL2008R2.SP1;
            SQLServerEditions ISQLVersioned.SupportedEditions => SQLServerEditions.All;

            public MonitorStatus MonitorStatus
            {
                get
                {
                    if (!Status.HasValue) return MonitorStatus.Unknown;
                    return Status.Value switch
                    {
                        ServiceStatuses.Stopped => MonitorStatus.Critical,
                        ServiceStatuses.StartPending or ServiceStatuses.StopPending or ServiceStatuses.Paused => MonitorStatus.Warning,
                        ServiceStatuses.Running or ServiceStatuses.ContinuePending or ServiceStatuses.PausePending => MonitorStatus.Good,
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                }
            }

            public string MonitorStatusReason
            {
                get
                {
                    if (!Status.HasValue) return ServiceName + " - Status unknown";
                    return Status.Value switch
                    {
                        ServiceStatuses.Running or ServiceStatuses.ContinuePending or ServiceStatuses.PausePending => null,
                        _ => ServiceName + " - " + (Status.HasValue ? Status.Value.AsString(EnumFormat.Description) : ""),
                    };
                }
            }

            public string ServiceName { get; internal set; }
            public string ServiceAccount { get; internal set; }
            public ServiceStartupTypes? StartupType { get; internal set; }
            public ServiceStatuses? Status { get; internal set; }
            public DateTimeOffset? LastStartupTime { get; internal set; }
            public string IsClustered { get; internal set; }
            public bool IsClusteredBool => IsClustered == "Y";

            public string GetFetchSQL(in SQLServerEngine e) => @"
Select servicename ServiceName,
       service_account ServiceAccount, 
       process_id ProcessId, 
       startup_type StartupType, 
       status Status,
       last_startup_time LastStartupTime,
       is_clustered IsClustered
  From sys.dm_server_services;
";
        }
    }
}
