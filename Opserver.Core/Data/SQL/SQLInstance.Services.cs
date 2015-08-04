using System;
using System.Collections.Generic;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<SQLServiceInfo>> _services;
        public Cache<List<SQLServiceInfo>> Services => _services ?? (_services = SqlCacheList<SQLServiceInfo>(5 * 60));

        public class SQLServiceInfo : ISQLVersionedObject, IMonitorStatus
        {
            public Version MinVersion => SQLServerVersions.SQL2008R2.SP1;

            public MonitorStatus MonitorStatus
            {
                get
                {
                    if (!Status.HasValue) return MonitorStatus.Unknown;
                    switch (Status.Value)
                    {
                        case ServiceStatuses.Stopped:
                            return MonitorStatus.Critical;
                        case ServiceStatuses.StartPending:
                        case ServiceStatuses.StopPending:
                        case ServiceStatuses.Paused:
                            return MonitorStatus.Warning;
                        case ServiceStatuses.Running:
                        case ServiceStatuses.ContinuePending:
                        case ServiceStatuses.PausePending:
                            return MonitorStatus.Good;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            public string MonitorStatusReason
            {
                get
                {
                    if (!Status.HasValue) return ServiceName + " - Status unknown";
                    switch (Status.Value)
                    {
                        case ServiceStatuses.Running:
                        case ServiceStatuses.ContinuePending:
                        case ServiceStatuses.PausePending:
                            return null;
                        default:
                            return ServiceName + " - " + Status.GetDescription();
                    }
                }
            }

            public string ServiceName { get; internal set; }
            public string ServiceAccount { get; internal set; }
            public ServiceStartupTypes? StartupType { get; internal set; }
            public ServiceStatuses? Status { get; internal set; }
            public DateTimeOffset? LastStartupTime { get; internal set; }
            public string IsClustered { get; internal set; }
            public bool IsClusteredBool => IsClustered == "Y";

            internal const string FetchSQL = @"
Select servicename ServiceName,
       service_account ServiceAccount, 
       process_id ProcessId, 
       startup_type StartupType, 
       status Status,
       last_startup_time LastStartupTime,
       is_clustered IsClustered
  From sys.dm_server_services";

            public string GetFetchSQL(Version version)
            {
                return FetchSQL;
            }
        }
    }
}
