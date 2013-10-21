using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.Redis
{
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
                        UpdateCache = GetFromRedis("INFO", rc =>
                        {
#pragma warning disable 618
                            var infoStr = rc.Wait(rc.GetInfo());
#pragma warning restore 618
                            if (ConnectionInfo.Features == null)
                                ConnectionInfo.Features = rc.Features;
                            var ri = RedisInfo.FromInfoString(infoStr);
                            if (ri != null) Replication = ri.Replication;
                            return ri;
                        })
                    });
            }
        }

        public RedisInfo.RedisInstanceRole Role { get { return Replication != null ? Replication.RedisInstanceRole : RedisInfo.RedisInstanceRole.Unknown; } }

        public bool IsMaster { get { return Role == RedisInfo.RedisInstanceRole.Master; } }
        public bool IsSlave { get { return Role == RedisInfo.RedisInstanceRole.Slave; } }
        public bool IsSlaving
        {
            get { return IsSlave && (Replication.MasterLinkStatus != "up" || Info.SafeData(true).Replication.MastSyncLeftBytes > 0); }
        }

        public RedisInstance Master
        {
            get
            {
                if (IsSlave && Replication.MasterHost.HasValue())
                    return GetInstance(Replication.MasterHost, Replication.MasterPort);
                return null;
            }
        }
        public int SlaveCount
        {
            get { return Replication != null ? Replication.ConnectedSlaves : 0; }
        }
        public int TotalSlaveCount
        {
            get { return SlaveCount + (SlaveCount > 0 && SlaveInstances != null ? SlaveInstances.Sum(s => s != null ? s.TotalSlaveCount : 0) : 0); }
        }
        public List<RedisInfo.RedisSlaveInfo> SlaveConnections
        {
            get { return Replication != null ? Replication.SlaveConnections : null; }
        }
        public List<RedisInstance> SlaveInstances
        {
            get
            {
                return
                    (Replication != null
                         ? Replication.SlaveConnections.Select(s => s.GetServer()).ToList()
                         : new List<RedisInstance>())
                        .Union(AllInstances.Where(i => i.Master == this)).ToList();
            }
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
