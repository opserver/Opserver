using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Opserver.Data.Dashboard;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<ResourceEvent>> _resourceHistory;
        public Cache<List<ResourceEvent>> ResourceHistory
        {
            get
            {
                return _resourceHistory ?? (_resourceHistory = new Cache<List<ResourceEvent>>
                {
                    CacheForSeconds = RefreshInterval,
                    UpdateCache = UpdateFromSql(nameof(ResourceHistory), async conn =>
                    {
                        var sql = GetFetchSQL<ResourceEvent>();
                        var result = await conn.QueryAsync<ResourceEvent>(sql).ConfigureAwait(false);
                        CurrentCPUPercent = result.Count > 0 ? result.Last().ProcessUtilization : (int?) null;
                        return result;
                    })
                });
            }
        }

        public int? CurrentCPUPercent { get; set; }

        public class ResourceEvent : ISQLVersioned, IGraphPoint
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            private long? _dateEpoch;
            public long DateEpoch => _dateEpoch ?? (_dateEpoch = EventTime.ToEpochTime()).Value;
            public DateTime EventTime { get; internal set; }
            public int ProcessUtilization { get; internal set; }
            public int MemoryUtilization { get; internal set; }
            public int SystemIdle { get; internal set; }
            public int ExternalProcessUtilization => 100 - SystemIdle - ProcessUtilization;
            
            public string GetFetchSQL(Version v) => @"
Select DateAdd(s, (timestamp - (osi.cpu_ticks / Convert(Float, (osi.cpu_ticks / osi.ms_ticks)))) / 1000, GETDATE()) AS EventTime,
	   Record.value('(./Record/SchedulerMonitorEvent/SystemHealth/SystemIdle)[1]', 'int') as SystemIdle,
	   Record.value('(./Record/SchedulerMonitorEvent/SystemHealth/ProcessUtilization)[1]', 'int') as ProcessUtilization,
	   Record.value('(./Record/SchedulerMonitorEvent/SystemHealth/MemoryUtilization)[1]', 'int') as MemoryUtilization
  From (Select timestamp, 
               convert(xml, record) As Record 
	      From sys.dm_os_ring_buffers 
		 Where ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR'
		   And record Like '%<SystemHealth>%') x
	    Cross Join sys.dm_os_sys_info osi
Order By timestamp";
        }
    }
}
