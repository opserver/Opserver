using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            var sb = new StringBuilder("expected one endpoint, got ").Append(endpoints.Length).Append(": ");
            foreach (var ep in endpoints)
                sb.Append(ep).Append(", ");
            sb.Length -= 2;
            throw new InvalidOperationException(sb.ToString());
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
                    CacheForSeconds = 5,
                    UpdateCache = GetFromRedisAsync("INFO", async rc =>
                    {
                        var server = rc.GetSingleServer();
                        string infoStr;
                            //TODO: Remove when StackExchange.Redis gets profiling
                            using (MiniProfiler.Current.CustomTiming("redis", "INFO"))
                        {
                            infoStr = await server.InfoRawAsync();
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
                if (AllInstances.Any(r => r.SlaveInstances.Any(s => s == this)))
                    return RedisInfo.RedisInstanceRole.Slave;
                return Replication?.RedisInstanceRole ?? RedisInfo.RedisInstanceRole.Unknown;
            }
        }

        public bool IsMaster => Role == RedisInfo.RedisInstanceRole.Master;
        public bool IsSlave => Role == RedisInfo.RedisInstanceRole.Slave;
        public bool IsSlaving => IsSlave && (Replication.MasterLinkStatus != "up" || Info.SafeData(true).Replication?.MastSyncLeftBytes > 0);

        public RedisInstance TopMaster
        {
            get
            {
                var top = this;
                while (top.Master != null) { top = top.Master; }
                return top;
            }
        }

        public RedisInstance Master
        {
            get
            {
                if (Replication?.MasterHost.HasValue() == true)
                    return GetInstance(Replication.MasterHost, Replication.MasterPort);
                return AllInstances.FirstOrDefault(i => i.SlaveInstances.Contains(this));
            }
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
                if (Info.LastPollStatus == FetchStatus.Success)
                {
                    return (Replication?.SlaveConnections.Select(s => s.GetServer()).ToList() ?? new List<RedisInstance>());
                }
                // If we can't poll this server, ONLY trust the other nodes we can poll
                //return AllInstances.Where(i => i.Master == this).ToList();
                return new List<RedisInstance>();
            }
        }

        public List<RedisInstance> GetAllSlavesInChain()
        {
            var slaves = SlaveInstances;
            return slaves.Union(slaves.SelectMany(i => i?.GetAllSlavesInChain())).Distinct().ToList();
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
