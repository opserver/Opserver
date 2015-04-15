using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public abstract class DashboardDataProvider : PollNode
    {
        public abstract bool HasData { get; }
        public string Name { get; protected set; }
        public string Host { get; protected set; }
        public string ConnectionString { get; protected set; }
        public int QueryTimeoutMs { get; protected set; }
        
        protected DashboardDataProvider(string uniqueKey) : base(uniqueKey) { }

        protected DashboardDataProvider(DashboardSettings.ProviderSettings provider) : base(provider.Name)
        {
            Name = provider.Name;
            Host = provider.Host;
            ConnectionString = provider.ConnectionString;
            QueryTimeoutMs = provider.QueryTimeoutMs;
        }

        /// <summary>
        /// Returns the current exceptions for this data provider
        /// </summary>
        public virtual IEnumerable<string> GetExceptions()
        {
            foreach (var p in DataPollers)
            {
                if (p.ErrorMessage.HasValue())
                    yield return p.ParentMemberName + ": " + p.ErrorMessage;
            }
        }

        public abstract List<Node> AllNodes { get; }

        public abstract Node GetNode(string host);

        public abstract IEnumerable<Node> GetNodesByIP(IPAddress ip);

        public virtual string GetManagementUrl(Node node) { return null; }

        public abstract PointSeries GetSeries(string metric, string host, int secondsAgo, int? pointCount = null, params Tuple<string, string>[] tags);
        public abstract PointSeries GetSeries(string metric, string host, DateTime? start, DateTime? end, int? pointCount = null, params Tuple<string, string>[] tags);

        #region Cache

        protected Cache<T> ProviderCache<T>(
            Func<T> fetch,
            int cacheSeconds,
            int? cacheFailureSeconds = null,
            bool affectsStatus = true,
            bool logExceptions = false,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
            where T : class
        {
            return new Cache<T>(memberName, sourceFilePath, sourceLineNumber)
                {
                    AffectsNodeStatus = affectsStatus,
                    CacheForSeconds = cacheSeconds,
                    CacheFailureForSeconds = cacheFailureSeconds,
                    UpdateCache = UpdateFromProvider(typeof (T).Name + "-List", fetch, logExceptions)
                };
        }

        public Action<Cache<T>> UpdateFromProvider<T>(string opName, Func<T> fetch, bool logExceptions = false) where T : class
        {
            return UpdateCacheItem(description: "Data Provieder Fetch: " + NodeType + ":" + opName,
                                   getData: fetch,
                                   logExceptions: logExceptions,
                                   addExceptionData: e => e.AddLoggedData("NodeType", NodeType));
        }

        #endregion
    }
}
