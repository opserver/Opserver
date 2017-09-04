using StackExchange.Opserver.Data.Dashboard.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace StackExchange.Opserver.Data.Dashboard
{
    public class DashboardModule : StatusModule
    {
        public static bool Enabled { get; }
        public static List<DashboardDataProvider> Providers { get; } = new List<DashboardDataProvider>();

        static DashboardModule()
        {
            var providers = Current.Settings.Dashboard.Providers;
            if (providers == null || !providers.Any())
            {
                Providers.Add(new EmptyDataProvider("EmptyDataProvider"));
                return;
            }

            Enabled = true;
            foreach (var p in providers.All)
            {
                p?.Normalize();
            }

            // Add each provider type here
            if (providers.Bosun != null)
                Providers.Add(new BosunDataProvider(providers.Bosun));
            if (providers.Orion != null)
                Providers.Add(new OrionDataProvider(providers.Orion));
            if (providers.WMI != null)
                Providers.Add(new WmiDataProvider(providers.WMI));

            Providers.ForEach(p => p.TryAddToGlobalPollers());
        }

        public override bool IsMember(string node)
        {
            return GetNodeByName(node) != null || GetNodeById(node) != null;
        }

        public static bool HasData => Providers?.Any(p => p.HasData) ?? false;

        public static bool AnyDoingFirstPoll => Providers.Any(p => p.LastPoll == null);

        public static IEnumerable<string> ProviderExceptions =>
            Providers.Count == 1
                ? Providers[0].GetExceptions()
                : Providers.SelectMany(p => p.GetExceptions());

        public static List<Node> AllNodes =>
            Providers.Count == 1
                ? Providers[0].AllNodes
                : Providers.SelectMany(p => p.AllNodes).ToList();

        public static Node GetNodeById(string id) => FirstById(p => p.GetNodeById(id));

        public static Node GetNodeByName(string hostName)
        {
            if (!Current.Settings.Dashboard.Enabled || hostName.IsNullOrEmpty()) return null;
            return AllNodes.Find(s => s.Name.Equals(hostName, StringComparison.InvariantCultureIgnoreCase)) ??
				AllNodes.Find(s => s.Name.ToLowerInvariant().Contains(hostName.ToLowerInvariant()));
        }

        public static IEnumerable<Node> GetNodesByIP(IPAddress ip) =>
            Providers.Count == 1
                ? Providers[0].GetNodesByIP(ip)
                : Providers.SelectMany(p => p.GetNodesByIP(ip));

        private static T FirstById<T>(Func<DashboardDataProvider, T> fetch) where T : class
        {
            if (!Enabled) return null;
            foreach (var p in Providers)
            {
                var i = fetch(p);
                if (i != null) return i;
            }
            return null;
        }
    }
}
