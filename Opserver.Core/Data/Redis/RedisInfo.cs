using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisInfo
    {
        public string FullInfoString { get; internal set; }
        public List<RedisInfoSection> UnrecognizedSections { get; internal set; }
        public IEnumerable<RedisInfoSection> Sections
        {
            get
            {
                yield return Replication;
                yield return Clients;
                yield return Server;
                yield return Memory;
                yield return Persistence;
                yield return Stats;
                yield return CPU;
                yield return Keyspace;
                if (UnrecognizedSections != null)
                    foreach (var s in UnrecognizedSections)
                        yield return s;
                // key space
            }
        }

        public RedisInfo()
        {
            Replication = new ReplicationInfo();
            Clients = new ClientInfo();
            Server = new ServerInfo();
            Memory = new MemoryInfo();
            Persistence = new PersistenceInfo();
            Stats = new StatsInfo();
            CPU = new CPUInfo();
            Keyspace = new KeyspaceInfo();
        }

        public enum RedisInstanceRole
        {
            Master,
            Slave,
            Unknown
        }

        public ReplicationInfo Replication { get; internal set; }
        public class ReplicationInfo : RedisInfoSection
        {
            public RedisInstanceRole RedisInstanceRole
            {
                get
                {
                   switch (Role)
                   {
                       case "master": return RedisInstanceRole.Master;
                       case "slave": return RedisInstanceRole.Slave;
                       default: return RedisInstanceRole.Unknown;
                   } 
                }
            }

            [RedisInfoProperty("role")]
            public string Role { get; internal set; }
            [RedisInfoProperty("master_host")]
            public string MasterHost { get; internal set; }
            [RedisInfoProperty("master_port")]
            public int MasterPort { get; internal set; }
            [RedisInfoProperty("master_link_status")]
            public string MasterLinkStatus { get; internal set; }
            [RedisInfoProperty("master_last_io_seconds_ago")]
            public int MasterLastIOSecondsAgo { get; internal set; }
            [RedisInfoProperty("master_sync_in_progress")]
            public string MasterSyncInProgress { get; internal set; }
            [RedisInfoProperty("master_sync_left_bytes")]
            public long MastSyncLeftBytes { get; internal set; }
            [RedisInfoProperty("master_sync_last_io_seconds_ago")]
            public int MasterSyncLastIOSecondsAgo { get; internal set; }
            [RedisInfoProperty("min_slaves_good_slaves")]
            public int MinSlavesGoodSlaves { get; internal set; }
            [RedisInfoProperty("slave_priority")]
            public string SlavePriority { get; internal set; }
            [RedisInfoProperty("connected_slaves")]
            public int ConnectedSlaves { get; internal set; }

            [RedisInfoProperty("master_repl_offset")]
            public long MasterReplicationOffset { get; internal set; }
            [RedisInfoProperty("slave_repl_offset")]
            public long SlaveReplicationOffset { get; internal set; }      

            [RedisInfoProperty("repl_backlog_active")]
            public bool BacklogActive { get; internal set; }
            [RedisInfoProperty("repl_backlog_size")]
            public long BacklogSize { get; internal set; }
            [RedisInfoProperty("repl_backlog_first_byte_offset")]
            public long BacklogFirstByteOffset { get; internal set; }
            [RedisInfoProperty("repl_backlog_histlen")]
            public long BacklogHistoryLength { get; internal set; }
            

            public List<RedisSlaveInfo> SlaveConnections = new List<RedisSlaveInfo>();
            private static readonly Regex _slaveRegex = new Regex(@"slave\d+", RegexOptions.Compiled);

            public override void MapUnrecognizedLine(string key, string value)
            {
                if (_slaveRegex.IsMatch(key) && value.HasValue())
                {
                    var parts = value.Split(StringSplits.Comma);
                    if (parts.Length == 3)
                    {
                        SlaveConnections.Add(new RedisSlaveInfo
                            {
                                Index = int.Parse(key.Replace("slave", "")),
                                IP = parts[0],
                                Port = int.Parse(parts[1]),
                                Status = parts[2]
                            });
                    }
                    // redis 2.8+
                    if (parts.Length > 3)
                    {
                        try
                        {
                            var si = new RedisSlaveInfo { Index = int.Parse(key.Replace("slave", "")) };
                            foreach(var p in parts) {
                                var pair = p.Split(StringSplits.Equal);
                                if (pair.Length != 2) continue;
                                var val = pair[1];
                                
                                switch(pair[0]) {
                                    case "ip":
                                        si.IP = val;
                                        break;
                                    case "port":
                                        si.Port = int.Parse(val);
                                        break;
                                    case "state":
                                        si.Status = val;
                                        break;
                                    case "offset":
                                        si.Offset = long.Parse(val);
                                        break;
                                }
                            }
                            SlaveConnections.Add(si);
                        } catch (Exception e) {
                            Current.LogException("Redis error: couldn't parse slave string: " + value, e);
                        }
                    }
                }
            }
        }

        public class RedisSlaveInfo
        {
            public int Index { get; internal set; }
            public string IP { get; internal set; }
            public int Port { get; internal set; }
            public string Status { get; internal set; }
            public long Offset { get; internal set; }

            private IPAddress _ipAddress;
            public IPAddress IPAddress => _ipAddress ?? (_ipAddress = IPAddress.Parse(IP));

            public RedisInstance GetServer()
            {
                return RedisInstance.GetInstance(Port, IPAddress);
            }
        }


        public ClientInfo Clients { get; internal set; }
        public class ClientInfo : RedisInfoSection
        {
            [RedisInfoProperty("connected_clients")]
            public int Connected { get; internal set; }
            [RedisInfoProperty("blocked_clients")]
            public int Blocked { get; internal set; }
            [RedisInfoProperty("client_longest_output_list")]
            public long LongestOutputList { get; internal set; }
            [RedisInfoProperty("client_biggest_input_buf")]
            public long BiggestInputBuffer { get; internal set; }
        }


        public ServerInfo Server { get; internal set; }
        public class ServerInfo : RedisInfoSection
        {
            private Version _version;
            public Version Version => _version ?? (_version = VersionNumber.HasValue() ? Version.Parse(VersionNumber) : new Version());

            [RedisInfoProperty("redis_version")]
            public string VersionNumber { get; internal set; }
            [RedisInfoProperty("redis_git_sha1")]
            public string GitSHA1 { get; internal set; }
            [RedisInfoProperty("redis_git_dirty")]
            public bool GitDirty { get; internal set; }
            [RedisInfoProperty("redis_mode")]
            public string RedisMode { get; internal set; }
            [RedisInfoProperty("os")]
            public string OS { get; internal set; }
            [RedisInfoProperty("arch_bits")]
            public string ArchitectureBits { get; internal set; }
            [RedisInfoProperty("multiplexing_api")]
            public string MultiplexingAPI { get; internal set; }
            [RedisInfoProperty("gcc_version")]
            public string GCCVersion { get; internal set; }
            [RedisInfoProperty("process_id")]
            public int ProcessID { get; internal set; }
            [RedisInfoProperty("run_id")]
            public string RunID { get; internal set; }
            [RedisInfoProperty("tcp_port")]
            public int TCPPort { get; internal set; }
            [RedisInfoProperty("uptime_in_seconds")]
            public long UptimeInSeconds { get; internal set; }
            [RedisInfoProperty("uptime_in_days")]
            public long UptimeInDays { get; internal set; }
            [RedisInfoProperty("hz")]
            public int Hz { get; internal set; }
            [RedisInfoProperty("lru_clock")]
            public int LRUClock { get; internal set; }
        }


        public MemoryInfo Memory { get; internal set; }
        public class MemoryInfo : RedisInfoSection
        {
            [RedisInfoProperty("used_memory")]
            public long UsedMemory { get; internal set; }
            [RedisInfoProperty("used_memory_human")]
            public string UsedMemoryHuman { get; internal set; }
            [RedisInfoProperty("used_memory_rss")]
            public long UsedMemoryRSS { get; internal set; }
            [RedisInfoProperty("used_memory_peak")]
            public long UsedMemoryPeak { get; internal set; }
            [RedisInfoProperty("used_memory_peak_human")]
            public string UsedMemoryPeakHuman { get; internal set; }
            [RedisInfoProperty("mem_fragmentation_ratio")]
            public double MemoryFragmentationRatio { get; internal set; }
            [RedisInfoProperty("used_memory_lua")]
            public long UsedMemoryLua { get; internal set; }
            [RedisInfoProperty("mem_allocator")]
            public string MemoryAllocator { get; internal set; }
        }


        public PersistenceInfo Persistence { get; internal set; }
        public class PersistenceInfo : RedisInfoSection
        {
            [RedisInfoProperty("loading")]
            public bool Loading { get; internal set; }
            [RedisInfoProperty("rdb_changes_since_last_save")]
            public long RDBChangesSinceLastSave { get; internal set; }
            [RedisInfoProperty("rdb_bgsave_in_progress")]
            public bool RDBBGSaveInProgress { get; internal set; }
            [RedisInfoProperty("rdb_last_save_time")]
            public long RDBLastSaveTime { get; internal set; }
            [RedisInfoProperty("rdb_last_bgsave_status")]
            public string RDBLastBGSaveStatus { get; internal set; }
            [RedisInfoProperty("rdb_last_bgsave_time_sec")]
            public long RDBLastBGSaveTimeSeconds { get; internal set; }
            [RedisInfoProperty("rdb_current_bgsave_time_sec")]
            public long RDBCurrentBGSaveTimeSeconds { get; internal set; }

            [RedisInfoProperty("aof_enabled")]
            public bool AOFEnabled { get; internal set; }
            [RedisInfoProperty("aof_rewrite_in_progress")]
            public bool AOFRewriteInProgress { get; internal set; }
            [RedisInfoProperty("aof_rewrite_scheduled")]
            public bool AOFRewriteScheduled { get; internal set; }
            [RedisInfoProperty("aof_last_rewrite_time_sec")]
            public long AOFLastRewriteTimeSeconds { get; internal set; }
            [RedisInfoProperty("aof_current_rewrite_time_sec")]
            public long AOFCurrentRewriteTimeSeconds { get; internal set; }
            [RedisInfoProperty("aof_last_bgrewrite_status")]
            public string AOFLastBGRewriteStatus { get; internal set; }
            [RedisInfoProperty("aof_current_size")]
            public long AOFCurrentSize { get; internal set; }
            [RedisInfoProperty("aof_base_size")]
            public long AOFBaseSize { get; internal set; }
            [RedisInfoProperty("aof_pending_rewrite")]
            public bool AOFPendingRewrite { get; internal set; }
            [RedisInfoProperty("aof_buffer_length")]
            public long AOFBuggerLength { get; internal set; }
            [RedisInfoProperty("aof_rewrite_buffer_length")]
            public long AOFRewriteBufferLength { get; internal set; }
            [RedisInfoProperty("aof_pending_bio_fsync")]
            public bool AOFPendingBackgroundIOFsync { get; internal set; }
            [RedisInfoProperty("aof_delayed_fsync")]
            public long AOFDelayedFSync { get; internal set; }
        }


        public StatsInfo Stats { get; internal set; }
        public class StatsInfo : RedisInfoSection
        {
            [RedisInfoProperty("total_connections_received")]
            public long TotalConnectionsReceived { get; internal set; }
            [RedisInfoProperty("total_commands_processed")]
            public long TotalCommandsProcessed { get; internal set; }
            [RedisInfoProperty("instantaneous_ops_per_sec")]
            public int InstantaneousOpsPerSec { get; internal set; }
            [RedisInfoProperty("rejected_connections")]
            public long RejectedConnections { get; internal set; }

            [RedisInfoProperty("expired_keys")]
            public long ExpiredKeys { get; internal set; }
            [RedisInfoProperty("evicted_keys")]
            public long EvictedKeys { get; internal set; }
            [RedisInfoProperty("keyspace_hits")]
            public long KeyspaceHits { get; internal set; }
            [RedisInfoProperty("keyspace_misses")]
            public long KeyspaceMisses { get; internal set; }
            [RedisInfoProperty("pubsub_channels")]
            public long PubSubChannels { get; internal set; }
            [RedisInfoProperty("pubsub_patterns")]
            public long PubSubPatterns { get; internal set; }
            [RedisInfoProperty("latest_fork_usec")]
            public long LatestForkMicroSeconds { get; internal set; }
        }


        public CPUInfo CPU { get; internal set; }
        public class CPUInfo : RedisInfoSection
        {
            [RedisInfoProperty("used_cpu_sys")]
            public double UsedCPUSystem { get; internal set; }
            [RedisInfoProperty("used_cpu_user")]
            public double UsedCPUUser { get; internal set; }
            [RedisInfoProperty("used_cpu_sys_children")]
            public double UsedCPUSystemChildren { get; internal set; }
            [RedisInfoProperty("used_cpu_user_children")]
            public double UsedCPUUserChildren { get; internal set; }
        }


        public KeyspaceInfo Keyspace { get; internal set; }
        public class KeyspaceInfo : RedisInfoSection
        {   
            public Dictionary<int, KeyData> KeyData = new Dictionary<int, KeyData>();

            private static readonly Regex _dbNameMatch = new Regex(@"db([0-9]+)", RegexOptions.Compiled);
            private static readonly Regex _keysMatch = new Regex(@"keys=([0-9]+),expires=([0-9]+)", RegexOptions.Compiled);
            internal override void AddLine(string key, string value)
            {
                var dbMatch = _dbNameMatch.Match(key);
                if (dbMatch.Success)
                {
                    var keysMatch = _keysMatch.Match(value);
                    if (keysMatch.Success)
                    {
                        try
                        {
                            var kd = new KeyData
                                {
                                    Keys = long.Parse(keysMatch.Groups[1].Value),
                                    Expires = long.Parse(keysMatch.Groups[2].Value)
                                };
                            KeyData[int.Parse(dbMatch.Groups[1].Value)] = kd;
                            value = $"{kd.Keys.ToComma()} keys, {kd.Expires.ToComma()} expires";
                        }
                        catch (Exception e)
                        {
                            var ex = new Exception("Error Pasing " + key + ":" + value + " from redis INFO - Parsed Keys=" + keysMatch.Groups[1].Value + ", Expires=" + keysMatch.Groups[2].Value + ".", e);
                            Current.LogException(ex);
                        }  
                    }
                }
                base.AddLine(key, value);
            }
        }
    }

    public class KeyData
    {
        public long Keys { get; internal set; }
        public long Expires { get; internal set; }
    }
}
