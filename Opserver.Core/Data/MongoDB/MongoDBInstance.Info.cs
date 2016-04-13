using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using StackExchange.Profiling;
using MongoDB.Driver.Core.Servers;

namespace StackExchange.Opserver.Data.MongoDB
{
    public static class ServerUtils
    {
        public static MongoConnectedClient GetSingleServer(this MongoConnectedClient connection)
        {
            return connection;
        }
    }

    public partial class MongoDBInstance
    {
        //Specially caching this since it's accessed so often
        public MongoDBInfo.ReplicationInfo Replication { get; private set; }

        private Cache<MongoDBInfo> _info;
        public Cache<MongoDBInfo> Info
        {
            get
            {
                return _info ?? (_info = new Cache<MongoDBInfo>
                {
                    CacheForSeconds = 10,
                    UpdateCache = GetFromMongoDBAsync(nameof(Info), async rc =>
                    {
                        var server = rc.GetSingleServer().MongoClient;

                        var db = server.GetDatabase("admin");

                        // server status
                        var commandServer = new BsonDocumentCommand<BsonDocument>(new BsonDocument { {"serverStatus", 1} });
                        var serverStatus = await db.RunCommandAsync(commandServer);

                        // master / replica
                        var commandMaster = new BsonDocumentCommand<BsonDocument>(new BsonDocument { {"isMaster", 1} });
                        var masterStatus = await db.RunCommandAsync(commandMaster);

                        // database list
                        var commandDatabase = new BsonDocumentCommand<BsonDocument>(new BsonDocument { {"listDatabases", 1} });
                        var databaseList = await db.RunCommandAsync(commandDatabase);

                        var ri = MongoDBInfo.FromInfo(serverStatus, masterStatus, databaseList);
                        if (ri != null) Replication = ri.Replication;
                        return ri;
                    })
                });
            }
        }

        public MongoDBInfo.MongoDBInstanceRole Role
        {
            get
            {
                var lastRole = Replication?.MongoDBInstanceRole;
                // If we think we're a master and the last poll failed - look to other nodes for info
                if (!Info.LastPollSuccessful && lastRole == MongoDBInfo.MongoDBInstanceRole.Master &&
                    AllInstances.Any(r => r.SlaveInstances.Any(s => s == this)))
                    return MongoDBInfo.MongoDBInstanceRole.Slave;
                return lastRole ?? MongoDBInfo.MongoDBInstanceRole.Unknown;
            }
        }

        public string RoleDescription
        {
            get
            {
                switch (Role)
                {
                    case MongoDBInfo.MongoDBInstanceRole.Master:
                        return "Master";
                    case MongoDBInfo.MongoDBInstanceRole.Slave:
                        return "Slave";
                    default:
                        return "Unknown";
                }
            }
        }

        public bool IsMaster => Role == MongoDBInfo.MongoDBInstanceRole.Master;
        public bool IsSlave => Role == MongoDBInfo.MongoDBInstanceRole.Slave;
        public bool IsSlaving => IsSlave && (Replication.MasterLinkStatus != "up" || Info.Data?.Replication?.MastSyncLeftBytes > 0);

        public MongoDBInstance TopMaster
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

        public MongoDBInstance Master
        {
            get { return Replication?.MasterHost.HasValue() == true ? GetInstance(Replication.MasterHost, Replication.MasterPort) : AllInstances.FirstOrDefault(i => i.SlaveInstances.Contains(this)); }
        }

        public int SlaveCount => Replication?.ConnectedSlaves ?? 0;

        public int TotalSlaveCount
        {
            get { return SlaveCount + (SlaveCount > 0 && SlaveInstances != null ? SlaveInstances.Sum(s => s?.TotalSlaveCount ?? 0) : 0); }
        }

        public List<MongoDBInfo.MongoDBSlaveInfo> SlaveConnections => Replication?.SlaveConnections;

        public List<MongoDBInstance> SlaveInstances
        {
            get
            {
                return Info.LastPollSuccessful ? (Replication?.SlaveConnections.Select(s => s.GetServer()).ToList() ?? new List<MongoDBInstance>()) : new List<MongoDBInstance>();
                // If we can't poll this server, ONLY trust the other nodes we can poll
                //return AllInstances.Where(i => i.Master == this).ToList();
            }
        }

        public List<MongoDBInstance> GetAllSlavesInChain()
        {
            var slaves = SlaveInstances;
            return slaves.Union(slaves.SelectMany(i => i?.GetAllSlavesInChain() ?? Enumerable.Empty<MongoDBInstance>())).Distinct().ToList();
        }

        public class MongoDBInfoSection
        {
            public bool IsGlobal { get; internal set; }

            public string Title { get; internal set; }

            public List<MongoDBInfoLine> Lines { get; internal set; }
        }

        public class MongoDBInfoLine
        {
            public bool Important { get; internal set; }
            public string Key { get; internal set; }
            public string ParsedValue { get; internal set; }
            public string OriginalValue { get; internal set; }
        }
    }
}
