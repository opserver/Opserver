using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Opserver.Helpers;

namespace Opserver.Data.SQL
{
    public partial class SQLInstance : PollNode<SQLModule>, ISearchableNode
    {
        protected const string EmptyRecordsetSQL = "Select 1 Where 1 = 0";

        public string Name => Settings.Name;
        public virtual string Description => Settings.Description;
        private TimeSpan? _refreshInterval;
        public TimeSpan RefreshInterval => _refreshInterval ??= (Settings.RefreshIntervalSeconds ?? Module.Settings.RefreshIntervalSeconds).Seconds();
        public string ObjectName { get; internal set; }
        public string CategoryName => "SQL";
        string ISearchableNode.DisplayName => Name;
        protected string ConnectionString { get; set; }
        public SQLServerEngine Engine { get; internal set; } = new SQLServerEngine(new Version(), SQLServerEdition.Standard); // default to 0.0
        protected SQLSettings.Instance Settings { get; }

        protected static readonly ConcurrentDictionary<Tuple<string, SQLServerEngine>, string> QueryLookup =
            new ConcurrentDictionary<Tuple<string, SQLServerEngine>, string>();

        public string GetFetchSQL<T>() where T : ISQLVersioned, new() => GetFetchSQL<T>(Engine);
        public static string GetFetchSQL<T>(in SQLServerEngine e) where T : ISQLVersioned, new() => Singleton<T>.Instance.GetFetchSQL(e);

        public SQLInstance(SQLModule module, SQLSettings.Instance settings) : base(module, settings.Name)
        {
            Settings = settings;
            ConnectionString = settings.ConnectionString.IsNullOrEmptyReturn(Module.Settings.DefaultConnectionString.Replace("$ServerName$", settings.Name));
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

        public override string NodeType => "SQL";
        public override int MinSecondsBetweenPolls => 2;

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return ServerProperties;
                if (Supports<SQLServerFeatures>())
                    yield return ServerFeatures;
                if (Supports<SQLConfigurationOption>())
                    yield return Configuration;
                if (Supports<Database>())
                    yield return Databases;
                if (Supports<ResourceEvent>())
                    yield return ResourceHistory;
                if (Supports<SQLJobInfo>())
                    yield return JobSummary;
                if (Supports<PerfCounterRecord>())
                    yield return PerfCounters;
                if (Supports<SQLMemoryClerkSummaryInfo>())
                    yield return MemoryClerkSummary;
                if (Supports<TraceFlagInfo>())
                    yield return TraceFlags;
                if (Supports<VolumeInfo>())
                    yield return Volumes;
                if (Supports<SQLConnectionInfo>())
                    yield return Connections;
                if (Supports<SQLConnectionSummaryInfo>())
                    yield return ConnectionsSummary;
            }
        }

        public bool Supports<T>() where T : ISQLVersioned, new() => Engine.Version >= Singleton<T>.Instance.MinVersion && Singleton<T>.Instance.SupportedEditions.Contains(Engine.Edition);

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            if (!HasPolled)
                yield return MonitorStatus.Unknown;
            if (HasPolled && !HasPolledCacheSuccessfully)
                yield return MonitorStatus.Critical;
            if (Databases.Data != null)
                yield return Databases.Data.GetWorstStatus();
        }

        protected override string GetMonitorStatusReason()
        {
            return Databases.Data?.GetReasonSummary();
        }

        /// <summary>
        /// Gets a connection for this server - YOU NEED TO DISPOSE OF IT
        /// </summary>
        /// <param name="timeoutMs">Maximum milliseconds to wait when opening the connection.</param>
        protected Task<DbConnection> GetConnectionAsync(int timeoutMs = 5000) => Connection.GetOpenAsync(ConnectionString, connectionTimeoutMs: timeoutMs);

        /// <summary>
        /// Gets a connection for this server - YOU NEED TO DISPOSE OF IT
        /// TODO: Remove with async views in MVC Core
        /// </summary>
        /// <param name="timeoutMs">Maximum milliseconds to wait when opening the connection.</param>
        protected DbConnection GetConnection(int timeoutMs = 5000) => Connection.GetOpen(ConnectionString, connectionTimeoutMs: timeoutMs);

        private string GetCacheKey(string itemName) { return $"SQL-Instance-{Name}-{itemName}"; }

        public Cache<List<T>> SqlCacheList<T>(
            TimeSpan cacheDuration,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
            where T : class, ISQLVersioned, new()
        {
            return GetSqlCache(memberName,
                conn => conn.QueryAsync<T>(GetFetchSQL<T>()),
                Supports<T>,
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
                Supports<T>,
                cacheDuration,
                logExceptions: true,
                memberName: memberName,
                sourceFilePath: sourceFilePath,
                sourceLineNumber: sourceLineNumber);
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
                    using var conn = await GetConnectionAsync();
                    return await get(conn);
                },
                logExceptions: logExceptions,
                addExceptionData: e => e.AddLoggedData("Server", Name),
                memberName: memberName,
                sourceFilePath: sourceFilePath,
                sourceLineNumber: sourceLineNumber
            );
        }

        public LightweightCache<T> TimedCache<T>(string key, Func<DbConnection, T> get, TimeSpan duration, TimeSpan staleDuration) where T : class
            => LightweightCache<T>.Get(this, GetCacheKey(key),
            () =>
            {
                using var conn = GetConnection();
                return get(conn);
            }, duration, staleDuration);

        public override string ToString() => Name;
    }
}
