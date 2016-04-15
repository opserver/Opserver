using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Jil;
using MongoDB.Bson;

namespace StackExchange.Opserver.Data.MongoDB
{
    public partial class MongoDBInfo
    {
        //public BsonDocument FullInfoBson { get; internal set; }
        public List<MongoDBInfoSection> UnrecognizedSections { get; internal set; }

        public IEnumerable<MongoDBInfoSection> Sections
        {
            get
            {
                yield return Replication;
                yield return Network;
                yield return Clients;
                yield return Server;
                yield return Mem;
                yield return Connections;
                yield return Stats;
                yield return Databases;
                if (UnrecognizedSections != null)
                    foreach (var s in UnrecognizedSections)
                        yield return s;
                // key space
            }
        }

        public enum MongoDBInstanceRole
        {
            Master,
            Slave,
            Unknown
        }

        public ReplicationInfo Replication { get; internal set; } = new ReplicationInfo();
        public class ReplicationInfo : MongoDBInfoSection
        {
            public MongoDBInstanceRole MongoDBInstanceRole
            {
                get
                {
                    if (IsMaster) return MongoDBInstanceRole.Master;
                    return MongoDBInstanceRole.Slave;
                }
            }

            [MongoDBInfoProperty("ismaster")]
            public bool IsMaster { get; internal set; }
            [MongoDBInfoProperty("master_host")]
            public string MasterHost { get; internal set; }
            [MongoDBInfoProperty("master_port")]
            public int MasterPort { get; internal set; }
            [MongoDBInfoProperty("master_link_status")]
            public string MasterLinkStatus { get; internal set; }
            [MongoDBInfoProperty("master_last_io_seconds_ago")]
            public int MasterLastIOSecondsAgo { get; internal set; }
            [MongoDBInfoProperty("master_sync_in_progress")]
            public string MasterSyncInProgress { get; internal set; }
            [MongoDBInfoProperty("master_sync_left_bytes")]
            public long MastSyncLeftBytes { get; internal set; }
            [MongoDBInfoProperty("master_sync_last_io_seconds_ago")]
            public int MasterSyncLastIOSecondsAgo { get; internal set; }
            [MongoDBInfoProperty("min_slaves_good_slaves")]
            public int MinSlavesGoodSlaves { get; internal set; }
            [MongoDBInfoProperty("slave_priority")]
            public string SlavePriority { get; internal set; }
            [MongoDBInfoProperty("connected_slaves")]
            public int ConnectedSlaves { get; internal set; }

            [MongoDBInfoProperty("master_repl_offset")]
            public long MasterReplicationOffset { get; internal set; }
            [MongoDBInfoProperty("slave_repl_offset")]
            public long SlaveReplicationOffset { get; internal set; }

            [MongoDBInfoProperty("repl_backlog_active")]
            public bool BacklogActive { get; internal set; }
            [MongoDBInfoProperty("repl_backlog_size")]
            public long BacklogSize { get; internal set; }
            [MongoDBInfoProperty("repl_backlog_first_byte_offset")]
            public long BacklogFirstByteOffset { get; internal set; }
            [MongoDBInfoProperty("repl_backlog_histlen")]
            public long BacklogHistoryLength { get; internal set; }


            public List<MongoDBSlaveInfo> SlaveConnections = new List<MongoDBSlaveInfo>();
        }
        public class MongoDBSlaveInfo
        {
            public int Index { get; internal set; }
            public string IP { get; internal set; }
            public int Port { get; internal set; }
            public string Status { get; internal set; }
            public long Offset { get; internal set; }

            private IPAddress _ipAddress;
            public IPAddress IPAddress => _ipAddress ?? (_ipAddress = IPAddress.Parse(IP));

            public MongoDBInstance GetServer()
            {
                return MongoDBInstance.GetInstance(Port, IPAddress);
            }
        }

        public NetworkInfo Network { get; internal set; } = new NetworkInfo();
        public class NetworkInfo : MongoDBInfoSection
        {
            [MongoDBInfoProperty("bytesIn")]
            public long Rx { get; internal set; }

            [MongoDBInfoProperty("bytesOut")]
            public long Tx { get; internal set; }

            [MongoDBInfoProperty("numRequests")]
            public long Requests { get; internal set; }
        }

        public ClientInfo Clients { get; internal set; } = new ClientInfo();
        public class ClientInfo : MongoDBInfoSection
        {
            [MongoDBInfoProperty("connected_clients")]
            public int Connected { get; internal set; }

            [MongoDBInfoProperty("blocked_clients")]
            public int Blocked { get; internal set; }

            [MongoDBInfoProperty("client_longest_output_list")]
            public long LongestOutputList { get; internal set; }

            [MongoDBInfoProperty("client_biggest_input_buf")]
            public long BiggestInputBuffer { get; internal set; }
        }

        public ServerInfo Server { get; internal set; } = new ServerInfo();
        public class ServerInfo : MongoDBInfoSection
        {
            private Version _version;
            public Version Version => _version ?? (_version = VersionNumber.HasValue() ? Version.Parse(VersionNumber) : new Version());

            [MongoDBInfoProperty("version")]
            public string VersionNumber { get; internal set; }

            [MongoDBInfoProperty("uptime")]
            public int UptimeInSeconds { get; internal set; }

            [MongoDBInfoProperty("writeBacksQueued")]
            public bool WriteBacksQueued { get; internal set; }
        }

        public MemInfo Mem { get; internal set; } = new MemInfo();
        public class MemInfo : MongoDBInfoSection
        {
            [MongoDBInfoProperty("bits")]
            public int PlatformVersion { get; internal set; }

            [MongoDBInfoProperty("resident")]
            public long Used { get; internal set; }

            [MongoDBInfoProperty("virtual")]
            public long Virtual { get; internal set; }

            [MongoDBInfoProperty("supported")]
            public bool ExtendedSupport { get; internal set; }

            [MongoDBInfoProperty("mapped")]
            public long Mapped { get; internal set; }

            [MongoDBInfoProperty("mappedWithJournal")]
            public long MappedWithJournal { get; internal set; }

            [MongoDBInfoProperty("note")]
            public string Note { get; internal set; }
        }

        public ConnectionsInfo Connections { get; internal set; } = new ConnectionsInfo();
        public class ConnectionsInfo : MongoDBInfoSection
        {
            [MongoDBInfoProperty("current")]
            public long Current { get; internal set; }

            [MongoDBInfoProperty("available")]
            public long Available { get; internal set; }

            [MongoDBInfoProperty("totalCreated")]
            public long TotalCreated { get; internal set; }
        }

        public StatsInfo Stats { get; internal set; } = new StatsInfo();
        public class StatsInfo : MongoDBInfoSection
        {
            [MongoDBInfoProperty("total_connections_received")]
            public long TotalConnectionsReceived { get; internal set; }

            [MongoDBInfoProperty("total_commands_processed")]
            public long TotalCommandsProcessed { get; internal set; }

            [MongoDBInfoProperty("instantaneous_ops_per_sec")]
            public int InstantaneousOpsPerSec { get; internal set; }

            [MongoDBInfoProperty("rejected_connections")]
            public long RejectedConnections { get; internal set; }

            [MongoDBInfoProperty("expired_keys")]
            public long ExpiredKeys { get; internal set; }

            [MongoDBInfoProperty("evicted_keys")]
            public long EvictedKeys { get; internal set; }

            [MongoDBInfoProperty("keyspace_hits")]
            public long KeyspaceHits { get; internal set; }

            [MongoDBInfoProperty("keyspace_misses")]
            public long KeyspaceMisses { get; internal set; }

            [MongoDBInfoProperty("pubsub_channels")]
            public long PubSubChannels { get; internal set; }

            [MongoDBInfoProperty("pubsub_patterns")]
            public long PubSubPatterns { get; internal set; }

            [MongoDBInfoProperty("latest_fork_usec")]
            public long LatestForkMicroSeconds { get; internal set; }
        }

        public DatabasesInfo Databases { get; internal set; } = new DatabasesInfo();
        public class DatabasesInfo : MongoDBInfoSection
        {
            [MongoDBInfoProperty("totalSize")]
            public double TotalSizeOnDisk { get; internal set; }

            [MongoDBInfoProperty("totalSizeMb")]
            public double TotalSize { get; internal set; }

            public List<MongoDBDatabase> Databases = new List<MongoDBDatabase>();

            internal override void AddLine(string key, string value)
            {
                if (key == "databases")
                {
                    var list = JSON.Deserialize<MongoDBDatabase[]>(value);
                    value = "";

                    foreach (var mongoDBDatabase in list)
                    {
                        this.Databases.Add(mongoDBDatabase);
                        value += $"{mongoDBDatabase.Name} , {mongoDBDatabase.SizeOnDisk} size";
                    }
                }
                base.AddLine(key, value);
            }
        }
    }

    public class MongoDBDatabase
    {
        [JilDirective(Name = "name")]
        public string Name { get; internal set; }

        [JilDirective(Name = "sizeOnDisk")]
        public double SizeOnDisk { get; internal set; }

        [JilDirective(Name = "empty")]
        public bool IsEmpty { get; internal set; }
    }
}
