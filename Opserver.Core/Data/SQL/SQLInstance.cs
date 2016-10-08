using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dapper;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Data.Dashboard;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance : PollNode, ISearchableNode
    {
        public string Name => Settings.Name;
        public int RefreshInterval => Settings.RefreshIntervalSeconds ?? Current.Settings.SQL.RefreshIntervalSeconds;
        public string ObjectName { get; internal set; }
        public string CategoryName => "SQL";
        string ISearchableNode.DisplayName => Name;
        protected string ConnectionString { get; set; }
        public Version Version { get; internal set; } = new Version(); // default to 0.0
        protected SQLSettings.Instance Settings { get; }
        
        protected static readonly ConcurrentDictionary<Tuple<string, Version>, string> QueryLookup =
            new ConcurrentDictionary<Tuple<string, Version>, string>();

        public string GetFetchSQL<T>() where T : ISQLVersioned, new() => GetFetchSQL<T>(Version);
        public string GetFetchSQL<T>(Version v) where T : ISQLVersioned, new() =>
            Singleton<T>.Instance.GetFetchSQL(v);

        public SQLInstance(SQLSettings.Instance settings) : base(settings.Name)
        {
            Settings = settings;
            ConnectionString = settings.ConnectionString.IsNullOrEmptyReturn(Current.Settings.SQL.DefaultConnectionString.Replace("$ServerName$", settings.Name));
            // Grab the instance name for performance counters and such
            var csb = new SqlConnectionStringBuilder(ConnectionString);
            var parts = csb.DataSource?.Split(StringSplits.BackSlash);
            if (Settings.ObjectName.HasValue())
            {
                ObjectName = Settings.ObjectName;
            }
            else
            {
                ObjectName = parts?.Length == 2 ? "MSSQL$" + parts[1].ToUpper() : "SQLServer";
            }
        }

        public static SQLInstance Get(string name)
        {
            return AllInstances.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }

        public override string NodeType => "SQL";
        public override int MinSecondsBetweenPolls => 2;

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return ServerProperties;
                if (Version >= Singleton<SQLConfigurationOption>.Instance.MinVersion)
                    yield return Configuration;
                if (Version >= Singleton<Database>.Instance.MinVersion)
                    yield return Databases;
                if (Version >= Singleton<ResourceEvent>.Instance.MinVersion)
                    yield return ResourceHistory;
                if (Version >= Singleton<SQLJobInfo>.Instance.MinVersion)
                    yield return JobSummary;
                if (Version >= Singleton<PerfCounterRecord>.Instance.MinVersion)
                    yield return PerfCounters;
                if (Version >= Singleton<SQLMemoryClerkSummaryInfo>.Instance.MinVersion)
                    yield return MemoryClerkSummary;
                if (Version >= Singleton<SQLServerFeatures>.Instance.MinVersion)
                    yield return ServerFeatures;
                if (Version >= Singleton<TraceFlagInfo>.Instance.MinVersion)
                    yield return TraceFlags;
                if (Version >= Singleton<VolumeInfo>.Instance.MinVersion)
                    yield return Volumes;
                if (Version >= Singleton<SQLConnectionInfo>.Instance.MinVersion)
                    yield return Connections;
                if (Version >= Singleton<SQLConnectionSummaryInfo>.Instance.MinVersion)
                    yield return ConnectionsSummary;
            }
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            if (Databases.Data != null)
                yield return Databases.Data.GetWorstStatus();
        }
        protected override string GetMonitorStatusReason()
        {
            return Databases.Data?.GetReasonSummary();
        }

        public Node ServerInfo => DashboardData.GetNodeByName(Name);

        /// <summary>
        /// Gets a connection for this server - YOU NEED TO DISPOSE OF IT
        /// </summary>
        protected Task<DbConnection> GetConnectionAsync(int timeout = 5000) => Connection.GetOpenAsync(ConnectionString, connectionTimeout: timeout);

        private string GetCacheKey(string itemName) { return $"SQL-Instance-{Name}-{itemName}"; }

        public Cache<List<T>> SqlCacheList<T>(
            int cacheSeconds,
            int? cacheFailureSeconds = null,
            bool affectsStatus = true,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
            where T : class, ISQLVersioned, new()
        {
            return GetSqlCache(memberName,
                conn => conn.QueryAsync<T>(GetFetchSQL<T>()),
                () => Singleton<T>.Instance.MinVersion > Version,
                cacheSeconds,
                cacheFailureSeconds,
                memberName: memberName,
                sourceFilePath: sourceFilePath,
                sourceLineNumber: sourceLineNumber
            );
        }

        public Cache<T> SqlCacheSingle<T>(
            int cacheSeconds,
            int? cacheFailureSeconds = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
            where T : class, ISQLVersioned, new()
        {
            return GetSqlCache(memberName,
                conn => conn.QueryFirstOrDefaultAsync<T>(GetFetchSQL<T>()),
                () => Singleton<T>.Instance.MinVersion > Version,
                cacheSeconds,
                cacheFailureSeconds,
                memberName: memberName,
                sourceFilePath: sourceFilePath,
                sourceLineNumber: sourceLineNumber
            );
        }
        
        protected Cache<T> GetSqlCache<T>(
            string opName,
            Func<DbConnection, Task<T>> get,
            Func<bool> shouldRun = null,
            int? cacheSeconds = null,
            int? cacheFailureSeconds = null,
            bool logExceptions = false,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            ) where T : class, new()
        {
            return new Cache<T>(this, "SQL Fetch: " + Name + ":" + opName,
                cacheSeconds ?? RefreshInterval,
                getData: async () =>
                {
                    if (shouldRun != null && !shouldRun()) return new T();
                    using (var conn = await GetConnectionAsync().ConfigureAwait(false))
                    {
                        return await get(conn).ConfigureAwait(false);
                    }
                },
                logExceptions: logExceptions,
                addExceptionData: e => e.AddLoggedData("Server", Name),
                memberName: memberName,
                sourceFilePath: sourceFilePath,
                sourceLineNumber: sourceLineNumber
            );
        }

        public override string ToString() => Name;
    }
}
