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
            foreach (var p in providers)
            {
                switch (p.Type.IsNullOrEmptyReturn("").ToLower())
                {
                    case "orion":
                        _dataProviders.Add(new OrionDataProvider(p));
                        break;
                    case "wmi":
                        _dataProviders.Add(new WmiDataProvider(p));
                        break;
                    // moar providers, feed me!
                }
            }
            _dataProviders.ForEach(p => p.TryAddToGlobalPollers());
            if (_dataProviders.Count == 1)
                _singleDataProvider = _dataProviders[0];
        }

        public static ReadOnlyCollection<DashboardDataProvider> DataProviders { get { return _dataProviders.AsReadOnly(); } }

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

        public static Node GetNodeById(int id)
        {
            return FirstById(p => p.GetNode(id));
        }

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

        public static Interface GetInterfaceById(int id)
        {
            return FirstById(p => p.GetInterface(id));
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
