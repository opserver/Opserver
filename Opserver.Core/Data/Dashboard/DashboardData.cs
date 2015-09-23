using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using StackExchange.Opserver.Data.Dashboard.Providers;

namespace StackExchange.Opserver.Data.Dashboard
{
    public class DashboardData
    {
        public static bool HasData
        {
            get { return _dataProviders != null && _dataProviders.Any(p => p.HasData); }
        }

        // special case for the common single provider case
        private static readonly DashboardDataProvider _singleDataProvider;
        private static readonly List<DashboardDataProvider> _dataProviders;
    
        static DashboardData()
        {
            _dataProviders = new List<DashboardDataProvider>();
            if (!Current.Settings.Dashboard.Enabled)
            {
                _singleDataProvider = new EmptyDataProvider("EmptyDataProvider");
                return;
            }
            var providers = Current.Settings.Dashboard.Providers;

            foreach (var p in providers.All)
            {
                p?.Normalize();
            }
            
            // Add each provider type here
            if (providers.Bosun != null)
                _dataProviders.Add(new BosunDataProvider(providers.Bosun));
            if (providers.Orion != null)
                _dataProviders.Add(new OrionDataProvider(providers.Orion));
            if (providers.WMI != null)
                _dataProviders.Add(new WmiDataProvider(providers.WMI));


            _dataProviders.ForEach(p => p.TryAddToGlobalPollers());
            if (_dataProviders.Count == 1)
                _singleDataProvider = _dataProviders[0];
        }

        public static ReadOnlyCollection<DashboardDataProvider> DataProviders => _dataProviders.AsReadOnly();

        public static IEnumerable<string> ProviderExceptions
        {
            get
            {
                return _singleDataProvider != null
                           ? _singleDataProvider.GetExceptions()
                           : _dataProviders.SelectMany(p => p.GetExceptions());
            }
        }

        public static List<Node> AllNodes
        {
            get
            {
                return _singleDataProvider != null
                           ? _singleDataProvider.AllNodes
                           : _dataProviders.SelectMany(p => p.AllNodes).ToList();
            }
        }

        public static Node GetNodeById(string id) => FirstById(p => p.GetNodeById(id));

        public static Node GetNodeByName(string hostName)
        {
            if (!Current.Settings.Dashboard.Enabled || hostName.IsNullOrEmpty()) return null;
            return AllNodes.FirstOrDefault(s => s.Name.ToLowerInvariant().Contains(hostName.ToLowerInvariant()));
        }

        public static IEnumerable<Node> GetNodesByIP(IPAddress ip)
        {
            return _singleDataProvider != null
                       ? _singleDataProvider.GetNodesByIP(ip)
                       : _dataProviders.SelectMany(p => p.GetNodesByIP(ip));
        }

        public static Interface GetInterfaceById(string id) => FirstById(p => p.GetInterface(id));

        private static T FirstById<T>(Func<DashboardDataProvider, T> fetch) where T : class
        {
            if (!Current.Settings.Dashboard.Enabled) return null;
            foreach (var p in _dataProviders)
            {
                var i = fetch(p);
                if (i != null) return i;
            }
            return null;
        }
        
        /// <summary>
        /// Gets CPU usage for this node (optionally) for the given time period, optionally sampled if pointCount is specified
        /// </summary>
        /// <param name="id">ID of the node</param>
        /// <param name="start">Start date, unbounded if null</param>
        /// <param name="end">End date, unbounded if null</param>
        /// <param name="pointCount">Points to return, if specified results will be sampled rather than including every point</param>
        /// <returns>CPU usage data points</returns>
        public static Task<List<Node.CPUUtilization>> GetCPUUtilization(string id, DateTime? start, DateTime? end, int? pointCount = null)
        {
            foreach (var p in _dataProviders)
            {
                var n = p.GetNodeById(id);
                if (n != null) return p.GetCPUUtilization(n, start, end, pointCount);
            }
            return Task.FromResult(new List<Node.CPUUtilization>());
        }

        /// <summary>
        /// Gets memory usage for this node (optionally) for the given time period, optionally sampled if pointCount is specified
        /// </summary>
        /// <param name="id">ID of the node</param>
        /// <param name="start">Start date, unbounded if null</param>
        /// <param name="end">End date, unbounded if null</param>
        /// <param name="pointCount">Points to return, if specified results will be sampled rather than including every point</param>
        /// <returns>Memory usage data points</returns>
        public static Task<List<Node.MemoryUtilization>> GetMemoryUtilization(string id, DateTime? start, DateTime? end, int? pointCount = null)
        {
            foreach (var p in _dataProviders)
            {
                var n = p.GetNodeById(id);
                if (n != null) return p.GetMemoryUtilization(n, start, end, pointCount);
            }
            return Task.FromResult(new List<Node.MemoryUtilization>());
        }



        /// <summary>
        /// Gets usage for this interface (optionally) for the given time period, optionally sampled if pointCount is specified
        /// </summary>
        /// <param name="id">ID of the interface</param>
        /// <param name="start">Start date, unbounded if null</param>
        /// <param name="end">End date, unbounded if null</param>
        /// <param name="pointCount">Points to return, if specified results will be sampled rather than including every point</param>
        /// <returns>Interface usage data points</returns>
        public static Task<List<Interface.InterfaceUtilization>> GetInterfaceUtilization(string id, DateTime? start, DateTime? end, int? pointCount = null)
        {
            foreach (var p in _dataProviders)
            {
                var i = p.GetInterface(id);
                if (i != null) return p.GetUtilization(i, start, end, pointCount);
            }
            return Task.FromResult(new List<Interface.InterfaceUtilization>());
        }



        /// <summary>
        /// Gets usage for this volume (optionally) for the given time period, optionally sampled if pointCount is specified
        /// </summary>
        /// <param name="id">ID of the interface</param>
        /// <param name="start">Start date, unbounded if null</param>
        /// <param name="end">End date, unbounded if null</param>
        /// <param name="pointCount">Points to return, if specified results will be sampled rather than including every point</param>
        /// <returns>Volume usage data points</returns>
        public Task<List<Volume.VolumeUtilization>> GetVolumeUtilization(string id, DateTime? start, DateTime? end, int? pointCount = null)
        {
            foreach (var p in _dataProviders)
            {
                var v = p.GetVolume(id);
                if (v != null) return p.GetUtilization(v, start, end, pointCount);
            }
            return Task.FromResult(new List<Volume.VolumeUtilization>());
        }
    }
}
