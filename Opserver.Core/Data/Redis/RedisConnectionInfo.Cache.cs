using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisConnectionInfo
    {
        public static List<RedisConnectionInfo> AllConnections
        {
            get { return _redisConnectionInfos ?? (_redisConnectionInfos = LoadRedisConnections()); }
        }

        private static readonly object _loadLock = new object();
        private static List<RedisConnectionInfo> _redisConnectionInfos;
        private static List<RedisConnectionInfo> LoadRedisConnections()
        {
            lock (_loadLock)
            {
                if (_redisConnectionInfos != null) return _redisConnectionInfos;

                var result = new List<RedisConnectionInfo>();
                if (Current.Settings.Redis.Enabled)
                {
                    var servers = Current.Settings.Redis.Servers;

                    var allServers = Current.Settings.Redis.AllServers;
                    if (allServers != null && allServers.Instances.Any())
                    {
                        var globalInstances = allServers.Instances;
                        foreach (var s in servers)
                        {
                            globalInstances.ForEach(gi => result.Add(new RedisConnectionInfo(s.Name, gi)));
                            if (s.Instances.Any())
                                s.Instances.ForEach(i => result.Add(new RedisConnectionInfo(s.Name, i)));
                        }
                    }
                }
                return result;
            }
        }

        public static RedisConnectionInfo FromCache(string host, int port)
        {
            return AllConnections.FirstOrDefault(i => i.Host == host && i.Port == port);
        }
    }
}
