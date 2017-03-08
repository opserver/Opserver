﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Dapper;
using StackExchange.Opserver.Data.SQL.QueryPlans;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        public LightweightCache<List<TopOperation>> GetTopOperations(TopSearchOptions options = null)
        {
            return TimedCache(nameof(GetTopOperations) + "-" + (options?.GetHashCode() ?? 0).ToString(),
                conn =>
                {
                    if (options != null && options.LastRunSeconds.HasValue)
                    {
                        var offset = ServerProperties.Data.TimeZoneInfo.GetUtcOffset(DateTime.UtcNow);

                        options.LastRunSeconds -= (Int32)offset.TotalSeconds;
                    }

                    var hasOptions = options != null;
                    var sql = string.Format(GetFetchSQL<TopOperation>(),
                        hasOptions ? options.ToSQLWhere() + options.ToSQLOrder() : "",
                        hasOptions ? options.ToSQLSearch() : "");
                    sql = sql.Replace("query_plan AS QueryPlan,", "")
                        .Replace("CROSS APPLY sys.dm_exec_query_plan(PlanHandle) AS qp", "");
                    var operations = conn.Query<TopOperation>(sql, options).AsList();

                    var timezone = ServerProperties.Data.TimeZoneInfo;
                    foreach (var operation in operations)
                    {
                        ConvertDateTimesToUtc(operation, timezone);
                    }

                    return operations;
                }, 10.Seconds(), 5.Minutes());
        }

        public LightweightCache<TopOperation> GetTopOperation(byte[] planHandle, int? statementStartOffset = null)
        {
            var timezone = ServerProperties.Data.TimeZoneInfo;
            var clause = " And (qs.plan_handle = @planHandle OR qs.sql_handle = @planHandle)";
            if (statementStartOffset.HasValue) clause += " And qs.statement_start_offset = @statementStartOffset";
            var sql = string.Format(GetFetchSQL<TopOperation>(), clause, "");
            return TimedCache(nameof(GetTopOperation) + "-" + planHandle.GetHashCode().ToString() + "-" + statementStartOffset.ToString(),
                conn => ConvertDateTimesToUtc(conn.QueryFirstOrDefault<TopOperation>(sql, new { planHandle, statementStartOffset, MaxResultCount = 1 }), timezone),
                60.Seconds(), 60.Seconds());
        }

        private TopOperation ConvertDateTimesToUtc(TopOperation operation, TimeZoneInfo timezone)
        {
            operation.PlanCreationTime = operation.PlanCreationTime.ToUniversalTime(timezone);
            operation.LastExecutionTime = operation.LastExecutionTime.ToUniversalTime(timezone);

            return operation;
        }

        public class TopOperation : ISQLVersioned
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            public long AvgCPU { get; internal set; }
            public long TotalCPU { get; internal set; }
            public long AvgCPUPerMinute { get; internal set; }
            public long AvgCPUPerMinuteLifetime { get; internal set; }
            public decimal PercentCPU { get; internal set; }
            public long AvgDuration { get; internal set; }
            public long TotalDuration { get; internal set; }
            public decimal PercentDuration { get; internal set; }
            public long AvgReads { get; internal set; }
            public long TotalReads { get; internal set; }
            public decimal PercentReads { get; internal set; }
            public long ExecutionCount { get; internal set; }
            public decimal PercentExecutions { get; internal set; }
            public decimal ExecutionsPerMinute { get; internal set; }
            public decimal ExecutionsPerMinuteLifetime { get; internal set; }
            public DateTime PlanCreationTime { get; internal set; }
            public DateTime LastExecutionTime { get; internal set; }
            public string QueryText { get; internal set; }
            public string FullText { get; internal set; }
            public string QueryPlan { get; internal set; }
            public byte[] PlanHandle { get; internal set; }
            public int StatementStartOffset { get; internal set; }
            public int StatementEndOffset { get; internal set; }
            public long MinReturnedRows { get; internal set; }
            public long MaxReturnedRows { get; internal set; }
            public decimal AvgReturnedRows { get; internal set; }
            public long TotalReturnedRows { get; internal set; }
            public long LastReturnedRows { get; internal set; }
            public string CompiledOnDatabase { get; internal set; }

            public string ReadablePlanHandle
            {
                get { return string.Join(string.Empty, PlanHandle.Select(x => x.ToString("X2"))); }
            }
            
            public ShowPlanXML GetShowPlanXML()
            {
                if (QueryPlan == null) return new ShowPlanXML();
                var s = new XmlSerializer(typeof(ShowPlanXML));
                using (var r = new StringReader(QueryPlan))
                {
                    return (ShowPlanXML)s.Deserialize(r);
                }
            }

            internal const string FetchSQL = @"
SELECT AvgCPU, AvgDuration, AvgReads, AvgCPUPerMinute,
       TotalCPU, TotalDuration, TotalReads,
       PercentCPU, PercentDuration, PercentReads, PercentExecutions,
       ExecutionCount,
       ExecutionsPerMinute,
       PlanCreationTime, LastExecutionTime,
       SUBSTRING(st.text,
                 (StatementStartOffset / 2) + 1,
                 ((CASE StatementEndOffset
                   WHEN -1 THEN DATALENGTH(st.text)
                   ELSE StatementEndOffset
                   END - StatementStartOffset) / 2) + 1) AS QueryText,
        st.Text FullText,
        query_plan AS QueryPlan,
        PlanHandle,
        StatementStartOffset,
        StatementEndOffset,
        MinReturnedRows,
        MaxReturnedRows,
        AvgReturnedRows,
        TotalReturnedRows,
        LastReturnedRows,
        DB_NAME(DatabaseId) AS CompiledOnDatabase
FROM (SELECT TOP (@MaxResultCount) 
             total_worker_time / execution_count AS AvgCPU,
             total_elapsed_time / execution_count AS AvgDuration,
             total_logical_reads / execution_count AS AvgReads,
             Cast(total_worker_time / age_minutes As BigInt) AS AvgCPUPerMinute,
             execution_count / age_minutes AS ExecutionsPerMinute,
             Cast(total_worker_time / age_minutes_lifetime As BigInt) AS AvgCPUPerMinuteLifetime,
             execution_count / age_minutes_lifetime AS ExecutionsPerMinuteLifetime,
             total_worker_time AS TotalCPU,
             total_elapsed_time AS TotalDuration,
             total_logical_reads AS TotalReads,
             execution_count ExecutionCount,
             CAST(ROUND(100.00 * total_worker_time / t.TotalWorker, 2) AS MONEY) AS PercentCPU,
             CAST(ROUND(100.00 * total_elapsed_time / t.TotalElapsed, 2) AS MONEY) AS PercentDuration,
             CAST(ROUND(100.00 * total_logical_reads / t.TotalReads, 2) AS MONEY) AS PercentReads,
             CAST(ROUND(100.00 * execution_count / t.TotalExecs, 2) AS MONEY) AS PercentExecutions,
             qs.creation_time AS PlanCreationTime,
             qs.last_execution_time AS LastExecutionTime,
             qs.plan_handle AS PlanHandle,
             qs.statement_start_offset AS StatementStartOffset,
             qs.statement_end_offset AS StatementEndOffset,
             qs.min_rows AS MinReturnedRows,
             qs.max_rows AS MaxReturnedRows,
             CAST(qs.total_rows as MONEY) / execution_count AS AvgReturnedRows,
             qs.total_rows AS TotalReturnedRows,
             qs.last_rows AS LastReturnedRows,
             qs.sql_handle AS SqlHandle,
			 Cast(pa.value as Int) DatabaseId
        FROM (SELECT *, 
                     CAST((CASE WHEN DATEDIFF(second, creation_time, GETDATE()) > 0 And execution_count > 1
                                THEN DATEDIFF(second, creation_time, GETDATE()) / 60.0
                                ELSE Null END) as MONEY) as age_minutes, 
                     CAST((CASE WHEN DATEDIFF(second, creation_time, last_execution_time) > 0 And execution_count > 1
                                THEN DATEDIFF(second, creation_time, last_execution_time) / 60.0
                                ELSE Null END) as MONEY) as age_minutes_lifetime
                FROM sys.dm_exec_query_stats) AS qs
             CROSS JOIN(SELECT SUM(execution_count) TotalExecs,
                               SUM(total_elapsed_time) TotalElapsed,
                               SUM(total_worker_time) TotalWorker,
                               SUM(total_logical_reads) TotalReads
                          FROM sys.dm_exec_query_stats) AS t
             CROSS APPLY sys.dm_exec_plan_attributes(qs.plan_handle) AS pa
     WHERE pa.attribute = 'dbid'
       {0}) sq
    CROSS APPLY sys.dm_exec_sql_text(SqlHandle) AS st
    CROSS APPLY sys.dm_exec_query_plan(PlanHandle) AS qp
{1}
";
            public string GetFetchSQL(Version v)
            {
                // Row info added in 2008 R2 SP2
                if (v < SQLServerVersions.SQL2008R2.SP2)
                {
                    return FetchSQL.Replace("qs.min_rows", "0")
                                   .Replace("qs.max_rows","0")
                                   .Replace("qs.total_rows","0")
                                   .Replace("qs.last_rows","0");
                }
                return FetchSQL;
            }
        }

        public enum TopSorts
        {
            [Description("Average CPU")] AvgCPU,
            [Description("Average CPU per minute")] AvgCPUPerMinute,
            [Description("Total CPU")] TotalCPU,
            [Description("Percent of Total CPU")] PercentCPU,
            [Description("Average Duration")] AvgDuration,
            [Description("Total Duration")] TotalDuration,
            [Description("Percent of Total Duration")] PercentDuration,
            [Description("Average Reads")] AvgReads,
            [Description("Total Reads")] TotalReads,
            [Description("Average Rows")] AvgReturnedRows,
            [Description("Total Rows")] TotalReturnedRows,
            [Description("Percent of Total Reads")] PercentReads,
            [Description("Execution Count")] ExecutionCount,
            [Description("Percent of Total Executions")] PercentExecutions,
            [Description("Executions per minute")] ExecutionsPerMinute,
            [Description("Plan Creation Time")] PlanCreationTime,
            [Description("Last Execution Time")] LastExecutionTime
        }

        public class TopSearchOptions
        {
            public TopSorts? Sort { get; set; }
            public int? MinExecs { get; set; }
            public int? MinExecsPerMin { get; set; }
            public DateTime? MinLastRunDate { get; set; }
            private int? _lastRunSeconds;

            public int? LastRunSeconds
            {
                get { return _lastRunSeconds; }
                set
                {
                    if (!value.HasValue) return;
                    _lastRunSeconds = value;
                    MinLastRunDate = DateTime.UtcNow.AddSeconds(-1 * value.Value);
                }
            }

            public string Search { get; set; }
            public int? MaxResultCount { get; set; }
            public int? Database { get; set; }
            
            public static readonly TopSearchOptions Default = new TopSearchOptions().SetDefaults();

            private const int DefaultMinExecs = 25;
            private const int DefaultLastRunSeconds = 24*60*60;
            private const int DefaultMaxResultCount = 100;

            public TopSearchOptions SetDefaults()
            {
                Sort = Sort ?? TopSorts.AvgCPUPerMinute;
                MinExecs = MinExecs ?? DefaultMinExecs;
                LastRunSeconds = LastRunSeconds ?? DefaultLastRunSeconds;
                MinLastRunDate = MinLastRunDate ?? DateTime.UtcNow.AddDays(-1);
                MaxResultCount = MaxResultCount ?? DefaultMaxResultCount;
                return this;
            }

            public bool IsNonDefault
            {
                get
                {
                    if (MinExecs != DefaultMinExecs) return true;
                    if (LastRunSeconds != DefaultLastRunSeconds) return true;
                    if (MaxResultCount != DefaultMaxResultCount) return true;
                    if (Database.HasValue) return true;
                    if (Search.HasValue()) return true;
                    return false;
                }
            }

            public string ToSQLWhere()
            {
                var clauses = new List<string>();
                if (MinExecs.GetValueOrDefault(0) > 0) clauses.Add("execution_count >= @MinExecs");
                if (MinExecsPerMin.GetValueOrDefault(0) > 0) clauses.Add("(Case When DATEDIFF(mi, creation_time, qs.last_execution_time) > 0 Then CAST((1.00 * execution_count / DATEDIFF(mi, creation_time, qs.last_execution_time)) AS money) Else Null End) >= @MinExecsPerMin");
                if (MinLastRunDate.HasValue) clauses.Add("qs.last_execution_time >= @MinLastRunDate");
                if (Database.HasValue) clauses.Add("Cast(pa.value as Int) = @Database");

                return clauses.Any() ? "\n       And " + string.Join("\n       And ", clauses) : "";
            }

            public string ToSQLSearch()
            {
                return Search.HasValue() ? @"Where SUBSTRING(st.text,
                 (StatementStartOffset / 2) + 1,
                 ((CASE StatementEndOffset
                   WHEN -1 THEN DATALENGTH(st.text)
                   ELSE StatementEndOffset
                   END - StatementStartOffset) / 2) + 1) Like '%' + @Search + '%'" : "";
            }

            public string ToSQLOrder()
            {
                return Sort.HasValue ? $"\nORDER BY {Sort} DESC" : "";
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = _lastRunSeconds.GetHashCode();
                    hashCode = (hashCode*397) ^ Sort.GetHashCode();
                    hashCode = (hashCode*397) ^ MinExecs.GetHashCode();
                    hashCode = (hashCode*397) ^ MinExecsPerMin.GetHashCode();
                    hashCode = (hashCode*397) ^ (Search?.GetHashCode() ?? 0);
                    hashCode = (hashCode*397) ^ MaxResultCount.GetHashCode();
                    hashCode = (hashCode*397) ^ Database.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}
