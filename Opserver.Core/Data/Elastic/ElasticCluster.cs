using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster : PollNode, ISearchableNode, IMonitedService
    {
        string ISearchableNode.DisplayName { get { return SettingsName; } }
        string ISearchableNode.Name { get { return SettingsName; } }
        string ISearchableNode.CategoryName { get { return "elastic"; } }
        private string SettingsName { get; set; }
        public List<ElasticNode> SettingsNodes { get; set; }

        public ElasticCluster(ElasticSettings.Cluster cluster) : base(cluster.Name)
        {
            SettingsName = cluster.Name;
            SettingsNodes = cluster.Nodes.Select(n => new ElasticNode(n)).ToList();
        }

        public class ElasticNode
        {
            public string Host { get; set; }
            public int Port { get; set; }

            public ElasticNode(string hostAndPort)
            {
                var parts = hostAndPort.Split(StringSplits.Colon);
                if (parts.Length == 2)
                {
                    Host = parts[0];
                    int port;
                    if (!int.TryParse(parts[1], out port))
                    {
                        Current.LogException(new ConfigurationErrorsException(string.Format("Invalid port specified for {0}: '{1}'", parts[0], parts[1])));
                        Port = 9200;
                    }
                    Port = port;
                }
                else
                {
                    Host = hostAndPort;
                    Port = 9200;
                }
            }
        }

        public override string NodeType { get { return "elastic"; } }
        public override int MinSecondsBetweenPolls { get { return 5; } }
        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return Nodes;
                yield return Stats;
                yield return Status;
                yield return HealthStatus;
                yield return Aliases;
            }
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            if (HealthStatus.Data != null && HealthStatus.Data.Indices != null)
                yield return HealthStatus.Data.Indices.GetWorstStatus();
        }
        protected override string GetMonitorStatusReason()
        {
            if (HealthStatus.Data != null && HealthStatus.Data.Indices != null)
                return HealthStatus.Data.Indices.GetReasonSummary();
            return null;
        }

        // TODO: Poll down nodes faster?
        //var hs = result as ClusterHealthStatusInfo;
        //if (hs != null)
        //{
        //    _settingsRefreshIntervalSeconds = hs.MonitorStatus == MonitorStatus.Good
        //                                        ? (int?)null
        //                                        : ConfigSettings.DownRefreshIntervalSeconds;
        //}

        private Cache<T> GetCache<T>(int seconds,
                                     [CallerMemberName] string memberName = "",
                                     [CallerFilePath] string sourceFilePath = "",
                                     [CallerLineNumber] int sourceLineNumber = 0) where T : ElasticDataObject, new()
        {
            return new Cache<T>(memberName, sourceFilePath, sourceLineNumber)
                {
                    CacheForSeconds = seconds,
                    UpdateCache = UpdateFromElastic<T>()
                };
        }

        public Action<Cache<T>> UpdateFromElastic<T>() where T : ElasticDataObject, new()
        {
            return UpdateCacheItem(description: "Elastic Fetch: " + SettingsName + ":" + typeof(T).Name,
                                   getData: () =>
                                       {
                                           var result = new T();
                                           result.PopulateFromConnections(SettingsNodes.Select(n => n.Host + ":" + n.Port));
                                           return result;
                                       },
                                   addExceptionData:
                                       e =>
                                       e.AddLoggedData("Cluster", SettingsName)
                                        .AddLoggedData("Type", typeof (T).Name));
        }

        public override string ToString()
        {
            return SettingsName;
        }
    }
}
