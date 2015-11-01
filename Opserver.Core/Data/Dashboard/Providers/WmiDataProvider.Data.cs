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

            internal List<Cache> Caches { get; }
            
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

            private void AddNetworkUtilization(Interface iface, Interface.InterfaceUtilization item)
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
                
                UpdateHistoryStorage(data, item, x => x.DateEpoch);
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

            private void AddCpuUtilization(CPUUtilization cpuUtilization)
            {
                UpdateHistoryStorage(CPUHistory, cpuUtilization, x => x.DateEpoch);
            }

            private void AddMemoryUtilization(MemoryUtilization utilization)
            {
                UpdateHistoryStorage(MemoryHistory, utilization, x => x.DateEpoch);
            }

            private void UpdateHistoryStorage<T>(List<T> data, T newItem, Func<T, long> getDateEpoch) where T : GraphPoint
            {
                lock (data)
                {
                    data.Add(newItem);

                    if (data.Count%100 != 0)
                        return;

                    var limit = DateTime.UtcNow.AddHours(-Config.HistoryHours).ToEpochTime();
                    if (getDateEpoch(data[0]) >= limit)
                        return;
                    var index = data.FindIndex(x => getDateEpoch(x) >= limit);
                    if (index <= 0)
                        return;

                    data.RemoveRange(0, index + 1);
                }
            }
        }

        public override Task<List<GraphPoint>> GetCPUUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var wNode = _wmiNodes.FirstOrDefault(x => x.Id == node.Id);
            return Task.FromResult(FilterHistory<Node.CPUUtilization, GraphPoint>(wNode?.CPUHistory, start, end).ToList<GraphPoint>());
        }

        public override Task<List<GraphPoint>> GetMemoryUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var wNode = _wmiNodes.FirstOrDefault(x => x.Id == node.Id);
            return Task.FromResult(FilterHistory<Node.MemoryUtilization, GraphPoint>(wNode?.MemoryHistory, start, end).ToList<GraphPoint>());
        }

        // TODO: Needs implementation
        public override Task<List<GraphPoint>> GetUtilization(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Task.FromResult(new List<GraphPoint>());
        }

        public override Task<List<DoubleGraphPoint>> GetUtilization(Interface @interface, DateTime? start, DateTime? end, int? pointCount = null)
        {
            var node = _wmiNodes.FirstOrDefault(x => x.Id == @interface.NodeId);
            return Task.FromResult(FilterHistory<Interface.InterfaceUtilization, DoubleGraphPoint>(node?.GetInterfaceUtilizationHistory(@interface), start, end).ToList());
        }

        private static IEnumerable<TResult> FilterHistory<T, TResult>(List<T> list, DateTime? start, DateTime? end) where T : IGraphPoint, TResult, new()
        {
            if (list == null)
                return Enumerable.Empty<TResult>();

            lock (list)
            {
                int startIndex = 0, endIndex = list.Count;
                var comparer = new GraphPointComparer<T>();

                if (start != null)
                {
                    var index = list.BinarySearch(new T { DateEpoch = start.Value.ToEpochTime() }, comparer);
                    if (index < 0) index = ~index;
                    startIndex = Math.Min(index, endIndex);
                }
                if (end != null)
                {
                    var index = list.BinarySearch(new T { DateEpoch = end.Value.ToEpochTime() }, comparer);
                    if (index < 0) index = ~index;
                    endIndex = Math.Min(index + 1, endIndex);
                }
                var sliceLength = endIndex - startIndex;
                return sliceLength <= 0 ? Enumerable.Empty<TResult>() : list.Skip(startIndex).Take(sliceLength).Cast<TResult>();
            }
        }

        class GraphPointComparer<T> : IComparer<T> where T : IGraphPoint
        {
            public int Compare(T x, T y) => x.DateEpoch.CompareTo(y.DateEpoch);
        }
    }
}
