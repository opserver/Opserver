using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MongoDB.Bson;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Data.Dashboard;
using MongoDB.Driver;

namespace StackExchange.Opserver.Data.MongoDB
{
    public partial class MongoDBInstance : PollNode, IEquatable<MongoDBInstance>, ISearchableNode
    {
        // TODO: Per-Instance searchability, sub-nodes
        string ISearchableNode.DisplayName => HostAndPort + " - " + Name;
        string ISearchableNode.Name => HostAndPort;
        string ISearchableNode.CategoryName => "MongoDB";

        public MongoDBConnectionInfo ConnectionInfo { get; internal set; }
        public string Name => ConnectionInfo.Name;
        public string Host => ConnectionInfo.Host;
        public string ShortHost { get; internal set; }

        public Version Version => Info.HasData() ? Info.Data.Server.Version : null;

        public string Password => ConnectionInfo.Password;
        public int Port => ConnectionInfo.Port;

        private string _hostAndPort;
        public string HostAndPort => _hostAndPort ?? (_hostAndPort = Host + ":" + Port.ToString());

        protected override TimeSpan BackoffDuration => TimeSpan.FromSeconds(5);

        public override string NodeType => "MongoDB";
        public override int MinSecondsBetweenPolls => 5;

        private readonly object _connectionLock = new object();
        private MongoConnectedClient _connection;
        public MongoConnectedClient Connection
        {
            get
            {
                if (_connection == null || !IsConnected())
                {
                    _connection = new MongoConnectedClient();
                    _connection.MongoClient = GetConnection();
                }
                return _connection;
            }
        }
        private bool IsConnected()
        {
            var mc = _connection.MongoClient;
            if (mc == null) return false;

            var db = mc.GetDatabase("admin");
            var doc = db.RunCommand<BsonDocument>(" { ping : 1 }");

            return doc["ok"] == 1D;
        }

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return Info;
                yield return Clients;
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
            if (Role == MongoDBInfo.MongoDBInstanceRole.Unknown) yield return MonitorStatus.Critical;
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
            if (Role == MongoDBInfo.MongoDBInstanceRole.Unknown) return "Unknown role";
            if (Replication == null) return "Replication Unknown";
            if (IsSlave && Replication.MasterLinkStatus != "up") return "Master link down";
            if (IsMaster && Replication.SlaveConnections.Any(s => s.Status != "online")) return "Slave offline";
            return null;
        }

        public MongoDBInstance(MongoDBConnectionInfo connectionInfo) : base(connectionInfo.Host + ":" + connectionInfo.Port.ToString())
        {
            ConnectionInfo = connectionInfo;
            ShortHost = Host.Split(StringSplits.Period).First();
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
            //TODO: MongoDB instance search
            return AppCache.GetHostName(hostOrIp);
        }

        private IMongoClient GetConnection(bool allowAdmin = false, int syncTimeout = 10000)
        {
            var config = new MongoClientSettings();
            config.Server = new MongoServerAddress(Host, Port);

            return new MongoClient(config);
        }

        public Func<Cache<T>, Task> GetFromMongoDBAsync<T>(string opName, Func<MongoConnectedClient, Task<T>> getFromConnection) where T : class
        {
            return UpdateCacheItem(description: "MongoDB Fetch: " + Name + ":" + opName,
                                   getData: () => getFromConnection(Connection),
                                   logExceptions: false,
                                   addExceptionData: e => e.AddLoggedData("Server", Name)
                                                           .AddLoggedData("Host", Host)
                                                           .AddLoggedData("Port", Port.ToString()),
                                   timeoutMs: 10000);
        }

        public bool Equals(MongoDBInstance other)
        {
            if (other == null) return false;
            return Host == other.Host && Port == other.Port;
        }

        public override string ToString() => HostAndPort;

        public MongoDBMemoryAnalysis GetDatabaseMemoryAnalysis(string database, bool runIfMaster = false)
        {
            var ci = ConnectionInfo;
            if (IsMaster && !runIfMaster)
            {
                // no slaves, and a master - boom
                if (SlaveCount == 0)
                    return new MongoDBMemoryAnalysis(ConnectionInfo, database)
                    {
                        ErrorMessage = "Cannot run memory analysis on a master - it hurts."
                    };

                // Go to the first slave, automagically
                ci = SlaveInstances.First().ConnectionInfo;
            }

            return MongoDBAnalyzer.AnalyzeDatabaseMemory(ci, database);
        }

        public void ClearDatabaseMemoryAnalysisCache(string database)
        {
            MongoDBAnalyzer.ClearDatabaseMemoryAnalysisCache(ConnectionInfo, database);
        }

        //static MongoDBInstance()
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
