using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace Opserver.Data.Dashboard.Providers
{
    internal partial class WmiDataProvider
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
            internal readonly List<Volume.VolumePerformanceUtilization> CombinedVolumePerformanceHistory;
            private readonly ConcurrentDictionary<string, List<CPUUtilization>> ProcessCPUHistory;
            private readonly ConcurrentDictionary<string, List<MemoryUtilization>> ProcessMemoryHistory;
            private readonly ConcurrentDictionary<string, List<Interface.InterfaceUtilization>> NetHistory;
            private readonly ConcurrentDictionary<string, List<Volume.VolumeUtilization>> VolumeHistory;
            private readonly ConcurrentDictionary<string, List<Volume.VolumePerformanceUtilization>> VolumePerformanceHistory;

            internal readonly ConcurrentDictionary<string, PerfRawData> previousPerfDataCache = new ConcurrentDictionary<string, PerfRawData>();

            /// <summary>
            /// Defines if we can use "Win32_PerfFormattedData_Tcpip_NetworkAdapter" to query adapter utilization or not.
            /// This is needed because "Win32_PerfFormattedData_Tcpip_NetworkAdapter" was first introduced in Windows 8 and Windows 2012.
            /// </summary>
            private bool _canQueryAdapterUtilization;
            private bool _canQueryTeamingInformation;
            private bool _nodeInfoAvailable;

            public uint NumberOfLogicalProcessors { get; private set; }

            /// <summary>
            /// Name as specified in DashboardSettings.json.
            /// Real name can be different, like for localhost, for example.
            /// </summary>
            internal string Endpoint { get; }

            internal List<Cache> Caches { get; }

            private WmiDataProvider WmiProvider { get; }
            private WMISettings Config => WmiProvider.Settings;

            public WmiNode(WmiDataProvider provider, string endpoint)
            {
                WmiProvider = provider;
                DataProvider = provider;
                Endpoint = endpoint;
                Id = endpoint.ToLower();
                Name = endpoint.ToLower();
                MachineType = "Windows";

                Caches = new List<Cache>(2);
                Interfaces = new List<Interface>(2);
                Volumes = new List<Volume>(3);
                Services = new List<NodeService>();
                VMs = new List<Node>();
                Apps = new List<Application>();

                // TODO: Size for retention / interval and convert to limited list
                MemoryHistory = new List<MemoryUtilization>(1024);
                CPUHistory = new List<CPUUtilization>(1024);
                ProcessCPUHistory = new ConcurrentDictionary<string, List<CPUUtilization>>();
                ProcessMemoryHistory = new ConcurrentDictionary<string, List<MemoryUtilization>>();
                CombinedNetHistory = new List<Interface.InterfaceUtilization>(1024);
                NetHistory = new ConcurrentDictionary<string, List<Interface.InterfaceUtilization>>();
                VolumeHistory = new ConcurrentDictionary<string, List<Volume.VolumeUtilization>>();
                CombinedVolumePerformanceHistory = new List<Volume.VolumePerformanceUtilization>(1024);
                VolumePerformanceHistory = new ConcurrentDictionary<string, List<Volume.VolumePerformanceUtilization>>();
            }

            internal List<Interface.InterfaceUtilization> GetInterfaceUtilizationHistory(Interface iface)
            {
                if (iface != null
                    && Interfaces.Find(x => x == iface) != null
                    && NetHistory.ContainsKey(iface.Name))
                {
                    return NetHistory[iface.Name];
                }
                else
                {
                    return new List<Interface.InterfaceUtilization>(0);
                }
            }

            internal List<Volume.VolumePerformanceUtilization> GetVolumePerformanceUtilizationHistory(Volume iface)
            {
                if (iface != null
                    && Volumes.Find(x => x == iface) != null
                    && VolumePerformanceHistory.ContainsKey(iface.Name))
                {
                    return VolumePerformanceHistory[iface.Name];
                }
                else
                {
                    return new List<Volume.VolumePerformanceUtilization>(0);
                }
            }

            internal List<CPUUtilization> GetCPUProcessUtilizationHistory(string processName)
            {
                if(processName != null
                    && ProcessCPUHistory.ContainsKey(processName))
                {
                    return ProcessCPUHistory[processName];
                }
                else
                {
                    return new List<CPUUtilization>(0);
                }
            }

            internal List<MemoryUtilization> GetProcessMemoryUtilizationHistory(string processName)
            {
                if (processName != null
                    && ProcessMemoryHistory.ContainsKey(processName))
                {
                    return ProcessMemoryHistory[processName];
                }
                else
                {
                    return new List<MemoryUtilization>(0);
                }
            }

            private void UpdateHistoryStorage<T>(List<T> data, T newItem) where T : GraphPoint
            {
                lock (data)
                {
                    data.Add(newItem);

                    if (data.Count % 100 != 0)
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

        public override Task<List<DoubleGraphPoint>> GetVolumePerformanceUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null) =>
            Task.FromResult(FilterHistory<Volume.VolumePerformanceUtilization, DoubleGraphPoint>(GetWmiNodeById(node.Id)?.CombinedVolumePerformanceHistory, start, end).ToList());

        // TODO: Needs implementation
        public override Task<List<GraphPoint>> GetUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            return Task.FromResult(new List<GraphPoint>());
        }

        public Task<ServiceActionResult> UpdateServiceAsync(Node node, string serviceName, NodeService.Action action) =>
            GetWmiNodeById(node.Id).UpdateServiceAsync(serviceName, action);

        public override Task<List<DoubleGraphPoint>> GetPerformanceUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null) =>
            Task.FromResult(FilterHistory<Volume.VolumePerformanceUtilization, DoubleGraphPoint>(GetWmiNodeById(volume.NodeId)?.GetVolumePerformanceUtilizationHistory(volume), start, end).ToList());

        public override Task<List<DoubleGraphPoint>> GetUtilizationAsync(Interface iface, DateTime? start, DateTime? end, int? pointCount = null) =>
            Task.FromResult(FilterHistory<Interface.InterfaceUtilization, DoubleGraphPoint>(GetWmiNodeById(iface.NodeId)?.GetInterfaceUtilizationHistory(iface), start, end).ToList());

        public Task<List<GraphPoint>> GetCPUProcessUtilizationAsync(string processName, Node node, DateTime? start, DateTime? end, int? pointCount = null) =>
            Task.FromResult(FilterHistory<Node.CPUUtilization, GraphPoint>(GetWmiNodeById(node.Id)?.GetCPUProcessUtilizationHistory(processName), start, end).ToList());

        public Task<List<GraphPoint>> GetProcessMemoryUtilizationAsync(string processName, Node node, DateTime? start, DateTime? end, int? pointCount = null) =>
            Task.FromResult(FilterHistory<Node.MemoryUtilization, GraphPoint>(GetWmiNodeById(node.Id)?.GetProcessMemoryUtilizationHistory(processName), start, end).ToList());

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

        private class GraphPointComparer<T> : IComparer<T> where T : IGraphPoint
        {
            public int Compare(T x, T y) => x.DateEpoch.CompareTo(y.DateEpoch);
        }

        private class PerfRawData
        {
            private readonly ManagementObject _data;
            private PerfRawData _previousData;

            public string Identifier { get; }
            private string Classname { get; }
            private ulong Timestamp { get; }

            public PerfRawData(WmiNode node, dynamic data) : this(node, (ManagementObject)data) { }

            public PerfRawData(WmiNode node, ManagementObject data)
            {
                _data = data;

                Classname = data.ClassPath.ClassName;
                Identifier = (string)_data["Name"];
                Timestamp = Convert.ToUInt64(_data["Timestamp_Sys100NS"]);

                string cacheKey = $"{Classname}.{Identifier}";

                // "previous.previousData = null" to cleanup previousData from previousData. Otherwise we get a linked list which never cleans up.
                node.previousPerfDataCache.AddOrUpdate(cacheKey, _ => _previousData = this, (_, previous) =>
                {
                    previous._previousData = null;
                    _previousData = previous;
                    return this;
                });
            }

            public double GetCalculatedValue(string property, double scale = 1D) => GetCalculatedValue(_previousData, property, scale);

            private double GetCalculatedValue(PerfRawData previousData, string property, double scale)
            {
                var timestampDiff = Math.Max(1, Timestamp - previousData.Timestamp);
                var valueDiff = Convert.ToUInt64(_data[property]) - Convert.ToUInt64(previousData._data[property]);
                var scaledValueDiff = valueDiff * scale;
                return scaledValueDiff / timestampDiff;
            }
        }
    }
}
