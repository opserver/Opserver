using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using StackExchange.Opserver.Data.SQL.QueryPlans;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        public Cache<List<ActiveOperation>> GetActiveOperations(ActiveSearchOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options), "Active Operations requires options");

            return new Cache<List<ActiveOperation>>
                {
                    CacheKey = GetCacheKey("ActiveOperations-" + options.GetHashCode()),
                    CacheForSeconds = 5,
                    CacheStaleForSeconds = 5*60,
                    UpdateCache = UpdateFromSql("ActiveOperations", async conn => (await conn.QueryAsync<WhoIsActiveRow>(options.ToSQLQuery(), options, commandTimeout: 300))
                                                                                .Select(row => new ActiveOperation(row))
                                                                                .ToList())
                };
        }

        public class WhoIsActiveRow
        {
            // ReSharper disable InconsistentNaming
            public Int16 session_id { get; internal set; }
            public string sql_text { get; internal set; }
            public string sql_command { get; internal set; }
            public string login_name { get; internal set; }
            public string wait_info { get; internal set; }
            public Int16? tasks { get; internal set; }
            public string tran_log_writes { get; internal set; }
            public int? CPU { get; internal set; }

            public long? tempdb_allocations { get; internal set; }
            public long? tempdb_current { get; internal set; }
            public Int16? blocking_session_id { get; internal set; }
            
            public long? reads { get; internal set; }
            public long? writes { get; internal set; }
            public long? physical_reads { get; internal set; }
            public string query_plan { get; internal set; }
            public long used_memory { get; internal set; }

            public string status { get; internal set; }
            public DateTime? tran_start_time { get; internal set; }
            public Int16? open_tran_count { get; internal set; }
            public Single? percent_complete { get; internal set; }
            public string host_name { get; internal set; }
            public string database_name { get; internal set; }

            public string program_name { get; internal set; }
            public DateTime start_time { get; internal set; }
            public DateTime? login_time { get; internal set; }
            public DateTime collection_time { get; internal set; }

            public string additional_info { get; internal set; }
            // ReSharper restore InconsistentNaming
        }

        public class ActiveOperation
        {
            public TimeSpan Duration => CollectionTime - StartTime;
            public Int16 SessionId { get; internal set; }

            private string _sqlText;
            public string SqlText
            {
                get { return _sqlText; }
                set { _sqlText = value.IsNullOrEmptyReturn("").Replace("<?query --\r\n", "").Replace("\r\n--?>", ""); }
            }
            public string SqlCommand { get; internal set; }
            private string _loginName;
            public string LoginName
            {
                get { return _loginName; }
                set { _loginName = _loginLookups.ContainsKey(value) ? _loginLookups[value] : value.Split(StringSplits.BackSlash).Last(); }
            }
            public string WaitInfo { get; internal set; }
            public Int16? Tasks { get; internal set; }
            public string TransactionLogWrites { get; internal set; }

            public int? CPU { get; internal set; }

            public long? TempDBAllocations { get; internal set; }
            public long? TempDBCurrent { get; internal set; }
            public Int16? BlockingSessionId { get; internal set; }

            public long? Reads { get; internal set; }
            public long? Writes { get; internal set; }
            public long? ContextSwitches { get; internal set; }
            public long? PhysicalIO { get; internal set; }

            public long? PhysicalReads { get; internal set; }
            public string QueryPlan { get; internal set; }
            public long UsedMemory { get; internal set; }

            public string Status { get; internal set; }
            public DateTime? TransactionStartTime { get; internal set; }
            public Int16? OpenTransactionCount { get; internal set; }
            public Single? PercentComplete { get; internal set; }
            public string HostName { get; internal set; }
            public string DatabaseName { get; internal set; }

            public string ProgramName { get; internal set; }
            public DateTime StartTime { get; internal set; }
            public DateTime? LoginTime { get; internal set; }
            public DateTime CollectionTime { get; internal set; }

            // TODO: Additional Info
            
            public ShowPlanXML GetShowPlanXML()
            {
                if (QueryPlan == null) return new ShowPlanXML();
                var s = new XmlSerializer(typeof(ShowPlanXML));
                using (var r = new StringReader(QueryPlan))
                {
                    return (ShowPlanXML)s.Deserialize(r);
                }
            }

            public ActiveOperation(WhoIsActiveRow row)
            {
                SessionId = row.session_id;
                SqlText = row.sql_text;
                SqlCommand = row.sql_command;
                LoginName = row.login_name;
                WaitInfo = row.wait_info;
                Tasks = row.tasks;
                TransactionLogWrites = row.tran_log_writes;
                CPU = row.CPU;

                TempDBAllocations = row.tempdb_allocations;
                TempDBCurrent = row.tempdb_current;
                BlockingSessionId = row.blocking_session_id;

                Reads = row.reads;
                Writes = row.writes;
                PhysicalReads = row.physical_reads;
                QueryPlan = row.query_plan;
                UsedMemory = row.used_memory;

                Status = row.status;
                TransactionStartTime = row.tran_start_time;
                OpenTransactionCount = row.open_tran_count;
                HostName = row.host_name;
                DatabaseName = row.database_name;

                ProgramName = row.program_name;
                StartTime = row.start_time;
                LoginTime = row.login_time;
                CollectionTime = row.collection_time;
            }

            internal const string FetchSQL = @"
Exec sp_WhoIsActive @format_output = 0;
";

            private static readonly Dictionary<string, string> _loginLookups = new Dictionary<string, string>
            {
                {"NT AUTHORITY\\SYSTEM", "(Local System)"}
            };
        }

        public class ActiveSearchOptions
        {
            /// <summary>
            /// Whether to include this session
            /// </summary>
            public bool IncludeOwnSession { get; set; }

            /// <summary>
            /// Whether to include system sessions
            /// </summary>
            public bool IncludeSystemSessions { get; set; }

            /// <summary>
            /// Wether to show sleeping sessions, by default only those with an open transaction are included
            /// </summary>
            public ShowSleepingSessionOptions IncludeSleepingSessions { get; set; }

            /// <summary>
            /// Wether to get the full batch
            /// If true, gets the full stored procedure or running batch, when available
	        /// If false, gets only the actual statement that is currently running in the batch or procedure
            /// </summary>
            public bool GetFullInnerText { get; set; }

            /// <summary>
            /// Pulls the plans associated with each operation
            /// </summary>
            public GetPlansOptions GetPlans { get; set; }

            /// <summary>
            /// Whether the outer command running (e.g. the whole stored procedure)
            /// </summary>
            public bool GetOuterCommand { get; set; }

            /// <summary>
            /// Enables pulling transaction log write info and transaction duration
            /// </summary>
            public bool GetTransactionInfo { get; set; }

            /// <summary>
            /// Get information on active tasks, based on three interest levels
	        /// None does not pull any task-related information
            /// Lightweight is a lightweight mode that pulls the top non-CXPACKET wait, giving preference to blockers
	        /// AllAvailable pulls all available task-based metrics, including: number of active tasks, current wait stats, physical I/O, context switches, and blocker information
            /// </summary>
            public GetTaskInfoOptions GetTaskInfo { get; set; }

            /// <summary>
            /// Returns additional non-performance-related session/request information
            /// If the script finds a SQL Agent job running, the name of the job and job step will be reported
            /// If GetTaskInfo = AllAvailable and the WhoIsActive finds a lock wait, the locked object will be reported
            /// </summary>
            public bool GetAdditionalInfo { get; set; }
            /// <summary>
            /// WARNING: Very Expensive
            /// Gets associated locks for each request
            /// </summary>
            public bool GetLocks { get; set; }

            /// <summary>
            /// Field to filter on
            /// </summary>
            public FilterFields FilterField { get; set; }
            /// <summary>
            /// The filter to apply to that field, "" or 0 for session return all sessions
            /// Use % for LIKE matches, or set WildcardSearch to true to default to %FilterValue% searches
            /// </summary>
            public string FilterValue { get; set; }
            /// <summary>
            /// Whether to search all FilterValue as a contains rather than exact match
            /// </summary>
            public bool WildcardSearch { get; set; }

            public string FilterFieldString => FilterField.ToString().ToLower();
            
            public string FilterValueString => FilterValue.HasValue() ? string.Format("{0}{1}{0}", WildcardSearch ? "%" : "", FilterValue) : "";

            //TODO: Sort Order
            
            public ActiveSearchOptions()
            {
                // Setup the default options
                //TODO: Settings driving these?
                GetPlans = GetPlansOptions.ByPlanHandle;
                IncludeSleepingSessions = ShowSleepingSessionOptions.OpenTransaction;
                GetTransactionInfo = true;
                GetTaskInfo = GetTaskInfoOptions.AllAvailable;
                GetAdditionalInfo = true;
                FilterValue = "";
                WildcardSearch = true;
            }

            public string ToSQLQuery()
            {
                return @"
Exec sp_WhoIsActive 
    @format_output = 0,
    @show_own_spid = @IncludeOwnSession,
    @show_system_spids = @IncludeSystemSessions,
    @show_sleeping_spids = @IncludeSleepingSessions,
    @get_full_inner_text = @GetFullInnerText,
    @get_plans = @GetPlans,
    @get_outer_command = @GetOuterCommand,
    @get_transaction_info = @GetTransactionInfo,
    @get_task_info = @GetTaskInfo,
    @get_additional_info = @GetAdditionalInfo,
    @get_locks = @GetLocks,
    @filter_type = @FilterFieldString,
    @filter = @FilterValueString;
";
            }

            public enum ShowSleepingSessionOptions
            {
                None = 0,
                [Description("Open Transactions")]
                OpenTransaction = 1,
                All = 2
            }

            public enum GetPlansOptions
            {
                None = 0,
                [Description("By Statement Offset")]
                ByStatementOffset = 1,
                [Description("By Plan Handle")]
                ByPlanHandle = 2
            }

            public enum GetTaskInfoOptions
            {
                None = 0,
                Lightweight = 1,
                [Description("AllAvailable")]
                AllAvailable = 2
            }

            public enum FilterFields
            {
                Session = 0,
                Program = 1,
                Database = 2,
                Login = 3,
                Host = 4
            }
        }

    }
}
