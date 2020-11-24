using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using StackExchange.Opserver.Helpers;
using StackExchange.Redis;

namespace StackExchange.Opserver.Data.Redis
{
    public class RedisConnectionInfo
    {
        public string Name => Settings.Name;
        public string Host => Server.HostName;
        public int Port => Settings.Port;
        public string Password => Settings.Password;
        public SslProtocols? SslProtocols => ParseSslProtocols(Settings.SslProtocols);
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

        private static SslProtocols? ParseSslProtocols(string sslProtocols)
        {
            if (String.IsNullOrWhiteSpace(sslProtocols))
            {
                return null;
            }

            var result = System.Security.Authentication.SslProtocols.None;

            var protocols = sslProtocols.Split(',');

            for (int i = 0; i < protocols.Length; i++)
            {
                SslProtocols protocol;
                if (Enum.TryParse(protocols[i].Trim(), ignoreCase: true, out protocol))
                {
                    result |= protocol;
                }
            }

            return result;
        }
    }
}
