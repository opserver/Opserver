using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.HAProxy
{
    public partial class HAProxyGroup
    {
        public static List<HAProxyGroup> AllGroups { get; } =
            Current.Settings.HAProxy.Enabled
                ? Current.Settings.HAProxy.Groups.Select(g => new HAProxyGroup(g))
                    .Union(Current.Settings.HAProxy.Instances.Select(c => new HAProxyGroup(c)))
                    .ToList()
                : new List<HAProxyGroup>();

        public static bool IsHAProxyServer(string node)
        {
            //TODO: Get/Store Host IPs from config, compare to instance passed in
            // Or based on data provider metrics, e.g. in Bosun with identifiers, hmmmm
            return false;
        }
    }
}
