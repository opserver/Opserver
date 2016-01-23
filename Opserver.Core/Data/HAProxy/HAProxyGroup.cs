using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data.HAProxy
{
    public partial class HAProxyGroup : IMonitedService, ISearchableNode
    {
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
            Instances = new List<HAProxyInstance>
            {
                new HAProxyInstance(instance)
                {
                    Group = this
                }
            };
            Instances.ForEach(i => i.TryAddToGlobalPollers());
        }

        public override string ToString()
        {
            return string.Concat(Name, " - ", Instances != null ? Instances.Count.ToString() + " instances" : "");
        }
        
        /// <summary>
        /// Gets the HAProxy instance with the given name, null if it doesn't exist
        /// </summary>
        public static HAProxyGroup GetGroup(string name)
        {
            return AllGroups.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.InvariantCultureIgnoreCase));
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
