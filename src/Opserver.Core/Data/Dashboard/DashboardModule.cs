using Microsoft.Extensions.Options;
using StackExchange.Opserver.Data.Dashboard.Providers;
using StackExchange.Opserver.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace StackExchange.Opserver.Data.Dashboard
{
    public class DashboardModule : StatusModule<DashboardSettings>
    {
        public override string Name => "Dashboard";
        public override bool Enabled { get; }
        public List<DashboardDataProvider> Providers { get; } = new List<DashboardDataProvider>();
        public List<DashboardCategory> AllCategories { get; }

        public DashboardModule(IOptions<DashboardSettings> settings) : base(settings)
        {
            var providers = settings.Value.Providers;
            if (providers == null || !providers.Any())
            {
                Providers.Add(new EmptyDataProvider(this, "EmptyDataProvider"));
                return;
            }

            Enabled = true;
            foreach (var p in providers.All)
            {
                p?.Normalize();
            }

            // Add each provider type here
            if (providers.Bosun != null)
                Providers.Add(new BosunDataProvider(this, providers.Bosun));
            if (providers.Orion != null)
                Providers.Add(new OrionDataProvider(this, providers.Orion));
            if (providers.WMI != null)
                Providers.Add(new WmiDataProvider(this, providers.WMI));

            Providers.ForEach(p => p.TryAddToGlobalPollers());

            AllCategories = settings.Value.Categories
                .Select(sc => new DashboardCategory(this, sc))
                .ToList();
        }

        public override MonitorStatus MonitorStatus => AllNodes.GetWorstStatus();
        public override bool IsMember(string node)
        {
            return GetNodeByName(node) != null || GetNodeById(node) != null;
        }

        public bool HasData => Providers?.Any(p => p.HasData) ?? false;

        public bool AnyDoingFirstPoll => Providers.Any(p => p.LastPoll == null);

        public IEnumerable<string> ProviderExceptions =>
            Providers.Count == 1
                ? Providers[0].GetExceptions()
                : Providers.SelectMany(p => p.GetExceptions());

        public List<Node> AllNodes =>
            Providers.Count == 1
                ? Providers[0].AllNodes
                : Providers.SelectMany(p => p.AllNodes).ToList();

        public Node GetNodeById(string id) => FirstById(p => p.GetNodeById(id));

        public Node GetNodeByName(string hostName)
        {
            if (!Settings.Enabled || hostName.IsNullOrEmpty()) return null;
            return AllNodes.Find(s => s.Name.Equals(hostName, StringComparison.InvariantCultureIgnoreCase)) ??
				AllNodes.Find(s => s.Name.IndexOf(hostName, StringComparison.InvariantCultureIgnoreCase) >= 0);
        }

        public string GetServerName(string hostOrIp)
        {
            if (Settings.Enabled && IPAddress.TryParse(hostOrIp, out var addr))
            {
                var nodes = GetNodesByIP(addr).ToList();
                if (nodes.Count == 1) return nodes[0].PrettyName;
            }
            return AppCache.GetHostName(hostOrIp);
        }

        public IEnumerable<Node> GetNodesByIP(IPAddress ip) =>
            Providers.Count == 1
                ? Providers[0].GetNodesByIP(ip)
                : Providers.SelectMany(p => p.GetNodesByIP(ip));

        private T FirstById<T>(Func<DashboardDataProvider, T> fetch) where T : class
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
