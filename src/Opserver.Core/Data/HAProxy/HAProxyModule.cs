using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data.HAProxy
{
    public class HAProxyModule : StatusModule<HAProxySettings>
    {
        public override string Name => "HAProxy"; 
        public override bool Enabled => Groups.Count > 0;
        public List<HAProxyGroup> Groups { get; }
        public HAProxyAdmin Admin { get; }

        public HAProxyModule(IOptions<HAProxySettings> settings) : base(settings)
        {
            var snapshot = settings.Value;
            Groups = snapshot.Groups.Select(g => new HAProxyGroup(this, g))
                .Union(snapshot.Instances.Select(c => new HAProxyGroup(this, c)))
                .ToList();
            Admin = new HAProxyAdmin(this);
        }
        public override MonitorStatus MonitorStatus => Groups.GetWorstStatus();
        public override bool IsMember(string node)
        {
            //TODO: Get/Store Host IPs from config, compare to instance passed in
            // Or based on data provider metrics, e.g. in Bosun with identifiers, hmmmm
            return false;
        }

        /// <summary>
        /// Gets the HAProxy instance with the given name, null if it doesn't exist
        /// </summary>
        /// <param name="name">The name of the <see cref="HAProxyGroup"/> to fetch.</param>
        public HAProxyGroup GetGroup(string name)
        {
            return Groups.Find(e => string.Equals(e.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Gets the list of proxies from HAProxy
        /// </summary>
        public List<Proxy> GetAllProxies()
        {
            if (!Enabled) return new List<Proxy>();
            var instances = Groups.SelectMany(g => g.Instances).ToList();
            return GetProxies(instances);
        }

        internal List<Proxy> GetProxies(List<HAProxyInstance> instances)
        {
            using (MiniProfiler.Current.Step("HAProxy - GetProxies()"))
            {
                return instances.AsParallel().SelectMany(i => i.Proxies.Data ?? Enumerable.Empty<Proxy>())
                    .Where(p => p != null)
                    .OrderBy(p => instances.IndexOf(p.Instance))
                    .ToList();
            }
        }
    }
}
