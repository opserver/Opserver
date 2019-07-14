using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Options;
using Opserver.Helpers;
using static Opserver.Data.Redis.RedisInfo;

namespace Opserver.Data.Redis
{
    public class RedisModule : StatusModule<RedisSettings>
    {
        public override string Name => "Redis";
        public override bool Enabled => Instances.Count > 0;

        public List<RedisReplicationGroup> ReplicationGroups { get; }
        public List<RedisHost> Hosts { get; }
        public List<RedisInstance> Instances { get; }
        public List<RedisConnectionInfo> Connections { get; }

        private readonly HashSet<string> HostNames;

        public RedisModule(IOptions<RedisSettings> settings, PollingService poller, AddressCache addressCache) : base(settings, poller)
        {
            Connections = LoadRedisConnections(settings.Value, addressCache);
            Instances = Connections.Select(rci => new RedisInstance(this, rci))
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

        public override MonitorStatus MonitorStatus => Instances.GetWorstStatus();
        public override bool IsMember(string node) => HostNames.Contains(node);

        private List<RedisConnectionInfo> LoadRedisConnections(RedisSettings settings, AddressCache addressCache)
        {
            var result = new List<RedisConnectionInfo>();
            var defaultServerInstances = settings.Defaults.Instances;
            var allServerInstances = settings.AllServers.Instances ?? Enumerable.Empty<RedisSettings.Instance>();

            foreach (var s in settings.Servers)
            {
                var server = new RedisHost(this, s);

                var count = result.Count;
                // Add instances that belong to any servers
                foreach (var asi in allServerInstances)
                {
                    result.Add(new RedisConnectionInfo(server, asi, addressCache));
                }

                // Add instances defined on this server
                foreach (var si in s.Instances)
                {
                    result.Add(new RedisConnectionInfo(server, si, addressCache));
                }

                // If we have no instances added at this point, defaults it is!
                if (defaultServerInstances != null && count == result.Count)
                {
                    foreach (var dsi in defaultServerInstances)
                    {
                        result.Add(new RedisConnectionInfo(server, dsi, addressCache));
                    }
                }
            }
            return result;
        }

        public RedisHost GetHost(string hostName)
        {
            if (hostName.IsNullOrEmpty() || hostName.Contains(":")) return null;

            return Hosts.Find(h => string.Equals(h.HostName, hostName, StringComparison.InvariantCultureIgnoreCase));
        }

        public RedisInstance GetInstance(RedisConnectionInfo info)
        {
            foreach (var i in Instances)
            {
                if (i.ConnectionInfo == info) return i;
            }
            return null;
        }

        public RedisInstance GetInstance(string connectionString)
        {
            if (connectionString.IsNullOrEmpty()) return null;
            if (connectionString.Contains(":"))
            {
                var parts = connectionString.Split(StringSplits.Colon);
                if (parts.Length != 2) return null;
                if (int.TryParse(parts[1], out int port)) return GetInstance(parts[0], port);
            }
            else
            {
                return GetAllInstances(connectionString).FirstOrDefault();
            }
            return null;
        }

        public RedisInstance GetInstance(string host, int port)
        {
            foreach (var ri in Instances)
            {
                if (ri.Host.HostName == host && ri.Port == port) return ri;
            }
            var shortHost = host.Split(StringSplits.Period)[0];
            foreach (var ri in Instances)
            {
                if (ri.Port == port && ri.ShortHost == shortHost) return ri;
            }
            return null;
        }

        public RedisInstance GetInstance(int port, IPAddress ipAddress)
        {
            foreach (var i in Instances)
            {
                if (i.ConnectionInfo.Port != port) continue;
                foreach (var ip in i.ConnectionInfo.IPAddresses)
                {
                    if (ip.Equals(ipAddress)) return i;
                }
            }
            return null;
        }

        public RedisInstance GetInstance(RedisSlaveInfo info) => GetInstance(info.Port, info.IPAddress);

        public List<RedisInstance> GetAllInstances(string node)
        {
            return Instances.Where(ri => string.Equals(ri.Host.HostName, node, StringComparison.InvariantCultureIgnoreCase)).ToList();
        }
    }
}
