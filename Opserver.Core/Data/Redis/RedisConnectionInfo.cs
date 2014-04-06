using System.Collections.Generic;
using System.Net;
using BookSleeve;
using StackExchange.Opserver.Helpers;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisConnectionInfo
    {
        public string Name { get { return Settings.Name; } }
        public string Host { get; internal set; }
        public int Port { get { return Settings.Port; } }
        public string Password { get { return Settings.Password; } }
        public RedisFeatures Features { get; internal set; }
        internal RedisSettings.Instance Settings { get; set; }

        internal RedisConnectionInfo(string host, RedisSettings.Instance settings)
        {
            Settings = settings;
            Host = host;
        }

        public List<IPAddress> IPAddresses
        {
            get { return AppCache.GetHostAddresses(Host); }
        }

        public override string ToString()
        {
            return string.Format("{0} ({1}:{2})", Name, Host, Port);
        }
    }
}
