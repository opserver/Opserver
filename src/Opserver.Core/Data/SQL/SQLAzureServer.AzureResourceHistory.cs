using System;
using System.Collections.Generic;
using System.Linq;
using Opserver.Data.Dashboard;

namespace Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<AzureResourceEvent>> _azureResourceHistory;

        public Cache<List<AzureResourceEvent>> AzureResourceHistory =>
            _azureResourceHistory ??= GetSqlCache(nameof(AzureResourceHistory), async conn =>
            {
                var sql = GetFetchSQL<AzureResourceEvent>();
                var result = await conn.QueryAsync<AzureResourceEvent>(sql);
                var lastResult = result.Count > 0 ? result.Last() : null;
                CurrentDTUPercent = lastResult?.AvgDTUPercent;
                CurrentDTULimit = lastResult?.DTULimit;
                return result;
            });

        public double? CurrentDTUPercent { get; set; }
        public int? CurrentDTULimit { get; set; }

        public class AzureResourceEvent : ISQLVersioned, IGraphPoint
        {
            Version IMinVersioned.MinVersion => SQLServerVersions.SQL2012.RTM;
            SQLServerEditions ISQLVersioned.SupportedEditions => SQLServerEditions.Azure;

            private long? _dateEpoch;
            public long DateEpoch => _dateEpoch ??= EventTime.ToEpochTime();
            public DateTime EventTime { get; internal set; }
            public double AvgDTUPercent { get; internal set; }
            public double AvgCPUPercent { get; internal set; }
            public double AvgDataIOPercent { get; internal set; }
            public double AvgLogWritePercent { get; internal set; }
            public double AvgMemoryPercent { get; internal set; }
            public double XTPStoragePercent { get; internal set; }
            public double MaxWorkerPercent { get; internal set; }
            public double MaxSessionPercent { get; internal set; }
            public double AvgInstanceCPUPercent { get; internal set; }

            public double AvgInstanceMemoryPercent { get; internal set; }
            public double AvgLoginRatePercent { get; internal set; }
            public double CPULimit { get; internal set; }
            public int DTULimit { get; internal set; }
            public ReplicaRoleType ReplicaRole { get; internal set; }

            public enum ReplicaRoleType
            {
                Primary = 0,
                ReadOnly = 1,
                Forwarder = 2,
            }

            public string GetFetchSQL(in SQLServerEngine e) => @"
Select end_time AS EventTime,
	  (Select Max(v) From (Values (avg_cpu_percent), (avg_data_io_percent), (avg_log_write_percent)) As value(v)) As AvgDTUPercent,
	   avg_cpu_percent AvgCPUPercent,
	   avg_data_io_percent AvgDataIOPercent,
	   avg_log_write_percent AvgLogWritePercent,
	   avg_memory_usage_percent AvgMemoryPercent,
	   xtp_storage_percent XTPStoragePercent,
	   max_worker_percent MaxWorkerPercent,
	   max_session_percent MaxSessionPercent,
	   avg_instance_cpu_percent AvgInstanceCPUPercent, 
	   avg_instance_memory_percent AvgInstanceMemoryPercent,
	   avg_login_rate_percent AvgLoginRatePercent,
	   cpu_limit CPULimit, 
	   dtu_limit DTULimit, 
	   replica_role ReplicaRole
  From sys.dm_db_resource_stats
	    Cross Join sys.dm_os_sys_info osi;";
        }
    }
}
