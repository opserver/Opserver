using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.Redis
{
    public class RedisModule : StatusModule
    {
        public static bool Enabled => Instances.Count > 0;
        public static List<RedisInstance> Instances { get; }
        public static List<RedisConnectionInfo> Connections { get; }

        static RedisModule()
        {
            Connections = LoadRedisConnections();
            Instances = Connections
                .Select(rci => new RedisInstance(rci))
                .Where(rsi => rsi.TryAddToGlobalPollers())
                .ToList();
        }
        
        public override bool IsMember(string node) 
        {
            foreach (var i in Instances)
            {
                // TODO: Dictionary
                if (string.Equals(i.Host, node, StringComparison.InvariantCultureIgnoreCase)) return true;
            }
            return false;
        }

        private static List<RedisConnectionInfo> LoadRedisConnections()
        {
            var result = new List<RedisConnectionInfo>();
            var defaultServerInstances = Current.Settings.Redis.Defaults.Instances;
            var allServerInstances = Current.Settings.Redis.AllServers.Instances;

            foreach (var s in Current.Settings.Redis.Servers)
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
    }
}
