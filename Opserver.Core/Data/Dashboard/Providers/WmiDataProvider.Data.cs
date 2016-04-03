using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
            internal readonly List<MemoryUtilization> MemoryHistory;
            internal readonly List<CPUUtilization> CPUHistory;
            internal readonly List<Interface.InterfaceUtilization> CombinedNetHistory;
            private readonly ConcurrentDictionary<string, List<Interface.InterfaceUtilization>> NetHistory;
            private readonly ConcurrentDictionary<string, List<Volume.VolumeUtilization>> VolumeHistory;

            /// <summary>
            /// Name as specified in DashboardSettings.json.
            /// Real name can be different, like for localhost, for example.
            /// </summary>
            internal string Endpoint { get; }

            internal List<Cache> Caches { get; }
            
            internal WMISettings Config { get; set; }

            public WmiNode(string endpoint)
            {
                Endpoint = endpoint;
                Id = endpoint.ToLower();
                Name = endpoint.ToLower();
                MachineType = "Windows";

                Caches = new List<Cache>(2);
                Interfaces = new List<Interface>(2);
                Volumes = new List<Volume>(3);
                VMs = new List<Node>();
                Apps = new List<Application>();

                // TODO: Size for retention / interval and convert to limited list
                MemoryHistory = new List<MemoryUtilization>(1024);
                CPUHistory = new List<CPUUtilization>(1024);
                CombinedNetHistory = new List<Interface.InterfaceUtilization>(1024);
                NetHistory = new ConcurrentDictionary<string, List<Interface.InterfaceUtilization>>();
                VolumeHistory = new ConcurrentDictionary<string, List<Volume.VolumeUtilization>>();
            }

            internal List<Interface.InterfaceUtilization> GetInterfaceUtilizationHistory(Interface iface)
            {
                List<Interface.InterfaceUtilization> result;
                if (iface != null 
                    && Interfaces.FirstOrDefault(x => x == iface) != null
                    && NetHistory.ContainsKey(iface.Name))
                {
                    result = NetHistory[iface.Name];
                }
                else
                {
                    result = new List<Interface.InterfaceUtilization>(0);
                }
                return result;
            }

            private void UpdateHistoryStorage<T>(List<T> data, T newItem) where T : GraphPoint
            {
                lock (data)
                {
                    data.Add(newItem);

                    if (data.Count%100 != 0)
                        return;

                    var limit = DateTime.UtcNow.AddHours(-Config.HistoryHours).ToEpochTime();
                    if (data[0].DateEpoch >= limit)
                        return;
                    var index = data.FindIndex(x => x.DateEpoch >= limit);
                    if (index <= 0)
                        return;

                    data.RemoveRange(0, index + 1);
                }
            }
        }

        public override Task<List<GraphPoint>> GetCPUUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null) =>
            Task.FromResult(FilterHistory<Node.CPUUtilization, GraphPoint>(GetWmiNodeById(node.Id)?.CPUHistory, start, end).ToList());

        public override Task<List<GraphPoint>> GetMemoryUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null) =>
            Task.FromResult(FilterHistory<Node.MemoryUtilization, GraphPoint>(GetWmiNodeById(node.Id)?.MemoryHistory, start, end).ToList());
        
        public override Task<List<DoubleGraphPoint>> GetNetworkUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null) => 
            Task.FromResult(FilterHistory<Interface.InterfaceUtilization, DoubleGraphPoint>(GetWmiNodeById(node.Id)?.CombinedNetHistory, start, end).ToList());

        // TODO: Needs implementation
        public override Task<List<GraphPoint>> GetUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Task.FromResult(new List<GraphPoint>());
        }

        public override Task<List<DoubleGraphPoint>> GetUtilizationAsync(Interface iface, DateTime? start, DateTime? end, int? pointCount = null) =>
            Task.FromResult(FilterHistory<Interface.InterfaceUtilization, DoubleGraphPoint>(GetWmiNodeById(iface.NodeId)?.GetInterfaceUtilizationHistory(iface), start, end).ToList());

        private static IEnumerable<TResult> FilterHistory<T, TResult>(List<T> list, DateTime? start, DateTime? end) where T : GraphPoint, TResult, new()
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
