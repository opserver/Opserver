using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public abstract class DashboardDataProvider : PollNode
    {
        public abstract bool HasData { get; }
        public string Name { get; protected set; }
        public string ConnectionString { get; protected set; }
        public int QueryTimeoutMs { get; protected set; }
        
        protected DashboardDataProvider(string uniqueKey) : base(uniqueKey) { }

        protected DashboardDataProvider(DashboardSettings.Provider provider) : base(provider.Name)
        {
            Name = provider.Name;
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

        #region Nodes

        public abstract List<Node> AllNodes { get; }

        public Node GetNode(int id)
        {
            return AllNodes.FirstOrDefault(s => s.Id == id);
        }

        public Node GetNode(string hostName)
        {
            if (!Current.Settings.Dashboard.Enabled || hostName.IsNullOrEmpty()) return null;
            return AllNodes.FirstOrDefault(s => s.Name.ToLowerInvariant().Contains(hostName.ToLowerInvariant()));
        }

        public abstract IEnumerable<Node> GetNodesByIP(IPAddress ip);
        public abstract IEnumerable<IPAddress> GetIPsForNode(Node node);

        public virtual string GetManagementUrl(Node node) { return null; }
        public abstract Task<IEnumerable<Node.CPUUtilization>> GetCPUUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null);
        public abstract Task<IEnumerable<Node.MemoryUtilization>> GetMemoryUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null);

        #endregion

        #region Interfaces

        public abstract List<Interface> AllInterfaces { get; }

        public Interface GetInterface(int id)
        {
            return AllInterfaces.FirstOrDefault(i => i.Id == id);
        }

        public abstract Task<IEnumerable<Interface.InterfaceUtilization>> GetUtilization(Interface volume, DateTime? start, DateTime? end, int? pointCount = null);

        #endregion

        #region Volumes

        public abstract List<Volume> AllVolumes { get; }

        public Volume GetVolume(int id)
        {
            return AllVolumes.FirstOrDefault(v => v.Id == id);
        }

        public abstract Task<IEnumerable<Volume.VolumeUtilization>> GetUtilization(Volume volume, DateTime? start, DateTime? end, int? pointCount = null);

        #endregion

        #region Applications

        public abstract List<Application> AllApplications { get; }

        public Application GetApplication(int id)
        {
            return AllApplications.FirstOrDefault(a => a.Id == id);
        }

        #endregion

        #region Cache

        protected Cache<T> ProviderCache<T>(
            Func<Task<T>> fetch,
            int cacheSeconds,
            int? cacheFailureSeconds = null,
            bool affectsStatus = true,
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
                    UpdateCache = UpdateFromProvider(typeof (T).Name + "-List", fetch)
                };
        }

        public Action<Cache<T>> UpdateFromProvider<T>(string opName, Func<Task<T>> fetch) where T : class
        {
            return UpdateCacheItem(description: "Data Provieder Fetch: " + NodeType + ":" + opName,
                                   getData: fetch,
                                   addExceptionData: e => e.AddLoggedData("NodeType", NodeType));
        }

        #endregion

    }
}
