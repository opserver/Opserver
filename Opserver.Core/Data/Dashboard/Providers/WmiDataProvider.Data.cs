using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    partial class WmiDataProvider
    {
        /// <summary>
        /// Contains node's data and stores utilizastion history
        /// (as soon as WMI provider doens't has orion storage...)
        /// </summary>
        private partial class WmiNode : Node
        {
            private readonly Dictionary<string, List<Interface.InterfaceUtilization>> _netUtilization;
            
            internal readonly List<MemoryUtilization> MemoryHistory;

            internal readonly List<CPUUtilization> CPUHistory;

            /// <summary>
            /// Name as specified in DashboardSettings.json.
            /// Real name can be different, like for localhost, for example.
            /// </summary>
            internal string OriginalName { get; set; }

            internal List<Cache> Caches { get; private set; }
            
            internal WMISettings Config { get; set; }

            public WmiNode()
            {
                Caches = new List<Cache>(2);
                _netUtilization = new Dictionary<string, List<Interface.InterfaceUtilization>>(2);
                Interfaces = new List<Interface>(2);
                // TODO: Size for retention / interval and convert to limited list
                MemoryHistory = new List<MemoryUtilization>(1024);
                CPUHistory = new List<CPUUtilization>(1024);
                Volumes = new List<Volume>(3);
                VMs = new List<Node>();
                Apps = new List<Application>();
                IPs = new List<IPAddress>();
            }

            public void AddNetworkUtilization(Interface iface, Interface.InterfaceUtilization item)
            {
                if (iface == null)
                    return;

                var ownIface = Interfaces.FirstOrDefault(x => x == iface);
                if (ownIface == null)
                    return;

                List<Interface.InterfaceUtilization> data;
                if (!_netUtilization.ContainsKey(ownIface.Name))
                {
                    data = new List<Interface.InterfaceUtilization>(1024);
                    _netUtilization[ownIface.Name] = data;
                }
                else
                {
                    data = _netUtilization[ownIface.Name];
                }

                if (!item.InAvgBps.HasValue)
                {
                    item.InAvgBps = item.InMaxBps;
                }
                if (!item.OutAvgBps.HasValue)
                {
                    item.OutAvgBps = item.OutMaxBps;
                }

                if (iface.Speed.HasValue && iface.Speed.Value > 0)
                {
                    item.MaxLoad = (short)(100*(item.InMaxBps + item.OutMaxBps)/iface.Speed);
                    item.AvgLoad = (short)(100*(item.InAvgBps + item.OutAvgBps)/iface.Speed);
                }
                
                UpdateHistoryStorage(data, item, x => x.DateTime);
            }


            internal List<Interface.InterfaceUtilization> GetInterfaceUtilizationHistory(Interface @interface)
            {
                List<Interface.InterfaceUtilization> result;
                if (@interface != null 
                    && Interfaces.FirstOrDefault(x => x == @interface) != null
                    && _netUtilization.ContainsKey(@interface.Name))
                {
                    result = _netUtilization[@interface.Name];
                }
                else
                {
                    result = new List<Interface.InterfaceUtilization>(0);
                }
                return result;
            }

            public void AddCpuUtilization(CPUUtilization cpuUtilization)
            {
                UpdateHistoryStorage(CPUHistory, cpuUtilization, x => x.DateTime);
            }

            public void AddMemoryUtilization(MemoryUtilization utilization)
            {
                UpdateHistoryStorage(MemoryHistory, utilization, x => x.DateTime);
            }

            private void UpdateHistoryStorage<T>(List<T> data, T newItem, Func<T, DateTime> getDate)
            {
                lock (data)
                {
                    data.Add(newItem);

                    if (data.Count%100 != 0)
                        return;

                    var limit = DateTime.UtcNow.AddHours(-Config.HistoryHours);
                    if (getDate(data[0]) >= limit)
                        return;
 
                    var index = data.FindIndex(x => getDate(x) >= limit);
                    if (index <= 0)
                        return;

                    data.RemoveRange(0, index + 1);
                }
            }
        }

        public override Task<List<Node.CPUUtilization>> GetCPUUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var wNode = _wmiNodes.FirstOrDefault(x => x.Id == node.Id);
            if (wNode == null)
                return Task.FromResult(new List<Node.CPUUtilization>());

            var startVal = start.HasValue ? new Node.CPUUtilization {DateTime = start.Value} : null;
            var endVal = end.HasValue ? new Node.CPUUtilization { DateTime = end.Value } : null;

            return FilterHistory(wNode.CPUHistory, startVal, endVal, new CpuUtilComparer());
        }

        class CpuUtilComparer : IComparer<Node.CPUUtilization>
        {
            public int Compare(Node.CPUUtilization x, Node.CPUUtilization y)
            {
                return x.DateTime.CompareTo(y.DateTime);
            }
        }

        class MemoryUtilComparer : IComparer<Node.MemoryUtilization>
        {
            public int Compare(Node.MemoryUtilization x, Node.MemoryUtilization y)
            {
                return x.DateTime.CompareTo(y.DateTime);
            }
        }

        class InterfaceUtilComparer : IComparer<Interface.InterfaceUtilization>
        {
            public int Compare(Interface.InterfaceUtilization x, Interface.InterfaceUtilization y)
            {
                return x.DateTime.CompareTo(y.DateTime);
            }
        }

        private static Task<List<T>> FilterHistory<T>(List<T> list, T start, T end, IComparer<T> comparer = null) where T: class
        {
            lock (list)
            {
                var startIndex = 0;
                var endIndex = list.Count;

                if (start != null)
                {
                    var index = list.BinarySearch(start, comparer);
                    if (index < 0)
                    {
                        index = ~index;
                    }
                    startIndex = Math.Min(index, endIndex);
                }
                if (end != null)
                {
                    var index = list.BinarySearch(end, comparer);
                    if (index < 0)
                    {
                        index = ~index;
                    }
                    endIndex = Math.Min(index+1, endIndex);
                }
                var sliceLength = endIndex - startIndex;
                return Task.FromResult(sliceLength <= 0 ? new List<T>() : list.Skip(startIndex).Take(sliceLength).ToList());
            }
        }

        public override Task<List<Node.MemoryUtilization>> GetMemoryUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var wNode = _wmiNodes.FirstOrDefault(x => x.Id == node.Id);
            if (wNode == null)
                return Task.FromResult(new List<Node.MemoryUtilization>());

            Node.MemoryUtilization startVal = null;
            if (start.HasValue)
            {
                startVal = new Node.MemoryUtilization { DateTime = start.Value };
            }
            Node.MemoryUtilization endVal = null;
            if (end.HasValue)
            {
                endVal = new Node.MemoryUtilization { DateTime = end.Value };
            }
            return FilterHistory(wNode.MemoryHistory, startVal, endVal, new MemoryUtilComparer());
        }

        // TODO: Needs implementation
        public override Task<List<Volume.VolumeUtilization>> GetUtilization(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Task.FromResult(new List<Volume.VolumeUtilization>());
        }

        public override Task<List<Interface.InterfaceUtilization>> GetUtilization(Interface @interface, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var node = _wmiNodes.FirstOrDefault(x => x.Id == @interface.NodeId);
            if (node == null)
            {
                return Task.FromResult(new List<Interface.InterfaceUtilization>());
            }

            var history = node.GetInterfaceUtilizationHistory(@interface);

            Interface.InterfaceUtilization startVal = null;
            if (start.HasValue)
            {
                startVal = new Interface.InterfaceUtilization { DateTime = start.Value };
            }
            Interface.InterfaceUtilization endVal = null;
            if (end.HasValue)
            {
                endVal = new Interface.InterfaceUtilization { DateTime = end.Value };
            }
            return FilterHistory(history, startVal, endVal, new InterfaceUtilComparer());
        }
    }
}
