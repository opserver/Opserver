using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Redis;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisInstance : PollNode, IEquatable<RedisInstance>, ISearchableNode
    {
        // TODO: Per-Instance searchability, sub-nodes
        string ISearchableNode.DisplayName => Host + ":" + Port + " - " + Name;
        string ISearchableNode.Name => Host + ":" + Port;
        string ISearchableNode.CategoryName => "Redis";

        public RedisConnectionInfo ConnectionInfo { get; internal set; }
        public string Name => ConnectionInfo.Name;
        public string Host => ConnectionInfo.Host;

        public Version Version => Info.HasData() ? Info.Data.Server.Version : null;

        public string Password => ConnectionInfo.Password;
        public int Port => ConnectionInfo.Port;

        // Redis is spanish for WE LOVE DANGER, I think.
        protected override TimeSpan BackoffDuration => TimeSpan.FromSeconds(5);

        public override string NodeType => "Redis";
        public override int MinSecondsBetweenPolls => 5;

        private ConnectionMultiplexer _connection;
        public ConnectionMultiplexer Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = GetConnection(allowAdmin: true);
                }
                if (!_connection.IsConnected)
                {
                    _connection.Configure();
                }
                return _connection;
            }
        }

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return Config;
                yield return Info;
                yield return Clients;
                yield return SlowLog;
                yield return Tiebreaker;
            }
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            // WE DON'T KNOW ANYTHING, k?
            if (Info.PollsTotal == 0)
            {
                yield return MonitorStatus.Unknown;
                yield break;
            }
            if (Role == RedisInfo.RedisInstanceRole.Unknown) yield return MonitorStatus.Critical;
            if (Info.LastPollStatus == FetchStatus.Fail) yield return MonitorStatus.Warning;
            if (Replication == null)
            {
                yield return MonitorStatus.Unknown;
                yield break;
            }
            if (IsSlave && Replication.MasterLinkStatus != "up") yield return MonitorStatus.Warning;
            if (IsMaster && Replication.SlaveConnections.Any(s => s.Status != "online")) yield return MonitorStatus.Warning;
        }
        protected override string GetMonitorStatusReason()
        {
            if (Role == RedisInfo.RedisInstanceRole.Unknown) return "Unknown role";
            if (Replication == null) return "Replication Unknown";
            if (IsSlave && Replication.MasterLinkStatus != "up") return "Master link down";
            if (IsMaster && Replication.SlaveConnections.Any(s => s.Status != "online")) return "Slave offline";
            return null;
        }

        public RedisInstance(RedisConnectionInfo connectionInfo) : base(connectionInfo.Host + ":" + connectionInfo.Port)
        {
            ConnectionInfo = connectionInfo;
        }

        public string GetServerName(string hostOrIp)
        {
            IPAddress addr;
            if (Current.Settings.Dashboard.Enabled && IPAddress.TryParse(hostOrIp, out addr))
            {
                var nodes = DashboardData.GetNodesByIP(addr).ToList();
                if (nodes.Count == 1) return nodes[0].PrettyName;
            }
            //System.Net.Dns.GetHostEntry("10.7.0.46").HostName.Split(StringSplits.Period).First()
            //TODO: Redis instance search
            return AppCache.GetHostName(hostOrIp);
        }

        // We're not doing a lot of redis access, so tone down the thread count to 1 socket queue handler
        public static SocketManager SharedSocketManager = new SocketManager("Opserver Shared");

        private ConnectionMultiplexer GetConnection(bool allowAdmin = false, int syncTimeout = 5000)
        {
            var config = new ConfigurationOptions
            {
                SyncTimeout = syncTimeout,
                AllowAdmin = allowAdmin,
                Password = Password,
                EndPoints =
                {
                    { Host, Port }
                },
                ClientName = "Opserver",
                SocketManager = SharedSocketManager
            };
            return ConnectionMultiplexer.Connect(config);
        }

        public Action<Cache<T>> GetFromRedisAsync<T>(string opName, Func<ConnectionMultiplexer, Task<T>> getFromConnection) where T : class
        {
            return UpdateCacheItem(description: "Redis Fetch: " + Name + ":" + opName,
                                   getData: () => getFromConnection(Connection),
                                   logExceptions: false,
                                   addExceptionData: e => e.AddLoggedData("Server", Name)
                                                           .AddLoggedData("Host", Host)
                                                           .AddLoggedData("Port", Port.ToString()));
        }

        public bool Equals(RedisInstance other)
        {
            if (other == null) return false;
            return Host == other.Host && Port == other.Port;
        }

        public override string ToString()
        {
            return string.Concat(Host, ": ", Port);
        }

        public RedisMemoryAnalysis GetDatabaseMemoryAnalysis(int database, bool runIfMaster = false)
        {
            var ci = ConnectionInfo;
            if (IsMaster && !runIfMaster)
            {
                // no slaves, and a master - boom
                if (SlaveCount == 0)
                    return new RedisMemoryAnalysis(ConnectionInfo, database)
                    {
                        ErrorMessage = "Cannot run memory analysis on a master - it hurts."
                    };

                // Go to the first slave, automagically
                ci = SlaveInstances.First().ConnectionInfo;
            }

            return RedisAnalyzer.AnalyzeDatabaseMemory(ci, database);
        }

        public void ClearDatabaseMemoryAnalysisCache(int database)
        {
            RedisAnalyzer.ClearDatabaseMemoryAnalysisCache(ConnectionInfo, database);
        }

        //static RedisInstance()
        //{
        // Cache all the things! - need ClientFlags first though
        //RuntimeTypeModel.Default
        //                .Add(typeof (ClientInfo), false)
        //                .Add("Address",
        //                     "AgeSeconds",
        //                     "IdleSeconds",
        //                     "Database",
        //                     "SubscriptionCount",
        //                     "PatternSubscriptionCount",
        //                     "TransactionCommandLength",
        //                     "FlagsRaw",
        //                     "ClientFlags",
        //                     "LastCommand",
        //                     "Name");
        //}
    }
}
