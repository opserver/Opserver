using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data.HAProxy
{
    public partial class HAProxyGroup : IMonitedService, ISearchableNode
    {
        private static readonly object _insertLock = new object();

        public string DisplayName => Name;
        public string CategoryName => "HAProxy";

        public HAProxySettings.Group Settings { get; }

        public string Name => Settings.Name;
        public string Description => Settings.Description;
        public List<HAProxyInstance> Instances { get; }
        
        public MonitorStatus MonitorStatus => Instances.GetWorstStatus();

        public string MonitorStatusReason => Instances.GetReasonSummary();

        public HAProxyGroup(HAProxySettings.Group group)
        {
            Settings = group;
            Instances = group.Instances.Select(i => new HAProxyInstance(i, group) { Group = this }).ToList();
            Instances.ForEach(i => i.TryAddToGlobalPollers());
        }

        /// <summary>
        /// Creates a single instance group for consistent management at a higher level
        /// </summary>
        public HAProxyGroup(HAProxySettings.Instance instance)
        {
            Settings = new HAProxySettings.Group
                {
                    Name = instance.Name,
                    Description = instance.Description
                };
            Instances = new List<HAProxyInstance> {new HAProxyInstance(instance) {Group = this}};
            Instances.ForEach(i => i.TryAddToGlobalPollers());
        }

        public override string ToString()
        {
            return string.Concat(Name, " - ", Instances != null ? Instances.Count + " instances" : "");
        }
        
        /// <summary>
        /// Gets the HAProxy instance with the given name, null if it doesn't exist
        /// </summary>
        public static HAProxyGroup GetGroup(string name)
        {
            return AllGroups.FirstOrDefault(e => string.Equals(e.Name, Environment.MachineName + ":" + name, StringComparison.InvariantCultureIgnoreCase))
                ?? AllGroups.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Gets the list of proxies from HAProxy
        /// </summary>
        public static List<Proxy> GetAllProxies()
        {
            if (!Current.Settings.HAProxy.Enabled) return new List<Proxy>();
            var instances = AllGroups.SelectMany(g => g.Instances).ToList();
            return GetProxies(instances);
        }


        /// <summary>
        /// Gets the list of proxies for this group
        /// </summary>
        public List<Proxy> GetProxies()
        {
            if (!Current.Settings.HAProxy.Enabled) return new List<Proxy>();
            return GetProxies(Instances);
        }

        private static List<Proxy> GetProxies(List<HAProxyInstance> instances)
        {
            var proxies = new List<Proxy>();
            using (MiniProfiler.Current.Step("HAProxy - GetProxies()"))
                Parallel.ForEach(instances, i =>
                {
                    var result = i.Proxies.SafeData();
                    if (result == null) return;
                    lock (_insertLock)
                    {
                        proxies.AddRange(result);
                    }
                });
            proxies = proxies.OrderBy(p => instances.IndexOf(p.Instance)).ToList();
            return proxies;
        }
    }
}
