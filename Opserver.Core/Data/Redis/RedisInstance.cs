﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Redis;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisInstance : PollNode, IEquatable<RedisInstance>, ISearchableNode
    {
        // TODO: Per-Instance searchability, sub-nodes
        string ISearchableNode.DisplayName => HostAndPort + " - " + Name;
        string ISearchableNode.Name => HostAndPort;
        string ISearchableNode.CategoryName => "Redis";

        public RedisConnectionInfo ConnectionInfo { get; internal set; }
        public string Name => ConnectionInfo.Name;
        public string Host => ConnectionInfo.Host;
        public string ShortHost { get; internal set; }

        public Version Version => Info.Data?.Server.Version;

        public string Password => ConnectionInfo.Password;
        public int Port => ConnectionInfo.Port;
        public bool UseSsl => ConnectionInfo.Settings.UseSSL;

        private string _hostAndPort;
        public string HostAndPort => _hostAndPort ?? (_hostAndPort = Host + ":" + Port.ToString());

        // Redis is spanish for WE LOVE DANGER, I think.
        protected override TimeSpan BackoffDuration => TimeSpan.FromSeconds(5);

        public override string NodeType => "Redis";
        public override int MinSecondsBetweenPolls => 5;

        private readonly object _connectionLock = new object();
        private ConnectionMultiplexer _connection;
        public ConnectionMultiplexer Connection
        {
            get
            {
                if (_connection == null || !_connection.IsConnected)
                {
                    _connection = _connection ?? GetConnection(allowAdmin: true);
                    if (!_connection.IsConnected)
                    {
                        _connection.Configure();
                    }
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
            if (!Info.LastPollSuccessful) yield return MonitorStatus.Warning;
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

        public RedisInstance(RedisConnectionInfo connectionInfo) : base(connectionInfo.Host + ":" + connectionInfo.Port.ToString())
        {
            ConnectionInfo = connectionInfo;
            ShortHost = Host.Split(StringSplits.Period)[0];
        }

        public string GetServerName(string hostOrIp)
        {
            IPAddress addr;
            if (Current.Settings.Dashboard.Enabled && IPAddress.TryParse(hostOrIp, out addr))
            {
                var nodes = DashboardModule.GetNodesByIP(addr).ToList();
                if (nodes.Count == 1) return nodes[0].PrettyName;
            }
            //System.Net.Dns.GetHostEntry("10.7.0.46").HostName.Split(StringSplits.Period).First()
            //TODO: Redis instance search
            return AppCache.GetHostName(hostOrIp);
        }

        // We're not doing a lot of redis access, so tone down the thread count to 1 socket queue handler
        public static readonly SocketManager SharedSocketManager = new SocketManager("Opserver Shared");

        private ConnectionMultiplexer GetConnection(bool allowAdmin = false, int syncTimeout = 10000)
        {
            var config = new ConfigurationOptions
            {
                SyncTimeout = syncTimeout,
                ConnectTimeout = 20000,
                AllowAdmin = allowAdmin,
                Password = Password,
                Ssl=UseSsl,
                EndPoints =
                {
                    { Host, Port }
                },
                ClientName = "Opserver",
                SocketManager = SharedSocketManager
            };
            return ConnectionMultiplexer.Connect(config);
        }

        private Cache<T> GetRedisCache<T>(
            TimeSpan cacheDuration,
            Func<Task<T>> get,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            ) where T : class
        {
            return new Cache<T>(this, "Redis Fetch: " + Name + ":" + memberName,
                cacheDuration,
                get,
                addExceptionData: e => e.AddLoggedData("Server", Name)
                                        .AddLoggedData("Host", Host)
                                        .AddLoggedData("Port", Port.ToString()),
                timeoutMs: 10000,
                memberName: memberName,
                sourceFilePath: sourceFilePath,
                sourceLineNumber: sourceLineNumber
            );
        }

        public bool Equals(RedisInstance other)
        {
            if (other == null) return false;
            return Host == other.Host && Port == other.Port;
        }

        public override string ToString() => HostAndPort;

        public RedisMemoryAnalysis GetDatabaseMemoryAnalysis(int database, bool runIfMaster = false)
        {
            var ci = ConnectionInfo;
            if (IsMaster && !runIfMaster)
            {
                // no slaves, and a master - boom
                if (SlaveCount == 0)
                {
                    return new RedisMemoryAnalysis(ConnectionInfo, database)
                    {
                        ErrorMessage = "Cannot run memory analysis on a master - it hurts."
                    };
                }

                // Go to the first slave, automagically
                ci = SlaveInstances[0].ConnectionInfo;
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
