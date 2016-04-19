using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Profiling;
using StackExchange.Redis;

namespace StackExchange.Opserver.Data.Redis
{
    public static class ServerUtils
    {
        public static IServer GetSingleServer(this ConnectionMultiplexer connection)
        {
            var endpoints = connection.GetEndPoints(configuredOnly: true);
            if (endpoints.Length == 1) return connection.GetServer(endpoints[0]);

            var sb = StringBuilderCache.Get().Append("expected one endpoint, got ").Append(endpoints.Length).Append(": ");
            foreach (var ep in endpoints)
                sb.Append(ep).Append(", ");
            sb.Length -= 2;
            throw new InvalidOperationException(sb.ToStringRecycle());
        }
    }
    public partial class RedisInstance
    {
        //Specially caching this since it's accessed so often
        public RedisInfo.ReplicationInfo Replication { get; private set; }

        private Cache<RedisInfo> _info;
        public Cache<RedisInfo> Info
        {
            get
            {
                return _info ?? (_info = new Cache<RedisInfo>
                {
                    CacheForSeconds = 10,
                    UpdateCache = GetFromRedisAsync(nameof(Info), async rc =>
                    {
                        var server = rc.GetSingleServer();
                        string infoStr;
                        //TODO: Remove when StackExchange.Redis gets profiling
                        using (MiniProfiler.Current.CustomTiming("redis", "INFO"))
                        {
                            infoStr = await server.InfoRawAsync().ConfigureAwait(false);
                        }
                        ConnectionInfo.Features = server.Features;
                        var ri = RedisInfo.FromInfoString(infoStr);
                        if (ri != null) Replication = ri.Replication;
                        return ri;
                    })
                });
            }
        }

        public RedisInfo.RedisInstanceRole Role
        {
            get
            {
                var lastRole = Replication?.RedisInstanceRole;
                // If we think we're a master and the last poll failed - look to other nodes for info
                if (!Info.LastPollSuccessful && lastRole == RedisInfo.RedisInstanceRole.Master &&
                    AllInstances.Any(r => r.SlaveInstances.Any(s => s == this)))
                    return RedisInfo.RedisInstanceRole.Slave;
                return lastRole ?? RedisInfo.RedisInstanceRole.Unknown;
            }
        }

        public string RoleDescription
        {
            get
            {
                switch (Role)
                {
                    case RedisInfo.RedisInstanceRole.Master:
                        return "Master";
                    case RedisInfo.RedisInstanceRole.Slave:
                        return "Slave";
                    default:
                        return "Unknown";
                }
            }
        }

        public bool IsMaster => Role == RedisInfo.RedisInstanceRole.Master;
        public bool IsSlave => Role == RedisInfo.RedisInstanceRole.Slave;
        public bool IsSlaving => IsSlave && (Replication.MasterLinkStatus != "up" || Info.Data?.Replication?.MastSyncLeftBytes > 0);

        public RedisInstance TopMaster
        {
            get
            {
                var top = this;
                while (top.Master != null)
                {
                    top = top.Master;
                }
                return top;
            }
        }

        public RedisInstance Master
        {
            get { return Replication?.MasterHost.HasValue() == true ? GetInstance(Replication.MasterHost, Replication.MasterPort) : AllInstances.FirstOrDefault(i => i.SlaveInstances.Contains(this)); }
        }

        public int SlaveCount => Replication?.ConnectedSlaves ?? 0;

        public int TotalSlaveCount
        {
            get { return SlaveCount + (SlaveCount > 0 && SlaveInstances != null ? SlaveInstances.Sum(s => s?.TotalSlaveCount ?? 0) : 0); }
        }

        public List<RedisInfo.RedisSlaveInfo> SlaveConnections => Replication?.SlaveConnections;

        public List<RedisInstance> SlaveInstances
        {
            get
            {
                return Info.LastPollSuccessful ? (Replication?.SlaveConnections.Select(s => s.GetServer()).ToList() ?? new List<RedisInstance>()) : new List<RedisInstance>();
                // If we can't poll this server, ONLY trust the other nodes we can poll
                //return AllInstances.Where(i => i.Master == this).ToList();
            }
        }

        public List<RedisInstance> GetAllSlavesInChain()
        {
            var slaves = SlaveInstances;
            return slaves.Union(slaves.SelectMany(i => i?.GetAllSlavesInChain() ?? Enumerable.Empty<RedisInstance>())).Distinct().ToList();
        }

        public class RedisInfoSection
        {
            /// <summary>
            /// Indicates if this section is for the entire INFO
            /// Pretty much means this is from a pre-2.6 release of redis
            /// </summary>
            public bool IsGlobal { get; internal set; }

            public string Title { get; internal set; }

            public List<RedisInfoLine> Lines { get; internal set; }
        }

        public class RedisInfoLine
        {
            public bool Important { get; internal set; }
            public string Key { get; internal set; }
            public string ParsedValue { get; internal set; }
            public string OriginalValue { get; internal set; }
        }
    }
}
