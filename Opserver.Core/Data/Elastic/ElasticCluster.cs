using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Elastic;
using StackExchange.Opserver.Monitoring;
using StackExchange.Profiling;
using IProfilerProvider = StackExchange.Elastic.IProfilerProvider;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster : PollNode, ISearchableNode, IMonitedService
    {
        string ISearchableNode.Name => SettingsName;
        string ISearchableNode.DisplayName => SettingsName;
        public int RefreshInterval => Settings.RefreshIntervalSeconds;
        string ISearchableNode.CategoryName => "elastic";
        public ElasticSettings.Cluster Settings { get; }
        private string SettingsName => Settings.Name;
        public List<ElasticNode> SettingsNodes { get; set; }
        public ConnectionManager ConnectionManager { get; set; }

        public ElasticCluster(ElasticSettings.Cluster cluster) : base(cluster.Name)
        {
            Settings = cluster;
            SettingsNodes = cluster.Nodes.Select(n => new ElasticNode(n)).ToList();
            ConnectionManager = new ConnectionManager(SettingsNodes.Select(n => n.Url), new ElasticProfilerProvider());
        }

        private sealed class ElasticProfilerProvider : IProfilerProvider
        {
            public IProfiler GetProfiler() => new LightweightProfiler(MiniProfiler.Current, "elastic");
        }

        public class ElasticNode
        {
            private const int DefaultElasticPort = 9200;

            public string Host { get; set; }
            public int Port { get; set; }

            public string Url { get; set; }

            public ElasticNode(string hostAndPort)
            {
                Uri uri;
                if (Uri.TryCreate(hostAndPort, UriKind.Absolute, out uri))
                {
                    Url = uri.ToString();
                    Host = uri.Host;
                    Port = uri.Port;
                    return;
                }

                var parts = hostAndPort.Split(StringSplits.Colon);
                if (parts.Length == 2)
                {
                    Host = parts[0];
                    int port;
                    if (int.TryParse(parts[1], out port))
                    {
                        Port = port;
                    }
                    else
                    {
                        Current.LogException(new ConfigurationErrorsException(
                            $"Invalid port specified for {parts[0]}: '{parts[1]}'"));
                        Port = DefaultElasticPort;
                    }
                }
                else
                {
                    Host = hostAndPort;
                    Port = DefaultElasticPort;
                }
                Url = $"http://{Host}:{Port.ToString()}/";
            }
        }

        public override string NodeType => "elastic";
        public override int MinSecondsBetweenPolls => 5;

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
            if (HealthStatus.Data?.Indices != null)
                yield return HealthStatus.Data.Indices.GetWorstStatus();
            yield return DataPollers.GetWorstStatus();
        }
        protected override string GetMonitorStatusReason()
        {
            return HealthStatus.Data?.Indices?.GetReasonSummary();
        }

        // TODO: Poll down nodes faster?
        //var hs = result as ClusterHealthStatusInfo;
        //if (hs != null)
        //{
        //    _settingsRefreshIntervalSeconds = hs.MonitorStatus == MonitorStatus.Good
        //                                        ? (int?)null
        //                                        : ConfigSettings.DownRefreshIntervalSeconds;
        //}
        
        public Action<Cache<T>> UpdateFromElastic<T>(string opName, Func<SearchClient, Task<T>> getFromClient) where T : class, new()
        {
            return UpdateCacheItem(description: "Elastic Fetch: " + SettingsName + ":" + typeof (T).Name,
                getData: async () =>
                {
                    var cli = ConnectionManager.GetClient();
                    return await getFromClient(cli).ConfigureAwait(false);
                },
                addExceptionData:
                    e => e.AddLoggedData("Cluster", SettingsName)
                        .AddLoggedData("Type", typeof (T).Name));
        }

        public override string ToString() => SettingsName;
    }
}