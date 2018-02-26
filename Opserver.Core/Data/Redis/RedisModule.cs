using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.Redis
{
    public class RedisModule : StatusModule
    {
        public static bool Enabled => Instances.Count > 0;
        public static List<RedisReplicationGroup> ReplicationGroups { get; }
        public static List<RedisHost> Hosts { get; }
        public static List<RedisInstance> Instances { get; }
        public static List<RedisConnectionInfo> Connections { get; }

        private static readonly HashSet<string> HostNames;

        static RedisModule()
        {
            Connections = LoadRedisConnections();
            Instances = Connections.Select(rci => new RedisInstance(rci))
                                   .Where(rsi => rsi.TryAddToGlobalPollers())
                                   .ToList();

            Hosts = Instances.GroupBy(i => i.ConnectionInfo.Server)
                             .Select(g => { g.Key.Instances = g.ToList(); return g.Key; })
                             .ToList();

            ReplicationGroups = Hosts.Where(h => h.ReplicationGroupName.HasValue())
                                     .GroupBy(h => h.ReplicationGroupName)
                                     .Select(g => new RedisReplicationGroup(g.Key, g.ToList()))
                                     .ToList();

            HostNames = new HashSet<string>(Hosts.Select(h => h.HostName), StringComparer.OrdinalIgnoreCase);
        }

        public override bool IsMember(string node) => HostNames.Contains(node);

        private static List<RedisConnectionInfo> LoadRedisConnections()
        {
            var result = new List<RedisConnectionInfo>();
            var defaultServerInstances = Current.Settings.Redis.Defaults.Instances;
            var allServerInstances = Current.Settings.Redis.AllServers.Instances ?? Enumerable.Empty<RedisSettings.Instance>();

            foreach (var s in Current.Settings.Redis.Servers)
            {
                var server = new RedisHost(s);

                var count = result.Count;
                // Add instances that belong to any servers
                foreach (var asi in allServerInstances)
                {
                    result.Add(new RedisConnectionInfo(server, asi));
                }

                // Add instances defined on this server
                foreach (var si in s.Instances)
                {
                    result.Add(new RedisConnectionInfo(server, si));
                }

                // If we have no instances added at this point, defaults it is!
                if (defaultServerInstances != null && count == result.Count)
                {
                    foreach (var dsi in defaultServerInstances)
                    {
                        result.Add(new RedisConnectionInfo(server, dsi));
                    }
                }
            }
            return result;
        }
    }
}
