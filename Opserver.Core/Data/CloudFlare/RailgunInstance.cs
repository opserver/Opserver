using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.CloudFlare
{
    public partial class RailgunInstance : PollNode, IEquatable<RailgunInstance>, ISearchableNode
    {
        string ISearchableNode.DisplayName => Host + ":" + Port.ToString() + " - " + Name;
        string ISearchableNode.Name => Host + ":" + Port.ToString();
        string ISearchableNode.CategoryName => "CloudFlare";

        public CloudFlareSettings.Railgun Settings { get; internal set; }
        public string Name => Settings.Name;
        public string Host => Settings.Host;
        public int Port => Settings.Port;

        public override string NodeType => "Railgun";
        public override int MinSecondsBetweenPolls => 5;

        public override IEnumerable<Cache> DataPollers
        {
            get { yield return Stats; }
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() { return ""; }

        public RailgunInstance(CloudFlareSettings.Railgun settings) : base(settings.Host + ":" + settings.Port.ToString())
        {
            Settings = settings;
        }

        public Func<Cache<T>, Task> GetFromRailgun<T>(string opName, Func<RailgunInstance, Task<T>> getFromConnection) where T : class
        {
            return UpdateCacheItem(description: "CloudFlare - Railgun Fetch: " + Name + ":" + opName,
                getData: () => getFromConnection(this),
                logExceptions: false,
                addExceptionData: e => e.AddLoggedData("Server", Name)
                    .AddLoggedData("Host", Host)
                    .AddLoggedData("Port", Port.ToString()));
        }

        public bool Equals(RailgunInstance other)
        {
            if (other == null) return false;
            return Host == other.Host && Port == other.Port;
        }

        public override string ToString() => Host + ":" + Port.ToString();
    }
}
