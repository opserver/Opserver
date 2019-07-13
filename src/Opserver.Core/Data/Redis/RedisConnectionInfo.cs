using System.Collections.Generic;
using System.Net;
using Opserver.Helpers;
using StackExchange.Redis;

namespace Opserver.Data.Redis
{
    public class RedisConnectionInfo
    {
        public string Name => Settings.Name;
        public string Host => Server.HostName;
        public int Port => Settings.Port;
        public string Password => Settings.Password;
        public RedisFeatures Features { get; internal set; }
        public RedisHost Server { get; }
        internal RedisSettings.Instance Settings { get; set; }

        internal RedisConnectionInfo(RedisHost server, RedisSettings.Instance settings)
        {
            Server = server;
            Settings = settings;
        }

        public List<IPAddress> IPAddresses => AppCache.GetHostAddresses(Host);

        public override string ToString() => $"{Name} ({Host}:{Port})";
    }
}
