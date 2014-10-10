using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    partial class WmiDataProvider
    {
        /// <summary>
        /// Contains node's data and stores utilizastion history
        /// (as soon as WMI provider doens't has orion storage...)
        /// </summary>
        private class WmiNode
        {
            private readonly Dictionary<string, List<Interface.InterfaceUtilization>> _netUtilization;

            internal readonly List<Interface> Interfaces;

            internal readonly List<Node.MemoryUtilization> MemoryUtilization;

            internal readonly List<Node.CPUUtilization> CpuUtilization;

            internal readonly List<Volume> Volumes;

            internal int Id { get { return Node.Id; } }

            internal Node Node { get; private set; }

            /// <summary>
            /// Name as specified in WmiProviderSettings.json.
            /// Real name can be different, like for localhost, for example.
            /// </summary>
            internal string OriginalName { get; set; }

            internal List<Cache> Caches { get; private set; }
            
            internal WmiProviderSettings Config { get; set; }

            public WmiNode(Node node)
            {
                Caches = new List<Cache>(2);
                _netUtilization = new Dictionary<string, List<Interface.InterfaceUtilization>>(2);
                Interfaces = new List<Interface>(2);
                MemoryUtilization = new List<Node.MemoryUtilization>(1024);
                CpuUtilization = new List<Node.CPUUtilization>(1024);
                Volumes = new List<Volume>(3);

                Node = node;
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

            public void AddCpuUtilization(Node.CPUUtilization cpuUtilization)
            {
                UpdateHistoryStorage(CpuUtilization, cpuUtilization, x => x.DateTime);
            }

            public void AddMemoryUtilization(Node.MemoryUtilization utilization)
            {
                UpdateHistoryStorage(MemoryUtilization, utilization, x => x.DateTime);
            }

            private void UpdateHistoryStorage<T>(List<T> data, T newItem, Func<T, DateTime> getDate)
            {
                lock (data)
                {
                    data.Add(newItem);

                    if (data.Count%100 != 0)
                        return;

                    var limit = DateTime.UtcNow.AddHours(-Config.HoursToKeepHistory);
                    if (getDate(data[0]) >= limit)
                        return;
 
                    var index = data.FindIndex(x => getDate(x) >= limit);
                    if (index <= 0)
                        return;

                    data.RemoveRange(0, index + 1);
                }
            }
        }

        public override IEnumerable<Node.CPUUtilization> GetCPUUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var wNode = _wmiNodes.FirstOrDefault(x => x.Id == node.Id);
            if (wNode == null)
                return Enumerable.Empty<Node.CPUUtilization>();

            Node.CPUUtilization startVal = null;
            if (start.HasValue)
            {
                startVal = new Node.CPUUtilization { DateTime = start.Value };
            }
            Node.CPUUtilization endVal = null;
            if (end.HasValue)
            {
                endVal = new Node.CPUUtilization { DateTime = end.Value };
            }
            return FilterHistory(wNode.CpuUtilization, startVal, endVal, new CpuUtilComparer());
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

        private static IEnumerable<T> FilterHistory<T>(List<T> list, T start, T end, IComparer<T> comparer = null)
            where T: class
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
                return sliceLength <= 0 
                    ? Enumerable.Empty<T>() 
                    : list.Skip(startIndex).Take(sliceLength).ToList();
            }
        }

        public override IEnumerable<Node.MemoryUtilization> GetMemoryUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var wNode = _wmiNodes.FirstOrDefault(x => x.Id == node.Id);
            if (wNode == null)
                return Enumerable.Empty<Node.MemoryUtilization>();

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
            return FilterHistory(wNode.MemoryUtilization, startVal, endVal, new MemoryUtilComparer());
        }

        public override IEnumerable<Volume.VolumeUtilization> GetUtilization(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            yield break;
        }

        public override IEnumerable<Interface.InterfaceUtilization> GetUtilization(Interface @interface, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var node = _wmiNodes.FirstOrDefault(x => x.Id == @interface.NodeId);
            if (node == null)
            {
                return Enumerable.Empty<Interface.InterfaceUtilization>();
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
