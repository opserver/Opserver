using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    partial class WmiDataProvider : DashboardDataProvider
    {
        private readonly WmiProviderSettings _config;
        private List<WmiNode> _wmiNodes;

        public WmiDataProvider(DashboardSettings.Provider provider)
            : base(provider)
        {
            _config = Current.Settings.GetSettings<WmiProviderSettings>();
            _wmiNodes = InitNodeList(_config.Nodes).OrderBy(x => x.OriginalName).ToList();
            _config.Changed += OnConfigChanged;
        }

        private void OnConfigChanged(object sender, EventArgs eventArgs)
        {
            var added = new List<string>();
            var notChanged = new List<WmiNode>();

            //Determine added, removed and not changes nodes.
            FindChanges(_wmiNodes, _config.Nodes.OrderBy(x => x).ToList(), added, notChanged);

            //Init new list.
            var addedNodes = InitNodeList(added, _wmiNodes.Max(x => x.Id) + 1);
            var newNodeList = notChanged.Concat(addedNodes).OrderBy(x => x.OriginalName).ToList();
            _wmiNodes = newNodeList;
        }

        /// <summary>
        /// Find config changes. Lists should be sorded ascending.
        /// </summary>
        /// <param name="existingWmiNodes">Current wmi nodes list.</param>
        /// <param name="configNodeNames">Config node names to be checked for new/removed nodes.</param>
        /// <param name="added">New config names go here.</param>
        /// <param name="notChanged">WmiNode list items which should remain.</param>
        private static void FindChanges(
            IReadOnlyList<WmiNode> existingWmiNodes,
            IReadOnlyList<string> configNodeNames,
            ICollection<string> added,
            ICollection<WmiNode> notChanged)
        {
            var existingIndex = 0;
            var configIndex = 0;

            while (existingIndex < existingWmiNodes.Count && configIndex < configNodeNames.Count)
            {
                var existingWmiNode = existingWmiNodes[existingIndex];
                var configNodeName = configNodeNames[configIndex];
                var eq = String.Compare(existingWmiNode.OriginalName, configNodeName, StringComparison.OrdinalIgnoreCase);
                if (eq < 0)
                {
                    //left item less than right. which means that it has to be removed.
                    existingIndex++;
                }
                else if (eq > 0)
                {
                    //right item is less, add it.
                    added.Add(configNodeName);
                    configIndex++;
                }
                else //equal
                {
                    //No changes, increase both indexes;
                    notChanged.Add(existingWmiNode);
                    existingIndex++;
                    configIndex++;
                }
            }
            if (existingIndex >= existingWmiNodes.Count) //Reached end of left array. 
            {
                //Now all items from right array are to be added
                while (configIndex < configNodeNames.Count)
                {
                    added.Add(configNodeNames[configIndex++]);
                }
            }
            //No need to track removed explicityly here.
        }

        /// <summary>
        /// Make list of nodes as per configuration. 
        /// When adding, a node's ip address is resolved via Dns.
        /// </summary>
        private IEnumerable<WmiNode> InitNodeList(IList<string> names, int startId = 1)
        {
            var nodesList = new List<WmiNode>(names.Count());
            var id = startId;
            foreach (var nodeName in names)
            {
                var node = new Node
                {
                    Id = id++,
                    Name = nodeName.ToLower(),
                    DataProvider = this,
                    MachineType = "Windows",
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

                var wmiNode = new WmiNode(node)
                {
                    OriginalName = nodeName,
                    Config = _config
                };

                var staticDataCache = ProviderCache(
                    () => GetStaticData(wmiNode), 
                    _config.StaticDataTimeoutSec,
                    memberName: wmiNode.Node.Name + "-Static");
                wmiNode.Caches.Add(staticDataCache);
                wmiNode.Caches.Add(ProviderCache(
                    () => GetDynamicData(wmiNode), 
                    _config.DynamicDataTimeoutSec,
                    memberName: wmiNode.Node.Name + "-Dynamic"));

                //Force update static host data, incuding os info, volumes, interfaces.
                staticDataCache.Poll(true);

                nodesList.Add(wmiNode);
            }
            return nodesList;
        }

        public override int MinSecondsBetweenPolls
        {
            get { return 10; }
        }

        public override string NodeType
        {
            get { return "WMI"; }
        }

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                return _wmiNodes.SelectMany(x => x.Caches);
            }
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            yield break;
        }

        protected override string GetMonitorStatusReason()
        {
            return null;
        }

        public override bool HasData
        {
            get { return DataPollers.Any(x => x.HasData()); }
        }

        public override List<Node> AllNodes
        {
            get { return _wmiNodes.Select(w => w.Node).ToList(); }
        }

        public override List<Interface> AllInterfaces
        {
            get { return _wmiNodes.SelectMany(w => w.Interfaces).ToList(); }
        }

        public override List<Volume> AllVolumes
        {
            get { return _wmiNodes.SelectMany(w => w.Volumes).ToList(); }
        }

        public override List<Application> AllApplications
        {
            get { return new List<Application>(); }
        }

        public override IEnumerable<Node> GetNodesByIP(IPAddress ip)
        {
            yield break;
        }

        public override IEnumerable<IPAddress> GetIPsForNode(Node node)
        {
            yield break;
        }
    }
}
