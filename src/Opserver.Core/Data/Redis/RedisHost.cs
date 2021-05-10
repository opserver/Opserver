using System;
using System.Collections.Generic;
using System.Linq;

namespace Opserver.Data.Redis
{
    public class RedisHost : IMonitorStatus
    {
        private RedisModule Module { get; }
        public string HostName { get; }
        internal string ReplicationGroupName { get; }

        public string Region { get; }
        public RedisReplicationGroup ReplicationGroup { get; internal set; }
        public List<RedisInstance> Instances { get; internal set; }

        public MonitorStatus MonitorStatus => Instances.GetWorstStatus();
        public string MonitorStatusReason => "Instance Status";

        public RedisHost(RedisModule module, RedisSettings.Server server)
        {
            Module = module;
            HostName = server.Name;
            Region = Module.Settings.Replication?.RegionHostRegex?.Match(HostName)?.Value ?? "Global";
            ReplicationGroupName = server.ReplicationGroup;
        }

        public bool InSameRegionAs(RedisHost host) => string.Equals(Region, host.Region, StringComparison.OrdinalIgnoreCase);

        public bool CanReplicateFrom(RedisHost host, out string reason)
        {
            foreach (var i in Instances)
            {
                foreach (var di in i.GetAllReplicasInChain())
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
            Instances.SelectMany(i => i.ReplicaInstances)
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
