using System.Collections.Generic;
using System.Net;
using StackExchange.Opserver.Helpers;
using StackExchange.Redis;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisConnectionInfo
    {
        public string Name => Settings.Name;
        public string Host { get; internal set; }
        public int Port => Settings.Port;
        public string Password => Settings.Password;
        public RedisFeatures Features { get; internal set; }
        internal RedisSettings.Instance Settings { get; set; }

        internal RedisConnectionInfo(string host, RedisSettings.Instance settings)
        {
            Settings = settings;
            Host = host;
        }

        public List<IPAddress> IPAddresses => AppCache.GetHostAddresses(Host);

        public override string ToString()
        {
            return $"{Name} ({Host}:{Port})";
        }
    }
}
