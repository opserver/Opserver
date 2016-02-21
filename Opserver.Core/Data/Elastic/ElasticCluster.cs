using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Profiling;

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

        public ElasticCluster(ElasticSettings.Cluster cluster) : base(cluster.Name)
        {
            Settings = cluster;
            KnownNodes = cluster.Nodes.Select(n => new ElasticNode(n)).ToList();
        }

        public override string NodeType => "elastic";
        public override int MinSecondsBetweenPolls => 5;

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return Nodes;
                yield return IndexStats;
                yield return State;
                yield return HealthStatus;
                yield return Aliases;
            }
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            if (HealthStatus.Data?.Indexes != null)
                yield return HealthStatus.Data.Indexes.Values.GetWorstStatus();
            if (KnownNodes.All(n => n.LastException != null))
                yield return MonitorStatus.Critical;
            if (KnownNodes.Any(n => n.LastException != null))
                yield return MonitorStatus.Warning;

            yield return DataPollers.GetWorstStatus();
        }
        protected override string GetMonitorStatusReason()
        {
            var reason = HealthStatus.Data?.Indexes?.Values.GetReasonSummary();
            if (reason.HasValue()) return reason;

            var sb = StringBuilderCache.Get();
            foreach (var node in KnownNodes)
            {
                if (node.LastException != null)
                {
                    sb.Append(node.Name.IsNullOrEmptyReturn(node.Host))
                        .Append(": ")
                        .Append(node.LastException.Message)
                        .Append("; ");
                }
            }

            if (sb.Length > 2)
                sb.Length -= 2;

            return sb.ToStringRecycle();
        }

        // TODO: Poll down nodes faster?
        //var hs = result as ClusterHealthStatusInfo;
        //if (hs != null)
        //{
        //    _settingsRefreshIntervalSeconds = hs.MonitorStatus == MonitorStatus.Good
        //                                        ? (int?)null
        //                                        : ConfigSettings.DownRefreshIntervalSeconds;
        //}

        public async Task<T> GetAsync<T>(string path) where T : class
        {
            using(MiniProfiler.Current.CustomTiming("elastic", path))
            foreach (var n in KnownNodes)
            {
                var result = await n.GetAsync<T>(path).ConfigureAwait(false);
                if (result != null)
                    return result;
            }
            return null;
        }
        
        public Func<Cache<T>, Task> UpdateFromElastic<T>(string opName, Func<Task<T>> get) where T : class, new()
        {
            return UpdateCacheItem(description: "Elastic Fetch: " + SettingsName + ":" + typeof (T).Name,
                getData: get,
                addExceptionData:
                    e => e.AddLoggedData("Cluster", SettingsName)
                        .AddLoggedData("Type", typeof (T).Name));
        }
        
        private static MonitorStatus ColorToStatus(string color)
        {
            switch (color)
            {
                case "green":
                    return MonitorStatus.Good;
                case "yellow":
                    return MonitorStatus.Warning;
                case "red":
                    return MonitorStatus.Critical;
                default:
                    return MonitorStatus.Unknown;
            }
        }

        public override string ToString() => SettingsName;
    }
}