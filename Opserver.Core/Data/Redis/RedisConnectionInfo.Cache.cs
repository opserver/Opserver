using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisConnectionInfo
    {
        public static List<RedisConnectionInfo> AllConnections { get; } = LoadRedisConnections();
        
        private static List<RedisConnectionInfo> LoadRedisConnections()
        {
            var result = new List<RedisConnectionInfo>();
            if (!Current.Settings.Redis.Enabled) return result;

            var servers = Current.Settings.Redis.Servers;

            var defaultServerInstances = Current.Settings.Redis.Defaults.Instances;
            var allServerInstances = Current.Settings.Redis.AllServers.Instances;

            foreach (var s in servers)
            {
                var count = result.Count;
                // Add instances that belong to any servers
                allServerInstances?.ForEach(gi => result.Add(new RedisConnectionInfo(s.Name, gi)));

                // Add instances defined on this server
                if (s.Instances.Any())
                    s.Instances.ForEach(i => result.Add(new RedisConnectionInfo(s.Name, i)));

                // If we have no instances added at this point, defaults it is!
                if (defaultServerInstances != null && count == result.Count)
                    defaultServerInstances.ForEach(gi => result.Add(new RedisConnectionInfo(s.Name, gi)));
            }
            return result;
        }

        public static RedisConnectionInfo FromCache(string host, int port)
        {
            return AllConnections.FirstOrDefault(i => i.Host == host && i.Port == port);
        }
    }
}
