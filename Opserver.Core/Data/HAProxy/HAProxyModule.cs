using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.HAProxy
{
    public class HAProxyModule : StatusModule
    {
        public static bool Enabled => Groups.Count > 0;
        public static List<HAProxyGroup> Groups { get; }
        
        static HAProxyModule()
        {
            Groups = Current.Settings.HAProxy.Groups.Select(g => new HAProxyGroup(g))
                .Union(Current.Settings.HAProxy.Instances.Select(c => new HAProxyGroup(c)))
                .ToList();
        }

        public override bool IsMember(string node)
        {
            //TODO: Get/Store Host IPs from config, compare to instance passed in
            // Or based on data provider metrics, e.g. in Bosun with identifiers, hmmmm
            return false;
        }
    }
}
