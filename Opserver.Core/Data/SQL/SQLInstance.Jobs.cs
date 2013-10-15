using System;
using System.Collections.Generic;
using System.Data;
using Dapper;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<SQLJobInfo>> _jobSummary;
        public Cache<List<SQLJobInfo>> JobSummary
        {
            get { return _jobSummary ?? (_jobSummary = SqlCacheList<SQLJobInfo>(2*60)); }
        }

        public bool ToggleJob(Guid jobId, bool enabled)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Execute("msdb.dbo.sp_update_job", new {job_id = jobId, enabled = enabled ? 1 : 0}, commandType: CommandType.StoredProcedure);
                    JobSummary.Purge();
                    return true;
                }
            }
            catch (Exception e)
            {
                Current.LogException(e);
                return false;
            }
        }

        public class SQLJobInfo : ISQLVersionedObject, IMonitorStatus
        {
            public Version MinVersion { get { return SQLServerVersions.SQL2005.RTM; } }

            public MonitorStatus MonitorStatus
            {
                get
                {
                    return !IsEnabled
                               ? MonitorStatus.Unknown
                               : IsRunning
                                     ? MonitorStatus.Good
                                     : LastRunMonitorStatus;
                }
            }
            public string MonitorStatusReason
            {
                get
                {
                    if (!IsEnabled) return "Not enabled";
                    if (IsRunning || LastRunMonitorStatus == MonitorStatus.Good) return null;
                    return Name + " - Last run: " +
                           (LastRunStatus.HasValue ? LastRunStatus.Value.GetDescription() : "unknown");
                }
            }

            public MonitorStatus LastRunMonitorStatus
            {
                get
                {
                    if (!LastRunStatus.HasValue) return MonitorStatus.Unknown;
                    switch (LastRunStatus.Value)
                    {
                        case JobStatuses.Succeeded:
                            return MonitorStatus.Good;
                        case JobStatuses.Retry:
                        case JobStatuses.Canceled:
                            return MonitorStatus.Warning;
                        case JobStatuses.Failed:
                            return MonitorStatus.Critical;
                        default:
                            throw new ArgumentOutOfRangeException("", "LastRunStatus was not recognized");
                    }
                }
            }

            public Guid JobId { get; internal set; }
            public string Name { get; internal set; }
            public string Description { get; internal set; }
            public DateTime DateCreated { get; internal set; }
            public DateTime DateModified { get; internal set; }
            public int Version { get; internal set; }
            public bool IsEnabled { get; internal set; }
            public bool IsRunning { get; internal set; }
            public string Category { get; internal set; }
            public JobStatuses? LastRunStatus { get; internal set; }
            public string LastRunMessage { get; internal set; }
            public JobRunSources LastRunRequestedSource { get; internal set; }
            public DateTime? LastRunRequestedDate { get; internal set; }
            public DateTime? LastStartDate { get; internal set; }
            public int? LastRunDurationSeconds { get; internal set; }
            public DateTime? LastStopDate { get; internal set; }
            public int? LastRunInstanceId { get; internal set; }
            public int? LastStepId { get; internal set; }
            public string LastStepName { get; internal set; }
            public DateTime? NextRunDate { get; internal set; }

            public TimeSpan? LastRunDuration
            {
                get { return LastRunDurationSeconds.HasValue ? TimeSpan.FromSeconds(LastRunDurationSeconds.Value) : (TimeSpan?)null; }
            }

            internal const string FetchSQL = @"
Select j.job_id JobId,
       j.name Name,
       j.description Description,
       j.date_created DateCreated,
       j.date_modified DateModified,
       j.version_number Version,
       Cast(j.enabled as bit) IsEnabled,
       Cast(Case When ja.run_requested_date Is Not Null and ja.stop_execution_date Is Null Then 1
                 Else 0 
            End as Bit) IsRunning,
       c.name as Category,
       jh.run_status LastRunStatus,
       jh.message LastRunMessage,
       Cast(ja.run_requested_source as int) LastRunRequestedSource,
       ja.run_requested_date LastRunRequestedDate,
       Coalesce(ja.start_execution_date, msdb.dbo.agent_datetime(jh.run_date, jh.run_time)) LastStartDate,
       (Case When ja.run_requested_date Is Not Null and ja.stop_execution_date Is Null 
             Then DateDiff(Second, ja.run_requested_date, GETUTCDATE())
             Else jh.run_duration % 100 + ROUND((jh.run_duration % 10000)/100,0,0)*60 + ROUND((jh.run_duration%1000000)/10000,0,0)*3600
        End) LastRunDurationSeconds,
       ja.stop_execution_date LastStopDate,
       ja.job_history_id LastRunInstanceId,
       ja.last_executed_step_id LastStepId,
       s.step_name as LastStepName,
       ja.next_scheduled_run_date NextRunDate
  From msdb.dbo.sysjobs j
       Join msdb.dbo.syscategories c On j.category_id = c.category_id
       Outer Apply (Select Top 1 *
                      From msdb.dbo.sysjobactivity ja
                     Where j.job_id = ja.job_id
                     Order By ja.run_requested_date Desc) ja
       Left Join msdb.dbo.sysjobhistory jh 
         On j.job_id = jh.job_id
         And ja.job_history_id = jh.instance_id
       Left Join msdb.dbo.sysjobsteps s
         On ja.job_id = s.job_id
         And ja.last_executed_step_id = s.step_id
Order By j.name, LastStartDate";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }
    }
}
