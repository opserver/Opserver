using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.HAProxy
{
    public partial class HAProxyGroup
    {
        public static List<HAProxyGroup> AllGroups
        {
            get { return _allGroups ?? (_allGroups = LoadHAProxyGroups()); }
        }

        private static readonly object _loadLock = new object();
        private static List<HAProxyGroup> _allGroups;
        private static List<HAProxyGroup> LoadHAProxyGroups()
        {
            lock (_loadLock)
            {
                if (_allGroups != null) return _allGroups;

                if (!Current.Settings.HAProxy.Enabled)
                    return new List<HAProxyGroup>();

                var groups = Current.Settings.HAProxy.Groups.Select(g => new HAProxyGroup(g));
                var instances = Current.Settings.HAProxy.Instances.Select(c => new HAProxyGroup(c));
                return groups.Union(instances).ToList();
            }
        }

        public static bool IsHAProxyServer(string node)
        {
            //TODO: Get/Store Host IPs from config, compare to instance passed in
            return false;
        }
    }
}
