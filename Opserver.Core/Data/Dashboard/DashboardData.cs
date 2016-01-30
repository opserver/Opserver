using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using StackExchange.Opserver.Data.Dashboard.Providers;

namespace StackExchange.Opserver.Data.Dashboard
{
    public class DashboardData
    {
        public static bool HasData
        {
            get { return _dataProviders?.Any(p => p.HasData) ?? false; }
        }
        
        private static readonly List<DashboardDataProvider> _dataProviders;
    
        static DashboardData()
        {
            _dataProviders = new List<DashboardDataProvider>();
            if (!Current.Settings.Dashboard.Enabled)
            {
                _dataProviders.Add(new EmptyDataProvider("EmptyDataProvider"));
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
        }

        public static bool AnyDoingFirstPoll => _dataProviders.Any(p => p.FirstPollRun != null);

        public static ReadOnlyCollection<DashboardDataProvider> DataProviders => _dataProviders.AsReadOnly();

        public static IEnumerable<string> ProviderExceptions =>
            _dataProviders.Count == 1
                ? _dataProviders[0].GetExceptions()
                : _dataProviders.SelectMany(p => p.GetExceptions());

        public static List<Node> AllNodes =>
            _dataProviders.Count == 1
                ? _dataProviders[0].AllNodes
                : _dataProviders.SelectMany(p => p.AllNodes).ToList();

        public static Node GetNodeById(string id) => FirstById(p => p.GetNodeById(id));

        public static Node GetNodeByName(string hostName)
        {
            if (!Current.Settings.Dashboard.Enabled || hostName.IsNullOrEmpty()) return null;
            return AllNodes.FirstOrDefault(s => s.Name.ToLowerInvariant().Contains(hostName.ToLowerInvariant()));
        }

        public static IEnumerable<Node> GetNodesByIP(IPAddress ip)
        {
            return _dataProviders.Count == 0
                ? _dataProviders[0].GetNodesByIP(ip)
                : _dataProviders.SelectMany(p => p.GetNodesByIP(ip));
        }

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
    }
}
