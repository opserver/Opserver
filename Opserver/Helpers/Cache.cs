using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Status.Models;
using StackExchange.Status.Models.Dashboard;
using StackExchange.Status.Models.HAProxy;
using StackExchange.Status.Models.SQL;

namespace StackExchange.Status.Helpers
{
    public static class AppCache
    {
        #region Dashboard

        private static List<DashboardView> _dashboardViews;
        public static List<DashboardView> DashboardViews
        {
            get { return _dashboardViews ?? (_dashboardViews = Current.Settings.Dashboard.Views.All.Select(v => new DashboardView(v)).ToList()); }
        }

        public static ServerInfoCache ServerInfo
        {
            get
            {
                return Current.LocalCache.GetSet<ServerInfoCache>
                    ("server-cache",
                     (old, ctx) =>
                         {
                             try
                             {
                                 Dictionary<int, List<string>> nodeToIPs = null;
                                 Dictionary<string, int> ipToNodes = null;
                                 var servers = Models.Dashboard.ServerInfo.GetAll();
                                 try
                                 {
                                     var ips = Models.Dashboard.ServerInfo.GetIPs();
                                     nodeToIPs = ips.GroupBy(m => m.Item1).ToDictionary(g => g.Key, g => g.Select(i => i.Item2).OrderBy(ip => ip).ToList());
                                     var ipGroups = ips.GroupBy(m => m.Item2);
                                     ipToNodes = ipGroups.ToDictionary(g => g.Key, g => g.Max(i => i.Item1));
                                     var dupes = ipGroups.Where(g => g.Count() > 1).ToList();
                                     if (dupes.Any())
                                     {
                                         var dupeEx = new ApplicationException(string.Format("{0} Duplicate IP{1} found", dupes.Count, dupes.Count != 1 ? "s" : ""));
                                         foreach (var dupe in dupes)
                                         {
                                             var dupeNames = dupe.Select(i =>
                                                 {
                                                     var ipServer = servers != null
                                                                        ? servers.FirstOrDefault(s => s.Id == i.Item1)
                                                                        : null;
                                                     return ipServer != null ? ipServer.PrettyName : "Id: " + i.Item1;
                                                 });
                                             dupeEx.AddLoggedData(dupe.Key, string.Join(", ", dupeNames));
                                         }
                                         Current.LogException(dupeEx, key: "Cache.Servers.DuplicateIPs", reLogDelaySeconds: 5*60*60);
                                     }
                                 }
                                 catch (Exception e)
                                 {
                                     Current.LogException(e, key: "Cache.Servers");
                                     return new ServerInfoCache
                                     {
                                         Servers = servers,
                                         NodeIPs = nodeToIPs ?? (old != null ? old.NodeIPs : new Dictionary<int, List<string>>()),
                                         IPToNode = ipToNodes ?? (old != null ? old.IPToNode : new Dictionary<string, int>()),
                                         LastFetch = FetchInfo.Success(),
                                         LastSuccessfulFetch = FetchInfo.Success()
                                     };
                                 }
                                 return new ServerInfoCache
                                     {
                                         Servers = servers,
                                         NodeIPs = nodeToIPs,
                                         IPToNode = ipToNodes,
                                         LastFetch = FetchInfo.Success(),
                                         LastSuccessfulFetch = FetchInfo.Success()
                                     };
                             }
                             catch (Exception e)
                             {
                                 Current.LogException(e, key: "Cache.Servers.Outer");
                                 return new ServerInfoCache
                                     {
                                         Servers = old != null ? old.Servers : new List<ServerInfo>(),
                                         NodeIPs = old != null ? old.NodeIPs : new Dictionary<int, List<string>>(),
                                         IPToNode = old != null ? old.IPToNode : new Dictionary<string, int>(),
                                         LastFetch = FetchInfo.Fail(e.Message, e),
                                         LastSuccessfulFetch = old != null ? old.LastSuccessfulFetch : null
                                     };
                             }
                         }, 30, 24*60*60);
            }
        }

        public static List<Interface> Interfaces
        {
            get { return Current.LocalCache.GetSet<List<Interface>>("interface-cache", (old, ctx) => Interface.GetAll(), 30, 24 * 60 * 60); }
        }

        public static List<Volume> Volumes
        {
            get { return Current.LocalCache.GetSet<List<Volume>>("volume-cache", (old, ctx) => Volume.GetAll(), 120, 24 * 60 * 60); }
        }

        public static List<Application> Applications
        {
            get { return Current.LocalCache.GetSet<List<Application>>("application-cache", (old, ctx) => Application.GetAll(), 120, 24 * 60 * 60); }
        }

        public class ServerInfoCache
        {
            public List<ServerInfo> Servers { get; set; }
            public Dictionary<int, List<string>> NodeIPs { get; set; }
            public Dictionary<string, int> IPToNode { get; set; }
            public FetchInfo LastFetch { get; set; }
            public FetchInfo LastSuccessfulFetch { get; set; }
        }

        #endregion

        private static List<SQLCluster> _sqlClusters;
        public static List<SQLCluster> SQLClusters
        {
            get
            {
                if (_sqlClusters == null)
                {
                    if (Current.Settings.SQL.Enabled)
                        _sqlClusters = Current.Settings.SQL.Clusters.All.Select(c => new SQLCluster(c)).ToList();
                    else
                        _sqlClusters = new List<SQLCluster>();
                }
                return _sqlClusters;
            }
        }
        
        private static List<HAProxyInstance> _haProxyInstances;
        public static List<HAProxyInstance> HAProxyInstances
        {
            get
            {
                if (_haProxyInstances == null)
                {
                    if (Current.Settings.HAProxy.Enabled)
                        _haProxyInstances = Current.Settings.HAProxy.Instances.All.Select(c => new HAProxyInstance(c)).ToList();
                    else
                        _haProxyInstances = new List<HAProxyInstance>();
                }
                return _haProxyInstances;
            }
        }
    }
}