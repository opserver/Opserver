using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<SQLCPUEvent>> _cpuHistoryLastHour;
        public Cache<List<SQLCPUEvent>> CPUHistoryLastHour
        {
            get
            {
                return _cpuHistoryLastHour ?? (_cpuHistoryLastHour = new Cache<List<SQLCPUEvent>>
                    {
                        CacheForSeconds = 5*60,
                        UpdateCache = UpdateFromSql("CPUHistoryLastHour", conn =>
                            {
                                var sql = GetFetchSQL<SQLCPUEvent>();
                                var result = conn.Query<SQLCPUEvent>(sql, new {maxEvents = 60})
                                                 .OrderBy(e => e.EventTime)
                                                 .ToList();
                                CurrentCPUPercent = result.Count > 0 ? result.Last().ProcessUtilization : (int?) null;
                                return result;
                            })
                    });
            }
        }

        public int? CurrentCPUPercent { get; set; }

        public class SQLCPUEvent : ISQLVersionedObject
        {
            public Version MinVersion { get { return SQLServerVersions.SQL2005.RTM; } }

            public DateTime EventTime { get; internal set; }
            public int ProcessUtilization { get; internal set; }
            public int SystemIdle { get; internal set; }
            public int ExternalProcessUtilization { get { return 100 - SystemIdle - ProcessUtilization; } }

            private const string _fetchSQL = @"
Declare @ms_ticks bigint = (Select ms_ticks From sys.dm_os_sys_info);

Select Top (@maxEvents) DateAdd(ms, -1 * (@ms_ticks - timestamp_ms), GetDate()) as EventTime, 
	   ProcessUtilization,
	   SystemIdle
From (Select Record.value('(./Record/SchedulerMonitorEvent/SystemHealth/SystemIdle)[1]', 'int') as SystemIdle,
		     Record.value('(./Record/SchedulerMonitorEvent/SystemHealth/ProcessUtilization)[1]', 'int') as ProcessUtilization,
		     timestamp as timestamp_ms
	    From (Select timestamp, 
                     convert(xml, record) As Record 
		        From sys.dm_os_ring_buffers 
		       Where ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR'
		         And record Like '%<SystemHealth>%') x) y 
Order By timestamp_ms Desc";

            public string GetFetchSQL(Version v)
            {
                return _fetchSQL;
            }
        }
    }
}
