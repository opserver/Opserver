using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BookSleeve;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Data.Dashboard;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisInstance : PollNode, IEquatable<RedisInstance>, ISearchableNode
    {
        // TODO: Per-Instance searchability, sub-nodes
        string ISearchableNode.DisplayName { get { return Host + ":" + Port + " - " + Name; } }
        string ISearchableNode.Name { get { return Host + ":" + Port; } }
        string ISearchableNode.CategoryName { get { return "Redis"; } }

        public RedisConnectionInfo ConnectionInfo { get; internal set; }
        public string Name { get { return ConnectionInfo.Name; } }
        public string Host { get { return ConnectionInfo.Host; } }
        public int Port { get { return ConnectionInfo.Port; } }
        public string Password { get { return ConnectionInfo.Password; } }

        public override string NodeType { get { return "Redis"; } }
        public override int MinSecondsBetweenPolls { get { return 5; } }

        private RedisConnection _connection;
        private bool _connectionInitialized;
        public RedisConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = GetConnection(allowAdmin: true);
                }
                if (_connection.State != RedisConnectionBase.ConnectionState.Open)
                {
                    if (_connectionInitialized)
                    {
                        // broken connection, recreate and reconnect
                        _connection = GetConnection(allowAdmin: true);
                        _connectionInitialized = false;
                    }
                    _connection.Wait(_connection.Open());
                    _connectionInitialized = true;
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
            }
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            if (Role == RedisInfo.RedisInstanceRole.Unknown) yield return MonitorStatus.Critical;
            if (IsSlave && Replication.MasterLinkStatus != "up") yield return MonitorStatus.Warning;
            if (IsMaster && Replication.SlaveConnections.Any(s => s.Status != "online")) yield return MonitorStatus.Warning;
        }
        protected override string GetMonitorStatusReason()
        {
            if (Role == RedisInfo.RedisInstanceRole.Unknown) return "Unknown role";
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

        private RedisConnection GetConnection(int ioTimeout = 5000, int syncTimeout = 5000, bool allowAdmin = false)
        {
            return new RedisConnection(Host, Port, ioTimeout: ioTimeout, syncTimeout: syncTimeout, allowAdmin: allowAdmin, password: Password)
            {
                Name = "Opserver"
            };
        }

        public Action<Cache<T>> GetFromRedis<T>(string opName, Func<RedisConnection, T> getFromConnection) where T : class
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
