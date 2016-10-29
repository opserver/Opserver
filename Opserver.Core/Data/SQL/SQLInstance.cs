using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Data.Dashboard;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance : PollNode, ISearchableNode
    {
        public string Name => Settings.Name;
        private TimeSpan? _refreshInterval;
        public TimeSpan RefreshInterval => _refreshInterval ?? (_refreshInterval = (Settings.RefreshIntervalSeconds ?? Current.Settings.SQL.RefreshIntervalSeconds).Seconds()).Value;
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
                if (Version >= Singleton<SQLServerFeatures>.Instance.MinVersion)
                    yield return ServerFeatures;
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

        /// <summary>
        /// Gets a connection for this server - YOU NEED TO DISPOSE OF IT
        /// TODO: Remove with async views in MVC Core
        /// </summary>
        protected DbConnection GetConnection(int timeout = 5000) => Connection.GetOpen(ConnectionString, connectionTimeout: timeout);

        private string GetCacheKey(string itemName) { return $"SQL-Instance-{Name}-{itemName}"; }

        public Cache<List<T>> SqlCacheList<T>(
            TimeSpan cacheDuration,
            bool affectsStatus = true,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
            where T : class, ISQLVersioned, new()
        {
            return GetSqlCache(memberName,
                conn => conn.QueryAsync<T>(GetFetchSQL<T>()),
                () => Singleton<T>.Instance.MinVersion < Version,
                cacheDuration,
                memberName: memberName,
                sourceFilePath: sourceFilePath,
                sourceLineNumber: sourceLineNumber
            );
        }

        public Cache<T> SqlCacheSingle<T>(
            TimeSpan cacheDuration,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
            where T : class, ISQLVersioned, new()
        {
            return GetSqlCache(memberName,
                conn => conn.QueryFirstOrDefaultAsync<T>(GetFetchSQL<T>()),
                () => Singleton<T>.Instance.MinVersion < Version,
                cacheDuration,
                memberName: memberName,
                sourceFilePath: sourceFilePath,
                sourceLineNumber: sourceLineNumber,
                logExceptions: true
            );
        }
        
        protected Cache<T> GetSqlCache<T>(
            string opName,
            Func<DbConnection, Task<T>> get,
            Func<bool> shouldRun = null,
            TimeSpan? cacheDuration = null,
            bool logExceptions = false,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            ) where T : class, new()
        {
            return new Cache<T>(this, "SQL Fetch: " + Name + ":" + opName,
                cacheDuration ?? RefreshInterval,
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

        public LightweightCache<T> TimedCache<T>(string key, Func<DbConnection, T> get, TimeSpan duration, TimeSpan staleDuration) where T : class
        => Cache.GetTimedCache(GetCacheKey(key),
            () =>
            {
                using (var conn = GetConnection())
                {
                    return get(conn);
                }
            }, duration, staleDuration);

        public override string ToString() => Name;
    }
}
