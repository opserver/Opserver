using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<SQLConnectionSummaryInfo>> _connectionsSummary;
        public Cache<List<SQLConnectionSummaryInfo>> ConnectionsSummary
        {
            get { return _connectionsSummary ?? (_connectionsSummary = SqlCacheList<SQLConnectionSummaryInfo>(30)); }
        }

        private Cache<List<SQLConnectionInfo>> _connections;
        public Cache<List<SQLConnectionInfo>> Connections
        {
            get { return _connections ?? (_connections = SqlCacheList<SQLConnectionInfo>(10)); }
        }

        public class SQLConnectionSummaryInfo : ISQLVersionedObject
        {
            public Version MinVersion { get { return SQLServerVersions.SQL2005.SP2; } }

            public string LoginName { get; private set; }
            public string HostName { get; private set; }
            public TransactionIsolationLevel TransactionIsolationLevel { get; private set; }
            public DateTime LastConnectTime { get; private set; }
            public int ConnectionCount { get; private set; }
            public long TotalReads { get; private set; }
            public long TotalWrites { get; private set; }
            
            internal const string FetchSQL = @"
DECLARE @UTCOffset INT = DATEDIFF(MI, GETUTCDATE(), GETDATE())
Select s.login_name LoginName,
       s.host_name HostName,
       s.transaction_isolation_level TransactionIsolationLevel,
       DATEADD(mi, -@UTCOffset, Max(c.connect_time)) LastConnectTime,
       Count(*) ConnectionCount,
       Sum(Cast(c.num_reads as BigInt)) TotalReads, 
       Sum(Cast(c.num_writes as BigInt)) TotalWrites
  From sys.dm_exec_connections c
       Join sys.dm_exec_sessions s
         On c.most_recent_session_id = s.session_id
 Group By s.login_name, s.host_name, s.transaction_isolation_level";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }

        public class SQLConnectionInfo : ISQLVersionedObject
        {
            public Version MinVersion { get { return SQLServerVersions.SQL2005.RTM; } }

            public Guid Id { get; private set; }
            public DateTime ConnectTime { get; private set; }
            public byte[] PlanHandle { get; private set; }
            public string QueryText { get; private set; }
            public string LocalNetAddress { get; private set; }
            public int LocalTCPPort { get; private set; }
            public int NumReads { get; private set; }
            public int NumWrites { get; private set; }
            public int SessionId { get; private set; }
            public DateTime LoginTime { get; private set; }
            public string SessionStatus { get; private set; }
            public TransactionIsolationLevel TransactionIsolationLevel { get; private set; }

            // SQL 2005 SP2+ columns
            public int HostProcessId { get; private set; }
            public string LoginName { get; private set; }
            public string HostName { get; private set; }
            public string ProgramName { get; private set; }

            public string ReadablePlanHandle
            {
                get { return string.Join(string.Empty, PlanHandle.Select(x => x.ToString("X2"))); }
            }

            internal const string FetchSQL2005SP2Colums = @"
       s.host_process_id HostProcessId,
       s.login_name LoginName,
       s.host_name HostName,
       s.program_name ProgramName,";

            internal const string FetchSQL = @"
DECLARE @UTCOffset INT = DATEDIFF(MI, GETUTCDATE(), GETDATE())
Select c.connection_id Id, 
       DATEADD(mi, -@UTCOffset, c.connect_time) ConnectTime, 
       c.most_recent_sql_handle PlanHandle, 
       st.text QueryText,  
       c.client_net_address ClientNetAddress, 
       c.client_tcp_port ClientTCPPort, 
       c.local_net_address LocalNetAddress, 
       c.local_tcp_port LocalTCPPort,
       c.num_reads NumReads, 
       c.num_writes NumWrites,
       s.transaction_isolation_level TransactionIsolationLevel,
       s.session_id SessionId, 
       DATEADD(mi, -@UTCOffset, s.login_time) LoginTime, {0}       
       s.status SessionStatus
  From sys.dm_exec_connections c
       Join sys.dm_exec_sessions s
         On c.most_recent_session_id = s.session_id
       Cross Apply sys.dm_exec_sql_text(c.most_recent_sql_handle) st
Order By c.num_writes + c.num_reads Desc";

            public string GetFetchSQL(Version v)
            {
                if (v >= SQLServerVersions.SQL2005.SP2)
                    return string.Format(FetchSQL, FetchSQL2005SP2Colums);
                return string.Format(FetchSQL, "");
            }
        }
    }
}
