using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dapper;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Data.Dashboard;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance : PollNode, ISearchableNode
    {
        public string Name { get; internal set; }
        public string ObjectName { get; internal set; }
        public string CategoryName { get { return "SQL"; } }
        string ISearchableNode.DisplayName { get { return Name; } }
        protected string ConnectionString { get; set; }
        public Version Version { get; internal set; }
        public static Dictionary<Type, ISQLVersionedObject> VersionSingletons;

        static SQLInstance()
        {
            VersionSingletons = new Dictionary<Type, ISQLVersionedObject>();
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(typeof(ISQLVersionedObject).IsAssignableFrom))
            {
                if (!type.IsClass) continue;
                try
                {
                    VersionSingletons.Add(type, (ISQLVersionedObject)Activator.CreateInstance(type));
                }
                catch (Exception e)
                {
                    Current.LogException("Error creating ISQLVersionedObject lookup for " + type, e);
                }
            }
        }

        public SQLInstance(string name, string connectionString, string objectName) : base(name)
        {
            Version = new Version(); // default to 0.0
            Name = name;
            ObjectName = objectName.IsNullOrEmptyReturn(objectName, "SQLServer");
            ConnectionString = connectionString.IsNullOrEmptyReturn(Current.Settings.SQL.DefaultConnectionString.Replace("$ServerName$", name));
        }

        public static SQLInstance Get(string name)
        {
            return name.IsNullOrEmpty()
                       ? null
                       : AllInstances.FirstOrDefault(i => i.Name.ToLower() == name.ToLower());
        }

        public override string NodeType { get { return "SQL"; } }
        public override int MinSecondsBetweenPolls { get { return 2; } }

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return ServerProperties;
                yield return Configuration;
                yield return Databases;
                yield return DatabaseBackups;
                yield return DatabaseVLFs;
                yield return CPUHistoryLastHour;
                yield return JobSummary;
                yield return PerfCounters;
                yield return MemoryClerkSummary;
                yield return ServerFeatures;
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

        public Node ServerInfo
        {
            get { return DashboardData.GetNodeByName(Name); }
        }

        /// <summary>
        /// Gets a connection for this server - YOU NEED TO DISPOSE OF IT
        /// </summary>
        protected DbConnection GetConnection(int timeout = 5000)
        {
            return Connection.GetOpen(ConnectionString, connectionTimeout: timeout);
        }

        public string GetFetchSQL<T>() where T : ISQLVersionedObject
        {
            ISQLVersionedObject lookup;
            return VersionSingletons.TryGetValue(typeof (T), out lookup) ? lookup.GetFetchSQL(Version) : null;
        }

        private string GetCacheKey(string itemName) { return string.Format("SQL-Instance-{0}-{1}", Name, itemName); }

        public Cache<List<T>> SqlCacheList<T>(int cacheSeconds,
                                              int? cacheFailureSeconds = null,
                                              bool affectsStatus = true,
                                              [CallerMemberName] string memberName = "",
                                              [CallerFilePath] string sourceFilePath = "",
                                              [CallerLineNumber] int sourceLineNumber = 0) 
            where T : class, ISQLVersionedObject
        {
            return new Cache<List<T>>(memberName, sourceFilePath, sourceLineNumber)
                {
                    AffectsNodeStatus = affectsStatus,
                    CacheForSeconds = cacheSeconds,
                    CacheFailureForSeconds = cacheFailureSeconds,
                    UpdateCache = UpdateFromSql(typeof (T).Name + "-List", conn => conn.Query<T>(GetFetchSQL<T>()).ToList())
                };
        }

        public Cache<T> SqlCacheSingle<T>(int cacheSeconds,
                                              int? cacheFailureSeconds = null,
                                          [CallerMemberName] string memberName = "",
                                          [CallerFilePath] string sourceFilePath = "",
                                          [CallerLineNumber] int sourceLineNumber = 0)
            where T : class, ISQLVersionedObject
        {
            return new Cache<T>(memberName, sourceFilePath, sourceLineNumber)
                {
                    CacheForSeconds = cacheSeconds,
                    CacheFailureForSeconds = cacheFailureSeconds,
                    UpdateCache = UpdateFromSql(typeof (T).Name + "-Single", conn => conn.Query<T>(GetFetchSQL<T>()).FirstOrDefault())
                };
        }

        public Action<Cache<T>> UpdateFromSql<T>(string opName, Func<DbConnection, T> getFromConnection) where T : class
        {
            return UpdateCacheItem(description: "SQL Fetch: " + Name + ":" + opName,
                                   getData: () =>
                                       {
                                           using (var conn = GetConnection())
                                           {
                                               return getFromConnection(conn);
                                           }
                                       },
                                   addExceptionData: e => e.AddLoggedData("Server", Name));
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
