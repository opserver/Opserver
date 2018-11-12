using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.Redis
{
    public class RedisHost : IMonitorStatus
    {
        public string HostName { get; }
        internal string ReplicationGroupName { get; }

        public string Region { get; }
        public RedisReplicationGroup ReplicationGroup { get; internal set; }
        public List<RedisInstance> Instances { get; internal set; }

        public MonitorStatus MonitorStatus => Instances.GetWorstStatus();
        public string MonitorStatusReason => "Instance Status";

        public RedisHost(RedisSettings.Server server)
        {
            HostName = server.Name;
            Region = Current.Settings.Redis.Replication?.RegionHostRegex?.Match(HostName)?.Value ?? "Global";
            ReplicationGroupName = server.ReplicationGroup;
        }

        public static RedisHost Get(string hostName)
        {
            if (hostName.IsNullOrEmpty() || hostName.Contains(":")) return null;

            return RedisModule.Hosts.Find(h => string.Equals(h.HostName, hostName, StringComparison.InvariantCultureIgnoreCase));
        }

        public bool InSameRegionAs(RedisHost host) => string.Equals(Region, host.Region, StringComparison.OrdinalIgnoreCase);

        public bool CanSlaveTo(RedisHost host, out string reason)
        {
            foreach (var i in Instances)
            {
                foreach (var di in i.GetAllSlavesInChain())
                {
                    if (di?.ConnectionInfo?.Server == host)
                    {
                        reason = $"Current chain contains {di.HostAndPort}";
                        return false;
                    }
                }
            }
            reason = "";
            return true;
        }

        public class ReplicaSummary
        {
            public RedisHost SourceHost { get; internal set; }
            public RedisHost TargetHost { get; internal set; }
            public List<RedisInstance> ReplicatedInstances { get; internal set; }
        }

        public bool IsTopLevel => Instances.Any(i => i.IsMaster);

        public List<ReplicaSummary> GetDownstreamHosts(List<RedisInstance> filterTo = null) =>
            Instances.SelectMany(i => i.SlaveInstances)
                     .Where(i => filterTo == null || filterTo.Contains(i.Master))
                     .GroupBy(i => i.Host)
                     .Select(g => new ReplicaSummary
                     {
                         SourceHost = this,
                         TargetHost = g.Key,
                         ReplicatedInstances = g.ToList()
                     }).ToList();

        public override string ToString() => HostName;
    }
}
