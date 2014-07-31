using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using StackExchange.Elastic;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<ClusterNodeInfo> _nodes;
        public Cache<ClusterNodeInfo> Nodes
        {
            get { return _nodes ?? (_nodes = GetCache<ClusterNodeInfo>(20)); }
        }
        
        public string Name { get { return Nodes.Data != null ? Nodes.Data.Name : "Unknown"; } }

        public class ClusterNodeInfo : ElasticDataObject
        {
            public string Name { get; internal set; }
            public List<NodeInfoWrap> Nodes { get; internal set; }

            public override ElasticResponse RefreshFromConnection(SearchClient cli)
            {
                var infos = cli.GetClusterNodeInfo().Data;
                Name = infos.ClusterName;
                if (infos.Nodes != null)
                {
                    Nodes = infos.Nodes.Select(node => new NodeInfoWrap
                        {
                            GUID = node.Key,
                            Name = node.Value.Name,
                            Hostname = node.Value.Hostname,
                            VersionString = node.Value.Version,
                            Info = node.Value,
                        }).OrderBy(node => node.Name).ToList();
                }

                var rawStats = cli.GetClusterNodeStats();
                var stats = rawStats.Data;
                if (stats != null)
                {
                    Name = stats.ClusterName;
                    if (stats.Nodes != null)
                    {
                        foreach (var ns in stats.Nodes)
                        {
                            var ni = Nodes.FirstOrDefault(n => n.GUID == ns.Key);
                            if (ni != null)
                            {
                                ni.Stats = ns.Value;
                            }
                            else
                            {
                                Nodes.Add(new NodeInfoWrap
                                {
                                    GUID = ns.Key,
                                    Name = ns.Value.Name,
                                    Hostname = ns.Value.Hostname,
                                    Stats = ns.Value
                                });
                            }
                        }
                        Nodes = Nodes.OrderBy(n => n.Name).ToList();
                    }
                }
                return rawStats;
            }

            public NodeInfoWrap Get(string nameOrGuid)
            {
                if (Nodes == null) return null;
                return
                    Nodes.FirstOrDefault(
                        n => string.Equals(n.Name, nameOrGuid, StringComparison.InvariantCultureIgnoreCase)
                             || string.Equals(n.GUID, nameOrGuid, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public class NodeInfoWrap : IMonitorStatus
        {
            public string GUID { get; internal set; }
            public string Name { get; internal set; }
            public string Hostname { get; internal set; }
            public string VersionString { get; internal set; }
            public NodeInfo Info { get; internal set; }
            public NodeStats Stats { get; internal set; }
            public bool IsMaster { get; internal set; }
            // TODO: adjust monitor status/reason for nodes specified in config vs. what the cluster knows of
            public MonitorStatus MonitorStatus { get { return MonitorStatus.Good; } }
            public string MonitorStatusReason { get { return null; } }

            private Version _version;
            [JsonIgnore]
            public Version Version { get { return _version ?? (_version = VersionString.HasValue() ? Version.Parse(VersionString) : new Version("0.0")); } }
        }
    }
}
