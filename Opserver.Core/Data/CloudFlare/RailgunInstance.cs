using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.CloudFlare
{
    public partial class RailgunInstance : PollNode, IEquatable<RailgunInstance>, ISearchableNode
    {
        string ISearchableNode.DisplayName { get { return Host + ":" + Port + " - " + Name; } }
        string ISearchableNode.Name { get { return Host + ":" + Port; } }
        string ISearchableNode.CategoryName { get { return "CloudFlare"; } }

        public CloudFlareSettings.Railgun Settings { get; internal set; }
        public string Name { get { return Settings.Name; } }
        public string Host { get { return Settings.Host; } }
        public int Port { get { return Settings.Port; } }

        public override string NodeType { get { return "Railgun"; } }
        public override int MinSecondsBetweenPolls { get { return 5; } }
        
        public override IEnumerable<Cache> DataPollers
        {
            get { yield return Stats; }
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() { return ""; }

        public RailgunInstance(CloudFlareSettings.Railgun settings) : base(settings.Host + ":" + settings.Port)
        {
            Settings = settings;
        }

        public Action<Cache<T>> GetFromRailgun<T>(string opName, Func<RailgunInstance, Task<T>> getFromConnection) where T : class
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

        public override string ToString()
        {
            return string.Concat(Host, ": ", Port);
        }
    }
}
