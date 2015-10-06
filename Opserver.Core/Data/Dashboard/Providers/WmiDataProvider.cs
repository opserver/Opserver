using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    partial class WmiDataProvider : DashboardDataProvider<WMISettings>
    {
        private readonly WMISettings _config;
        private readonly List<WmiNode> _wmiNodes;

        public WmiDataProvider(WMISettings settings) : base(settings)
        {
            _config = settings;
            _wmiNodes = InitNodeList(_config.Nodes).OrderBy(x => x.OriginalName).ToList();
        }

        /// <summary>
        /// Make list of nodes as per configuration. 
        /// When adding, a node's ip address is resolved via Dns.
        /// </summary>
        private IEnumerable<WmiNode> InitNodeList(IList<string> names)
        {
            var nodesList = new List<WmiNode>(names.Count);
            foreach (var nodeName in names)
            {
                var node = new WmiNode
                {
                    Config = _config,
                    Id = nodeName.ToLower(),
                    Name = nodeName.ToLower(),
                    DataProvider = this,
                    MachineType = "Windows"
                };

                try
                {
                    var hostEntry = Dns.GetHostEntry(node.Name);
                    if (hostEntry.AddressList.Any())
                    {
                        node.Ip = hostEntry.AddressList[0].ToString();
                        node.Status = NodeStatus.Active;
                    }
                    else
                    {
                        node.Status = NodeStatus.Unreachable;
                    }
                }
                catch (Exception)
                {
                    node.Status = NodeStatus.Unreachable;
                }

                var staticDataCache = ProviderCache(
                    () => node.PollNodeInfo(), 
                    _config.StaticDataTimeoutSeconds,
                    memberName: node.Name + "-Static");
                node.Caches.Add(staticDataCache);

                node.Caches.Add(ProviderCache(
                    () => node.PollStats(), 
                    _config.DynamicDataTimeoutSeconds,
                    memberName: node.Name + "-Dynamic"));

                //Force update static host data, incuding os info, volumes, interfaces.
                staticDataCache.Poll(true);

                nodesList.Add(node);
            }
            return nodesList;
        }

        public override int MinSecondsBetweenPolls => 10;

        public override string NodeType => "WMI";

        public override IEnumerable<Cache> DataPollers => _wmiNodes.SelectMany(x => x.Caches);

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            yield break;
        }

        protected override string GetMonitorStatusReason() => null;

        public override bool HasData => DataPollers.Any(x => x.HasData());

        public override List<Node> AllNodes => _wmiNodes.Cast<Node>().ToList();
    }
}
