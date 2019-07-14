using System.Collections.Generic;
using System.Net;
using Opserver.Helpers;
using StackExchange.Redis;

namespace Opserver.Data.Redis
{
    public class RedisConnectionInfo
    {
        private AddressCache AddressCache { get; }
        public string Name => Settings.Name;
        public string Host => Server.HostName;
        public int Port => Settings.Port;
        public string Password => Settings.Password;
        public RedisFeatures Features { get; internal set; }
        public RedisHost Server { get; }
        internal RedisSettings.Instance Settings { get; set; }

        internal RedisConnectionInfo(RedisHost server, RedisSettings.Instance settings, AddressCache addressCache)
        {
            Server = server;
            Settings = settings;
            AddressCache = addressCache;
        }

        public List<IPAddress> IPAddresses => AddressCache.GetHostAddresses(Host);

        public override string ToString() => $"{Name} ({Host}:{Port})";
    }
}
