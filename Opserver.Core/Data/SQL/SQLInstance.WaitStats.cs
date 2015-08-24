using System;
using System.Collections.Generic;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<WaitStatRecord>> _waitStats;
        public Cache<List<WaitStatRecord>> WaitStats
        {
            get
            {
                return _waitStats ?? (_waitStats = new Cache<List<WaitStatRecord>>
                {
                    CacheForSeconds = 60,
                    UpdateCache = UpdateFromSql("WaitStats", conn =>
                    {
                        var sql = GetFetchSQL<WaitStatRecord>();
                        return conn.QueryAsync<WaitStatRecord>(sql, new { secondsBetween = 15 });
                    })
                });
            }
        }

        public class WaitStatRecord : ISQLVersionedObject
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            public string WaitType { get; internal set; }
            public int SecondsBetween { get; internal set; }
            public DateTime CreationDate { get; internal set; }
            public long WaitTimeMs { get; internal set; }
            public long WaitTaskCount { get; internal set; }

            private bool? _isIgnorable;

            public bool IsIgnorable => _isIgnorable ?? (_isIgnorable = IsIgnorableWait(WaitType)).Value;

            public static bool IsIgnorableWait(string waitType)
            {
                switch (waitType)
                {
                    case "BROKER_EVENTHANDLER":
                    case "BROKER_RECEIVE_WAITFOR":
                    case "BROKER_TASK_STOP":
                    case "BROKER_TO_FLUSH":
                    case "CHECKPOINT_QUEUE":
                    case "CLR_AUTO_EVENT":
                    case "CLR_MANUAL_EVENT":
                    case "DBMIRROR_DBM_MUTEX":
                    case "DBMIRROR_EVENTS_QUEUE":
                    case "DBMIRRORING_CMD":
                    case "DIRTY_PAGE_POLL":
                    case "DISPATCHER_QUEUE_SEMAPHORE":
                    case "FT_IFTS_SCHEDULER_IDLE_WAIT":
                    case "FT_IFTSHC_MUTEX":
                    case "HADR_FILESTREAM_IOMGR_IOCOMPLETION":
                    case "LAZYWRITER_SLEEP":
                    case "LOGMGR_QUEUE":
                    case "ONDEMAND_TASK_QUEUE":
                    case "QDS_CLEANUP_STALE_QUERIES_TASK_MAIN_LOOP_SLEEP":
                    case "QDS_PERSIST_TASK_MAIN_LOOP_SLEEP":
                    case "REQUEST_FOR_DEADLOCK_SEARCH":
                    case "SLEEP_TASK":
                    case "SP_SERVER_DIAGNOSTICS_SLEEP":
                    case "SQLTRACE_BUFFER_FLUSH":
                    case "SQLTRACE_INCREMENTAL_FLUSH_SLEEP":
                    case "WAITFOR":
                    case "XE_DISPATCHER_WAIT":
                    case "XE_TIMER_EVENT":
                        return true;
                    default:
                        return false;
                }
            }

            public double AverageWaitTime => (double)WaitTimeMs/SecondsBetween;

            public double AverageTaskCount => (double)WaitTaskCount / SecondsBetween;

            internal string FetchSQL = @"
Declare @delayInterval char(8) = Convert(Char(8), DateAdd(Second, @secondsBetween, '00:00:00'), 108);

If Object_Id('tempdb..#PWaitStats') Is Not Null
    Drop Table #PWaitStats;
If Object_Id('tempdb..#CWaitStats') Is Not Null
    Drop Table #CWaitStats;

  Select wait_type WaitType,
         GETDATE() CreationDate,
         Sum(wait_time_ms) WaitTimeMs,
         Sum(waiting_tasks_count) WaitTaskCount
    Into #PWaitStats
    From sys.dm_os_wait_stats
Group By wait_type;

WaitFor Delay @delayInterval;

  Select wait_type WaitType,
         GETDATE() CreationDate,
         Sum(wait_time_ms) WaitTimeMs,
         Sum(waiting_tasks_count) WaitTaskCount
    Into #CWaitStats
    From sys.dm_os_wait_stats
Group By wait_type;

Select cw.WaitType,
       DateDiff(Second, pw.CreationDate, cw.CreationDate) SecondsBetween,
       cw.CreationDate,
       cw.WaitTimeMs - pw.WaitTimeMs WaitTimeMs,
       cw.WaitTaskCount - pw.WaitTaskCount WaitTaskCount
  From #PWaitStats pw
       Join #CWaitStats cw On pw.WaitType = cw.WaitType
 Where cw.WaitTaskCount - pw.WaitTaskCount > 0

Drop Table #PWaitStats;
Drop Table #CWaitStats;";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }
    }
}
