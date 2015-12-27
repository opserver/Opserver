using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
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
            // TODO: Object Name regex for not SQLServer but InstanceName, e.g. "MSSQL$MyInstance" from "MyServer\\MyInstance"
            // ...or ConnectionStringBuilder?
            ObjectName = settings.ObjectName.IsNullOrEmptyReturn("SQLServer");
            ConnectionString = settings.ConnectionString.IsNullOrEmptyReturn(Current.Settings.SQL.DefaultConnectionString.Replace("$ServerName$", settings.Name));
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
                yield return Configuration;
                yield return Databases;
                yield return CPUHistoryLastHour;
                yield return JobSummary;
                yield return PerfCounters;
                yield return MemoryClerkSummary;
                yield return ServerFeatures;
                yield return TraceFlags;
                yield return Volumes;
            }
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            if (Databases.HasData())
                yield return Databases.Data.GetWorstStatus();
        }
        protected override string GetMonitorStatusReason()
        {
            return Databases.HasData() ? Databases.Data.GetReasonSummary() : null;
        }

        public Node ServerInfo => DashboardData.GetNodeByName(Name);

        /// <summary>
        /// Gets a connection for this server - YOU NEED TO DISPOSE OF IT
        /// </summary>
        protected Task<DbConnection> GetConnectionAsync(int timeout = 5000) => Connection.GetOpenAsync(ConnectionString, connectionTimeout: timeout);

        private string GetCacheKey(string itemName) { return $"SQL-Instance-{Name}-{itemName}"; }

        public Cache<List<T>> SqlCacheList<T>(int cacheSeconds,
                                              int? cacheFailureSeconds = null,
                                              bool affectsStatus = true,
                                              [CallerMemberName] string memberName = "",
                                              [CallerFilePath] string sourceFilePath = "",
                                              [CallerLineNumber] int sourceLineNumber = 0) 
            where T : class, ISQLVersioned, new()
        {
            return new Cache<List<T>>(memberName, sourceFilePath, sourceLineNumber)
                {
                    AffectsNodeStatus = affectsStatus,
                    CacheForSeconds = cacheSeconds,
                    CacheFailureForSeconds = cacheFailureSeconds,
                    UpdateCache = UpdateFromSql(typeof (T).Name + "-List", conn => conn.QueryAsync<T>(GetFetchSQL<T>()))
                };
        }

        public Cache<T> SqlCacheSingle<T>(int cacheSeconds,
                                              int? cacheFailureSeconds = null,
                                          [CallerMemberName] string memberName = "",
                                          [CallerFilePath] string sourceFilePath = "",
                                          [CallerLineNumber] int sourceLineNumber = 0)
            where T : class, ISQLVersioned, new()
        {
            return new Cache<T>(memberName, sourceFilePath, sourceLineNumber)
                {
                    CacheForSeconds = cacheSeconds,
                    CacheFailureForSeconds = cacheFailureSeconds,
                    UpdateCache = UpdateFromSql(typeof (T).Name + "-Single", async conn => (await conn.QueryAsync<T>(GetFetchSQL<T>())).FirstOrDefault())
                };
        }

        public Action<Cache<T>> UpdateFromSql<T>(string opName, Func<DbConnection, Task<T>> getFromConnection) where T : class
        {
            return UpdateCacheItem(description: "SQL Fetch: " + Name + ":" + opName,
                                   getData: async () =>
                                       {
                                           using (var conn = await GetConnectionAsync())
                                           {
                                               return await getFromConnection(conn);
                                           }
                                       },
                                   addExceptionData: e => e.AddLoggedData("Server", Name));
        }

        public override string ToString() => Name;
    }
}
