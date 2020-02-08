using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using StackExchange.Profiling;

namespace Opserver.Data.HAProxy
{
    public partial class HAProxyInstance : PollNode<HAProxyModule>, INodeRoleProvider
    {
        public string Name => Settings.Name;
        public string Description => Settings.Description;
        public int? QueryTimeoutMs => Settings.QueryTimeoutMs;
        public string Url => RawSettings.Url;
        public string User => Settings.User;
        public string Password => Settings.Password;
        public string AdminUser => Settings.AdminUser;
        public string AdminPassword => Settings.AdminPassword;

        public HAProxySettings.Instance RawSettings { get; }
        public HAProxySettings.InstanceSettings Settings { get; }

        public HAProxyGroup Group { get; internal set; }

        public bool HasAdminLogin => AdminUser.HasValue() && AdminPassword.HasValue();

        public override string NodeType => "HAProxy";
        public override int MinSecondsBetweenPolls => 5;

        public override IEnumerable<Cache> DataPollers
        {
            get { yield return Proxies; }
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            yield return Proxies.Data?.GetWorstStatus() ?? MonitorStatus.Unknown;
        }

        protected override string GetMonitorStatusReason()
        {
            if (Proxies.Data == null) return Name + ": No Data";

            var statuses = Proxies.Data
                .SelectMany(p => p.Servers)
                .GroupBy(s => s.ProxyServerStatus)
                .Where(g => g.Key.IsBad())
                .OrderByDescending(g => g.Key);
            return Name + ": " +
                   string.Join(", ", statuses.Select(g =>
                   {
                       var count = g.Count();
                       return $"{(count == 1 ? g.First().Name : count.Pluralize("Server"))} {g.Key.ShortDescription()}";
                   }));
        }

        public HAProxyInstance(HAProxyModule module, HAProxySettings.Instance instance, HAProxySettings.Group group = null)
            : base(module, instance.Name + ":" + instance.Description + " - " + instance.Url)
        {
            RawSettings = instance;
            Settings = Module.Settings.GetInstanceSettings(instance, group);
        }

        private Cache<List<Proxy>> _proxies;
        public Cache<List<Proxy>> Proxies =>
            _proxies ?? (_proxies = new Cache<List<Proxy>>(this, "HAProxy Fetch: " + Name,
                cacheDuration: 10.Seconds(),
                getData: FetchHAProxyStatsAsync,
                addExceptionData: e => e.AddLoggedData("Server", Name)
                    .AddLoggedData("Url", Url)
                    .AddLoggedData("QueryTimeout", QueryTimeoutMs.ToString())
            ));

        private async Task<List<Proxy>> FetchHAProxyStatsAsync()
        {
            var fetchUrl = Url + ";csv";
            using (MiniProfiler.Current.CustomTiming("http", fetchUrl, "GET"))
            {
                var req = (HttpWebRequest) WebRequest.Create(fetchUrl);
                req.Credentials = new NetworkCredential(User, Password);
                if (QueryTimeoutMs.HasValue)
                    req.Timeout = QueryTimeoutMs.Value;
                using (var resp = await req.GetResponseAsync())
                using (var rs = resp.GetResponseStream())
                {
                    if (rs == null) return null;
                    return await ParseHAProxyStats(rs);
                }
            }
        }

        private async Task<List<Proxy>> ParseHAProxyStats(Stream stream)
        {
            var stats = new List<Item>();
            using (var sr = new StreamReader(stream))
            {
                string line;
                while ((line = await sr.ReadLineAsync()) != null)
                {
                    //Skip the header
                    if (line.IsNullOrEmpty() || line.StartsWith("#")) continue;
                    //Collect each stat line as we go, group later
                    stats.Add(Item.FromLine(line));
                }
            }
            return stats.GroupBy(s => s.UniqueProxyId).Select(g => new Proxy
            {
                Instance = this,
                Name = g.First().ProxyName,
                Frontend = g.FirstOrDefault(s => s.Type == StatusType.Frontend) as Frontend,
                Servers = g.OfType<Server>().ToList(),
                Backend = g.FirstOrDefault(s => s.Type == StatusType.Backend) as Backend,
                PollDate = DateTime.UtcNow
            }).ToList();
        }

        public override string ToString() => string.Concat(Name, ": ", Url);

        public override int GetHashCode()
        {
            int key = 17;
            unchecked
            {
                key = (key * 23) + Name.GetHashCode();
                key = (key * 23) + Url.GetHashCode();
            }
            return key;
        }
    }
}
