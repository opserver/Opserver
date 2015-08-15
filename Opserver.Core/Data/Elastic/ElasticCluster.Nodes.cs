using System;
using System.Collections.Generic;
using System.Linq;
using Jil;
using StackExchange.Elastic;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<ClusterNodeInfo> _nodes;
        public Cache<ClusterNodeInfo> Nodes => _nodes ?? (_nodes = GetCache<ClusterNodeInfo>(20));

        public string Name => Nodes.Data != null ? Nodes.Data.Name : "Unknown";

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
                            BuildString = node.Value.Build,
                            Attributes = node.Value.Attributes,
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
                return
                    Nodes?.FirstOrDefault(
                        n => string.Equals(n.Name, nameOrGuid, StringComparison.InvariantCultureIgnoreCase)
                             || string.Equals(n.GUID, nameOrGuid, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        // TODO: Remove this wrapper now that we control the lib
        public class NodeInfoWrap : IMonitorStatus
        {
            public string GUID { get; internal set; }
            public string Name { get; internal set; }
            public string Hostname { get; internal set; }
            public string VersionString { get; internal set; }
            public string BuildString { get; internal set; }
            public Dictionary<string, string> Attributes { get; internal set; } 
            public NodeInfo Info { get; internal set; }
            public NodeStats Stats { get; internal set; }
            public bool IsMaster { get; internal set; }
            // TODO: adjust monitor status/reason for nodes specified in config vs. what the cluster knows of
            public MonitorStatus MonitorStatus => MonitorStatus.Good;
            public string MonitorStatusReason => null;

            private Version _version;
            [JilDirective(Ignore = true)]
            public Version Version => _version ?? (_version = VersionString.HasValue() ? Version.Parse(VersionString) : new Version("0.0"));

            public bool IsClient => GetAttribute("client") == "true";
            public bool IsDataNode => GetAttribute("data") == "true";

            private string GetAttribute(string key)
            {
                string val;
                return Attributes != null && Attributes.TryGetValue(key, out val) ? val : null;
            }
        }
    }
}
