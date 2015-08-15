using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisInstance
    {
        public static List<RedisInstance> AllInstances => _redisInstances ?? (_redisInstances = LoadRedisServerInfos());

        private static readonly object _loadLock = new object();
        private static List<RedisInstance> _redisInstances;
        private static List<RedisInstance> LoadRedisServerInfos()
        {
            lock (_loadLock)
            {
                if (_redisInstances != null) return _redisInstances;

                return RedisConnectionInfo.AllConnections.Select(rci => new RedisInstance(rci))
                                          .Where(rsi => rsi.TryAddToGlobalPollers()).ToList();
            }
        }

        public static RedisInstance GetInstance(RedisConnectionInfo info)
        {
            return AllInstances.FirstOrDefault(ri => ri.ConnectionInfo == info);
        }
        public static RedisInstance GetInstance(string connectionString)
        {
            if (connectionString.IsNullOrEmpty()) return null;
            if (connectionString.Contains(":"))
            {
                var parts = connectionString.Split(StringSplits.Colon);
                if (parts.Length != 2) return null;
                int port;
                if (int.TryParse(parts[1], out port)) return GetInstance(parts[0], port);
            }
            else
            {
                return GetInstances(connectionString).FirstOrDefault();
            }
            return null;
        }
        public static RedisInstance GetInstance(string host, int port)
        {
            return AllInstances.FirstOrDefault(ri => ri.Host == host && ri.Port == port)
                ?? AllInstances.FirstOrDefault(ri => ri.Host.Split(StringSplits.Period).First() == host.Split(StringSplits.Period).First() && ri.Port == port);
        }
        public static RedisInstance GetInstance(int port, IPAddress ipAddress)
        {
            return AllInstances.FirstOrDefault(s => s.ConnectionInfo.Port == port
                                                  && s.ConnectionInfo.IPAddresses.Any(ip => ip.Equals(ipAddress)));
        }

        public static List<RedisInstance> GetInstances(string node)
        {
            var infos = AllInstances.Where(ri => string.Equals(ri.Host, node, StringComparison.InvariantCultureIgnoreCase));
            return infos.ToList();
        }

        public static bool IsRedisServer(string node)
        {
            return AllInstances.Any(i => string.Equals(i.Host, node, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
