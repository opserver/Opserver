using System;
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
        public Cache<List<TopOperation>> GetTopOperations(TopSearchOptions options = null)
        {
            return new Cache<List<TopOperation>>
                {
                    CacheKey = GetCacheKey("TopOperations-" + (options == null ? 0 : options.GetHashCode())),
                    CacheForSeconds = 15,
                    CacheStaleForSeconds = 5*60,
                    UpdateCache = UpdateFromSql("Top Operations", conn =>
                        {
                            var sql = GetFetchSQL<TopOperation>() + (options != null ? options.ToSQLWhere() + options.ToSQLOrder() : "");
                            return conn.Query<TopOperation>(sql, options).ToList();
                        })
                };
        }

        public Cache<TopOperation> GetTopOperation(byte[] planHandle, int? statementStartOffset = null)
        {
            string sql = GetFetchSQL<TopOperation>() + " WHERE (qs.plan_handle = @planHandle OR qs.sql_handle = @planHandle)";
            if (statementStartOffset.HasValue) sql += " And qs.statement_start_offset = @statementStartOffset";
            return new Cache<TopOperation>
                {
                    CacheKey = GetCacheKey("TopOperation-" + planHandle.GetHashCode() + "-" + statementStartOffset),
                    CacheForSeconds = 60,
                    CacheStaleForSeconds = 5*60,
                    UpdateCache = UpdateFromSql("Top Operations",
                                                conn =>
                                                conn.Query<TopOperation>(sql, new {planHandle, statementStartOffset, MaxResultCount = 1}).FirstOrDefault())
                };
        }

        public class TopOperation : ISQLVersionedObject
        {
            // ReSharper disable UnusedAutoPropertyAccessor.Local
            public Version MinVersion { get { return SQLServerVersions.SQL2005.RTM; } }

            public long AvgCPU { get; private set; }
            public long TotalCPU { get; private set; }
            public long AvgCPUPerMinute { get; private set; }
            public decimal PercentCPU { get; private set; }
            public long AvgDuration { get; private set; }
            public long TotalDuration { get; private set; }
            public decimal PercentDuration { get; private set; }
            public long AvgReads { get; private set; }
            public long TotalReads { get; private set; }
            public decimal PercentReads { get; private set; }
            public long ExecutionCount { get; private set; }
            public decimal PercentExecutions { get; private set; }
            public decimal ExecutionsPerMinute { get; private set; }
            public DateTime PlanCreationTime { get; private set; }
            public DateTime LastExecutionTime { get; private set; }
            public string QueryText { get; private set; }
            public string QueryPlan { get; private set; }
            public byte[] PlanHandle { get; private set; }
            public int StatementStartOffset { get; private set; }
            public int StatementEndOffset { get; private set; }
            public long MinReturnedRows { get; private set; }
            public long MaxReturnedRows { get; private set; }
            public decimal AvgReturnedRows { get; private set; }
            public long TotalReturnedRows { get; private set; }
            public long LastReturnedRows { get; private set; }
            public string CompiledOnDatabase { get; private set; }
            // ReSharper restore UnusedAutoPropertyAccessor.Local

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
SELECT TOP (@MaxResultCount) total_worker_time / execution_count AS AvgCPU,
		total_elapsed_time / execution_count AS AvgDuration,
		total_logical_reads / execution_count AS AvgReads,
        CAST(total_worker_time / execution_count * (Case When DATEDIFF(mi, creation_time, qs.last_execution_time) > 0 Then CAST((1.00 * execution_count / DATEDIFF(mi, creation_time, qs.last_execution_time)) AS MONEY) Else Null End) AS BIGINT) AS AvgCPUPerMinute,
		(Case When DATEDIFF(mi, creation_time, qs.last_execution_time) > 0 Then CAST((1.00 * execution_count / DATEDIFF(mi, creation_time, qs.last_execution_time)) AS MONEY) Else Null End) AS ExecutionsPerMinute,
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
        SUBSTRING(st.text, ( qs.statement_start_offset / 2 ) + 1, ( ( CASE qs.statement_end_offset
                                                                      WHEN -1 THEN DATALENGTH(st.text)
                                                                      ELSE qs.statement_end_offset
                                                                      END - qs.statement_start_offset ) / 2 ) + 1) AS QueryText,
        query_plan AS QueryPlan,
        qs.plan_handle AS PlanHandle,
        qs.statement_start_offset AS StatementStartOffset,
        qs.statement_end_offset AS StatementEndOffset,
        qs.min_rows AS MinReturnedRows,
        qs.max_rows AS MaxReturnedRows,
        CAST(qs.total_rows as MONEY) / execution_count AS AvgReturnedRows,
        qs.total_rows AS TotalReturnedRows,
        qs.last_rows AS LastReturnedRows,
        DB_NAME(qp.dbid) AS CompiledOnDatabase
    FROM sys.dm_exec_query_stats AS qs
         CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS st 
         CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) AS qp 
         CROSS JOIN (SELECT SUM(execution_count) TotalExecs,
                            SUM(total_elapsed_time) TotalElapsed,
                            SUM(total_worker_time) TotalWorker,
                            SUM(total_logical_reads) TotalReads
                       FROM sys.dm_exec_query_stats) AS t
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

            public static TopSearchOptions Default
            {
                get { return new TopSearchOptions().SetDefaults(); }
            }

            public TopSearchOptions SetDefaults()
            {
                Sort = Sort ?? TopSorts.AvgCPUPerMinute;
                MinExecs = MinExecs ?? 25;
                LastRunSeconds = LastRunSeconds ?? 24*60*60;
                MinLastRunDate = MinLastRunDate ?? DateTime.UtcNow.AddDays(-1);
                MaxResultCount = MaxResultCount ?? 100;
                return this;
            }

            public string ToSQLWhere()
            {
                var clauses = new List<string>();
                if (MinExecs.GetValueOrDefault(0) > 0) clauses.Add("execution_count >= @MinExecs");
                if (MinExecsPerMin.GetValueOrDefault(0) > 0) clauses.Add("(Case When DATEDIFF(mi, creation_time, qs.last_execution_time) > 0 Then CAST((1.00 * execution_count / DATEDIFF(mi, creation_time, qs.last_execution_time)) AS money) Else Null End) >= @MinExecsPerMin");
                if (Search.HasValue()) clauses.Add("SUBSTRING(st.text, ( qs.statement_start_offset / 2 ) + 1, ( ( CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(st.text) ELSE qs.statement_end_offset END - qs.statement_start_offset ) / 2 ) + 1) Like '%' + @Search + '%'");
                if (MinLastRunDate.HasValue) clauses.Add("qs.last_execution_time >= @MinLastRunDate");

                return clauses.Any() ? " WHERE " + string.Join("\n  AND ", clauses) : "";
            }

            public string ToSQLOrder()
            {
                return Sort.HasValue ? string.Format("\nORDER BY {0} DESC", Sort) : "";
            }
        }
    }
}
