using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public abstract class DashboardDataProvider<TSettings> : DashboardDataProvider where TSettings : class, IProviderSettings
    {
        public TSettings Settings { get; protected set; }

        protected DashboardDataProvider(TSettings settings) : base(settings)
        {
            Settings = settings;
        }
    }

    public abstract class DashboardDataProvider : PollNode
    {
        public abstract bool HasData { get; }
        public string Name { get; protected set; }
        
        protected DashboardDataProvider(string uniqueKey) : base(uniqueKey) { }

        protected DashboardDataProvider(IProviderSettings settings) : base(settings.Name + "Dashboard")
        {
            Name = settings.Name;
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

        public Node GetNodeById(string id)
        {
            return AllNodes.FirstOrDefault(s => s.Id == id);
        }

        public Node GetNodeByHostname(string hostName)
        {
            if (!Current.Settings.Dashboard.Enabled || hostName.IsNullOrEmpty()) return null;
            return AllNodes.FirstOrDefault(s => s.Name.ToLowerInvariant().Contains(hostName.ToLowerInvariant()));
        }
        
        public virtual IEnumerable<Node> GetNodesByIP(IPAddress ip)
        {
            return AllNodes.Where(n => n.IPs?.Contains(ip) == true);
        }

        public virtual string GetManagementUrl(Node node) { return null; }
        public abstract Task<List<Node.CPUUtilization>> GetCPUUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null);
        public abstract Task<List<Node.MemoryUtilization>> GetMemoryUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null);
        
        public Interface GetInterface(string id) => AllNodes.SelectMany(n => n.Interfaces.Where(i => i.Id == id)).FirstOrDefault();

        public abstract Task<List<Interface.InterfaceUtilization>> GetUtilization(Interface iface, DateTime? start, DateTime? end, int? pointCount = null);
        
        public Volume GetVolume(string id) => AllNodes.SelectMany(n => n.Volumes.Where(v => v.Id == id)).FirstOrDefault();

        public abstract Task<List<Volume.VolumeUtilization>> GetUtilization(Volume volume, DateTime? start, DateTime? end, int? pointCount = null);
        
        public Application GetApplication(string id) => AllNodes.SelectMany(n => n.Apps.Where(a => a.Id == id)).FirstOrDefault();

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
