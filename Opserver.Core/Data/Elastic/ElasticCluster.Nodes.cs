using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<ClusterNodesInfo> _nodes;
        public Cache<ClusterNodesInfo> Nodes => _nodes ?? (_nodes = new Cache<ClusterNodesInfo>
        {
            CacheForSeconds = RefreshInterval,
            UpdateCache = UpdateFromElastic(nameof(Nodes), async () =>
            {
                var result = (await GetAsync<ClusterNodesInfo>("_nodes").ConfigureAwait(false))?.Prep();
                if (result == null) return null;

                var stats = await GetAsync<ClusterNodesStats>("_nodes/stats?all").ConfigureAwait(false);
                if (stats == null) return result;

                foreach (var s in stats.Nodes)
                {
                    var node = result.Nodes.FirstOrDefault(n => n.GUID == s.Key);
                    if (node != null)
                    {
                        node.Stats = s.Value;
                    }
                }
                return result;
            })
        });

        public string Name => Nodes.HasData() ? Nodes.Data.Name : SettingsName;
        public string ShortName(NodeInfo node) => node?.Name.TrimEnd("-" + Name);

        public class ClusterNodesInfo
        {
            public List<NodeInfo> Nodes { get; private set; }

            public ClusterNodesInfo Prep()
            {
                if (RawNodes != null)
                {
                    foreach (var n in RawNodes)
                    {
                        n.Value.GUID = n.Key;
                        n.Value.ShortName = n.Value.Name.Replace("-" + Name, "");
                    }
                }
                Nodes = RawNodes?.Values.OrderBy(n => n.Name).ToList() ?? new List<NodeInfo>();
                return this;
            }
            public NodeInfo Get(string nameOrGuid)
            {
                return Nodes.FirstOrDefault(
                    n => string.Equals(n.Name, nameOrGuid, StringComparison.InvariantCultureIgnoreCase)
                         || string.Equals(n.GUID, nameOrGuid, StringComparison.InvariantCultureIgnoreCase));
            }

            [DataMember(Name = "cluster_name")]
            public string Name { get; internal set; }
            [DataMember(Name = "nodes")]
            public Dictionary<string, NodeInfo> RawNodes { get; internal set; }
        }

        public class NodeInfo : IMonitorStatus
        {
            // TODO: Implement
            public MonitorStatus MonitorStatus => MonitorStatus.Good;
            public string MonitorStatusReason => null;

            private Version _version;
            [IgnoreDataMember]
            public Version Version => _version ?? (_version = VersionString.HasValue() ? Version.Parse(VersionString) : new Version("0.0"));

            public bool IsClient => GetAttribute("client") == "true";
            public bool IsDataNode => GetAttribute("data") == "true";

            private string GetAttribute(string key)
            {
                string val;
                return Attributes != null && Attributes.TryGetValue(key, out val) ? val : null;
            }

            public string GUID { get; internal set; }
            public string ShortName { get; internal set; }
            public ClusterNodesStats.NodeStats Stats { get; internal set; }

            [DataMember(Name = "name")]
            public string Name { get; internal set; }
            //[DataMember(Name = "transport_address")]
            //public string TransportAddress { get; internal set; }
            [DataMember(Name = "host")]
            public string Hostname { get; internal set; }
            [DataMember(Name = "version")]
            public string VersionString { get; internal set; }
            //[DataMember(Name = "build")]
            //public string Build { get; internal set; }
            [DataMember(Name = "attributes")]
            public Dictionary<string, string> Attributes { get; internal set; }
            //[DataMember(Name = "http_address")]
            //public string HttpAddress { get; internal set; }
            [DataMember(Name = "settings")]
            public Dictionary<string, dynamic> Settings { get; internal set; }
            //[DataMember(Name = "os")]
            //public OSInfo OS { get; internal set; }
            //[DataMember(Name = "process")]
            //public ProcessInfo Process { get; internal set; }
            [DataMember(Name = "jvm")]
            public JVMInfo JVM { get; internal set; }
            //[DataMember(Name = "thread_pool")]
            //public Dictionary<string, ThreadPoolThreadInfo> ThreadPool { get; internal set; }
            [DataMember(Name = "network")]
            public NetworkInfo Network { get; internal set; }
            //[DataMember(Name = "transport")]
            //public TransportInfo Transport { get; internal set; }
            [DataMember(Name = "http")]
            public HTTPInfo HTTP { get; internal set; }

            public class OSInfo
            {
                [DataMember(Name = "refresh_interval")]
                public int RefreshInterval { get; internal set; }
                [DataMember(Name = "available_processors")]
                public int AvailableProcessors { get; internal set; }
                [DataMember(Name = "cpu")]
                public OSCPUInfo Cpu { get; internal set; }
                [DataMember(Name = "mem")]
                public MemoryInfo Mem { get; internal set; }
                [DataMember(Name = "swap")]
                public MemoryInfo Swap { get; internal set; }

                public class OSCPUInfo
                {
                    [DataMember(Name = "vendor")]
                    public string Vendor { get; internal set; }
                    [DataMember(Name = "model")]
                    public string Model { get; internal set; }
                    [DataMember(Name = "mhz")]
                    public int Mhz { get; internal set; }
                    [DataMember(Name = "total_cores")]
                    public int TotalCores { get; internal set; }
                    [DataMember(Name = "total_sockets")]
                    public int TotalSockets { get; internal set; }
                    [DataMember(Name = "cores_per_socket")]
                    public int CoresPerSocket { get; internal set; }
                    [DataMember(Name = "cache_size")]
                    public string CacheSize { get; internal set; }
                    [DataMember(Name = "cache_size_in_bytes")]
                    public int CacheSizeInBytes { get; internal set; }
                }

                public class MemoryInfo
                {
                    [DataMember(Name = "total")]
                    public string Total { get; internal set; }
                    [DataMember(Name = "total_in_bytes")]
                    public long TotalInBytes { get; internal set; }
                }
            }

            public class ProcessInfo
            {
                [DataMember(Name = "refresh_interval")]
                public int RefreshInterval { get; internal set; }
                [DataMember(Name = "id")]
                public long Id { get; internal set; }
                [DataMember(Name = "max_file_descriptors")]
                public int MaxFileDescriptors { get; internal set; }
            }

            public class JVMInfo
            {
                [DataMember(Name = "pid")]
                public int PID { get; internal set; }
                [DataMember(Name = "version")]
                public string Version { get; internal set; }
                [DataMember(Name = "vm_name")]
                public string VMName { get; internal set; }
                [DataMember(Name = "vm_version")]
                public string VMVersion { get; internal set; }
                [DataMember(Name = "vm_vendor")]
                public string VMVendor { get; internal set; }
                [DataMember(Name = "start_time")]
                public long StartTime { get; internal set; }
                [DataMember(Name = "mem")]
                public JVMMemoryInfo Memory { get; internal set; }
                public class JVMMemoryInfo
                {
                    [DataMember(Name = "heap_init")]
                    public string HeapInit { get; internal set; }
                    [DataMember(Name = "heap_init_in_bytes")]
                    public long HeapInitInBytes { get; internal set; }
                    [DataMember(Name = "heap_max")]
                    public string HeapMax { get; internal set; }
                    [DataMember(Name = "heap_max_in_bytes")]
                    public long HeapMaxInBytes { get; internal set; }
                    [DataMember(Name = "non_heap_init")]
                    public string NonHeapInit { get; internal set; }
                    [DataMember(Name = "non_heap_init_in_bytes")]
                    public long NonHeapInitInBytes { get; internal set; }
                    [DataMember(Name = "non_heap_max")]
                    public string NonHeapMax { get; internal set; }
                    [DataMember(Name = "non_heap_max_in_bytes")]
                    public long NonHeapMaxInBytes { get; internal set; }
                    [DataMember(Name = "direct_max")]
                    public string DirectMax { get; internal set; }
                    [DataMember(Name = "direct_max_in_bytes")]
                    public long DirectMaxInBytes { get; internal set; }
                }
            }

            public class ThreadPoolThreadInfo
            {
                [DataMember(Name = "type")]
                public string Type { get; internal set; }
                [DataMember(Name = "min")]
                public int? Min { get; internal set; }
                [DataMember(Name = "max")]
                public int? Max { get; internal set; }
                [DataMember(Name = "keep_alive")]
                public string KeepAlive { get; internal set; }
            }

            public class NetworkInfo
            {
                [DataMember(Name = "refresh_interval")]
                public int RefreshInterval { get; internal set; }
                [DataMember(Name = "primary_interface")]
                public NetworkInterfaceInfo PrimaryInterface { get; internal set; }
                public class NetworkInterfaceInfo
                {
                    [DataMember(Name = "address")]
                    public string Address { get; internal set; }
                    [DataMember(Name = "name")]
                    public string Name { get; internal set; }
                    [DataMember(Name = "mac_address")]
                    public string MacAddress { get; internal set; }
                }
            }

            public class TransportInfo
            {
                [DataMember(Name = "bound_address")]
                public string BoundAddress { get; internal set; }
                [DataMember(Name = "publish_address")]
                public string PublishAddress { get; internal set; }
            }

            public class HTTPInfo
            {
                [DataMember(Name = "bound_address")]
                public dynamic BoundAddress { get; internal set; }
                [DataMember(Name = "publish_address")]
                public string PublishAddress { get; internal set; }
                [DataMember(Name = "max_content_length")]
                public string MaxContentLength { get; internal set; }
                [DataMember(Name = "max_content_length_in_bytes")]
                public long MaxContentLengthInBytes { get; internal set; }

                // TODO: Pretty
                public string PublishAddressPretty => PublishAddress;
            }
        }

        public class ClusterNodesStats
        {
            [DataMember(Name = "cluster_name")]
            public string ClusterName { get; internal set; }
            [DataMember(Name = "nodes")]
            public Dictionary<string, NodeStats> Nodes { get; set; }

            public class NodeStats
            {
                [DataMember(Name = "timestamp")]
                public long Timestamp { get; internal set; }
                [DataMember(Name = "name")]
                public string Name { get; internal set; }
                [DataMember(Name = "transport_address")]
                public string TransportAddress { get; internal set; }
                [DataMember(Name = "hostname")]
                public string Hostname { get; internal set; }
                [DataMember(Name = "indices")]
                public IndexStats Indexes { get; internal set; }
                [DataMember(Name = "os")]
                public OSStats OS { get; internal set; }
                [DataMember(Name = "process")]
                public ProcessStats Process { get; internal set; }
                [DataMember(Name = "jvm")]
                public JVMStats JVM { get; internal set; }
                [DataMember(Name = "thread_pool")]
                public Dictionary<string, ThreadCountStats> ThreadPool { get; internal set; }
                [DataMember(Name = "network")]
                public NetworkStats Network { get; internal set; }
                [DataMember(Name = "fs")]
                public FileSystemStats FileSystem { get; internal set; }
                [DataMember(Name = "transport")]
                public TransportStats Transport { get; internal set; }
                [DataMember(Name = "http")]
                public HTTPStats HTTP { get; internal set; }

                public class IndexStats
                {
                    [DataMember(Name = "store")]
                    public IndexStoreStats Store { get; internal set; }
                    [DataMember(Name = "docs")]
                    public DocStats Docs { get; internal set; }
                    [DataMember(Name = "indexing")]
                    public IndexingStats Indexing { get; internal set; }
                    [DataMember(Name = "get")]
                    public GetStats Get { get; internal set; }
                    [DataMember(Name = "search")]
                    public SearchStats Search { get; internal set; }
                    [DataMember(Name = "cache")]
                    public IndexCacheStats Cache { get; internal set; }
                    [DataMember(Name = "merges")]
                    public MergesStats Merges { get; internal set; }
                    [DataMember(Name = "refresh")]
                    public RefreshStats Refresh { get; internal set; }
                    [DataMember(Name = "flush")]
                    public FlushStats Flush { get; internal set; }

                    public class IndexStoreStats
                    {
                        [DataMember(Name = "size")]
                        public string Size { get; set; }
                        [DataMember(Name = "size_in_bytes")]
                        public double SizeInBytes { get; set; }
                        [DataMember(Name = "throttle_time")]
                        public string ThrottleTime { get; internal set; }
                        [DataMember(Name = "throttle_time_in_millis")]
                        public long ThrottleTimeInMilliseconds { get; internal set; }
                    }

                    public class DocStats
                    {
                        [DataMember(Name = "count")]
                        public long Count { get; set; }
                        [DataMember(Name = "deleted")]
                        public long Deleted { get; set; }
                    }

                    public class IndexingStats
                    {
                        [DataMember(Name = "index_total")]
                        public long Total { get; set; }
                        [DataMember(Name = "index_time")]
                        public string Time { get; set; }
                        [DataMember(Name = "index_time_in_millis")]
                        public double TimeInMilliseconds { get; set; }
                        [DataMember(Name = "index_current")]
                        public long Current { get; set; }
                        [DataMember(Name = "delete_total")]
                        public long DeleteTotal { get; set; }
                        [DataMember(Name = "delete_time")]
                        public string DeleteTime { get; set; }
                        [DataMember(Name = "delete_time_in_millis")]
                        public double DeleteTimeInMilliseconds { get; set; }
                        [DataMember(Name = "delete_current")]
                        public long DeleteCurrent { get; set; }
                        [DataMember(Name = "types")]
                        public Dictionary<string, TypeStats> Types { get; set; }
                    }

                    public class TypeStats
                    {
                        [DataMember(Name = "index_total")]
                        public long Total { get; set; }
                        [DataMember(Name = "index_time")]
                        public string Time { get; set; }
                        [DataMember(Name = "index_time_in_millis")]
                        public double TimeInMilliseconds { get; set; }
                        [DataMember(Name = "index_current")]
                        public long Current { get; set; }
                        [DataMember(Name = "delete_total")]
                        public long DeleteTotal { get; set; }
                        [DataMember(Name = "delete_time")]
                        public string DeleteTime { get; set; }
                        [DataMember(Name = "delete_time_in_millis")]
                        public double DeleteTimeInMilliseconds { get; set; }
                        [DataMember(Name = "delete_current")]
                        public long DeleteCurrent { get; set; }
                    }

                    public class GetStats
                    {
                        [DataMember(Name = "total")]
                        public long Total { get; set; }
                        [DataMember(Name = "time")]
                        public string Time { get; set; }
                        [DataMember(Name = "time_in_millis")]
                        public double TimeInMilliseconds { get; set; }
                        [DataMember(Name = "current")]
                        public long Current { get; set; }
                        [DataMember(Name = "exists_total")]
                        public long ExistsTotal { get; set; }
                        [DataMember(Name = "exists_time")]
                        public string ExistsTime { get; set; }
                        [DataMember(Name = "exists_time_in_millis")]
                        public double ExistsTimeInMilliseconds { get; set; }
                        [DataMember(Name = "missing_total")]
                        public long MissingTotal { get; set; }
                        [DataMember(Name = "missing_time")]
                        public string MissingTime { get; set; }
                        [DataMember(Name = "missing_time_in_millis")]
                        public double MissingTimeInMilliseconds { get; set; }
                    }

                    public class SearchStats
                    {
                        [DataMember(Name = "query_total")]
                        public long QueryTotal { get; set; }
                        [DataMember(Name = "query_time")]
                        public string QueryTime { get; set; }
                        [DataMember(Name = "query_time_in_millis")]
                        public double QueryTimeInMilliseconds { get; set; }
                        [DataMember(Name = "query_current")]
                        public long QueryCurrent { get; set; }
                        [DataMember(Name = "fetch_total")]
                        public long FetchTotal { get; set; }
                        [DataMember(Name = "fetch_time")]
                        public string FetchTime { get; set; }
                        [DataMember(Name = "fetch_time_in_millis")]
                        public double FetchTimeInMilliseconds { get; set; }
                        [DataMember(Name = "fetch_current")]
                        public long FetchCurrent { get; set; }
                    }

                    public class IndexCacheStats
                    {
                        [DataMember(Name = "field_evictions")]
                        public long FieldEvictions { get; internal set; }
                        [DataMember(Name = "field_size")]
                        public string FieldSize { get; internal set; }
                        [DataMember(Name = "field_size_in_bytes")]
                        public long FieldSizeInBytes { get; internal set; }
                        [DataMember(Name = "filter_count")]
                        public long FilterCount { get; internal set; }
                        [DataMember(Name = "filter_evictions")]
                        public long FilterEvictions { get; internal set; }
                        [DataMember(Name = "filter_size")]
                        public string FilterSize { get; internal set; }
                        [DataMember(Name = "filter_size_in_bytes")]
                        public long FilterSizeInBytes { get; internal set; }
                        [DataMember(Name = "bloom_size")]
                        public string BloomSize { get; internal set; }
                        [DataMember(Name = "bloom_size_in_bytes")]
                        public long BloomSizeInBytes { get; internal set; }
                        [DataMember(Name = "id_cache_size")]
                        public string IDCacheSize { get; internal set; }
                        [DataMember(Name = "id_cache_size_in_bytes")]
                        public long IDCacheSizeInBytes { get; internal set; }
                    }

                    public class MergesStats
                    {
                        [DataMember(Name = "current")]
                        public long Current { get; set; }
                        [DataMember(Name = "current_docs")]
                        public long CurrentDocuments { get; set; }
                        [DataMember(Name = "current_size")]
                        public string CurrentSize { get; set; }
                        [DataMember(Name = "current_size_in_bytes")]
                        public double CurrentSizeInBytes { get; set; }
                        [DataMember(Name = "total")]
                        public long Total { get; set; }
                        [DataMember(Name = "total_time")]
                        public string TotalTime { get; set; }
                        [DataMember(Name = "total_time_in_millis")]
                        public double TotalTimeInMilliseconds { get; set; }
                        [DataMember(Name = "total_docs")]
                        public long TotalDocuments { get; set; }
                        [DataMember(Name = "total_size")]
                        public string TotalSize { get; set; }
                        [DataMember(Name = "total_size_in_bytes")]
                        public long TotalSizeInBytes { get; set; }
                    }

                    public class RefreshStats
                    {
                        [DataMember(Name = "total")]
                        public long Total { get; set; }
                        [DataMember(Name = "total_time")]
                        public string TotalTime { get; set; }
                        [DataMember(Name = "total_time_in_millis")]
                        public double TotalTimeInMilliseconds { get; set; }
                    }
                    
                    public class FlushStats
                    {
                        [DataMember(Name = "total")]
                        public long Total { get; set; }
                        [DataMember(Name = "total_time")]
                        public string TotalTime { get; set; }
                        [DataMember(Name = "total_time_in_millis")]
                        public double TotalTimeInMilliseconds { get; set; }
                    }
                }

                public class OSStats : UptimeStats
                {
                    [DataMember(Name = "cpu")]
                    public CPUStats CPU { get; internal set; }
                    [DataMember(Name = "mem")]
                    public MemoryStats Memory { get; internal set; }
                    [DataMember(Name = "swap")]
                    public BaseMemoryStats Swap { get; internal set; }

                    public class CPUStats
                    {
                        [DataMember(Name = "sys")]
                        public int System { get; internal set; }
                        [DataMember(Name = "user")]
                        public int User { get; internal set; }
                        [DataMember(Name = "idle")]
                        public int Idle { get; internal set; }
                    }

                    public class BaseMemoryStats
                    {
                        [DataMember(Name = "used_in_bytes")]
                        public long UsedInBytes { get; internal set; }
                        [DataMember(Name = "free_in_bytes")]
                        public long FreeInBytes { get; internal set; }
                    }

                    public class MemoryStats : BaseMemoryStats
                    {
                        [DataMember(Name = "free_percent")]
                        public int FreePercent { get; internal set; }
                        [DataMember(Name = "used_percent")]
                        public int UsedPercent { get; internal set; }
                        [DataMember(Name = "total_in_bytes")]
                        public long? TotalInBytes { get; internal set; }
                    }
                }

                public class ProcessStats
                {
                    [DataMember(Name = "timestamp")]
                    public long Timestamp { get; internal set; }
                    [DataMember(Name = "open_file_descriptors")]
                    public int OpenFileDescriptors { get; internal set; }
                    [DataMember(Name = "cpu")]
                    public CPUStats CPU { get; internal set; }
                    [DataMember(Name = "mem")]
                    public MemoryStats Memory { get; internal set; }

                    public class CPUStats
                    {
                        [DataMember(Name = "percent")]
                        public int Percent { get; internal set; }
                        [DataMember(Name = "total_in_millis")]
                        public long TotalInMilliseconds { get; internal set; }
                    }

                    public class MemoryStats
                    {
                        [DataMember(Name = "resident")]
                        public string Resident { get; internal set; }
                        [DataMember(Name = "resident_in_bytes")]
                        public long ResidentInBytes { get; internal set; }
                        [DataMember(Name = "share")]
                        public string Share { get; internal set; }
                        [DataMember(Name = "share_in_bytes")]
                        public long ShareInBytes { get; internal set; }
                        [DataMember(Name = "total_virtual")]
                        public string TotalVirtual { get; internal set; }
                        [DataMember(Name = "total_virtual_in_bytes")]
                        public long TotalVirtualInBytes { get; internal set; }
                    }
                }

                public class UptimeStats
                {
                    [DataMember(Name = "timestamp")]
                    public long Timestamp { get; internal set; }
                    [DataMember(Name = "uptime")]
                    public string Uptime { get; internal set; }
                    [DataMember(Name = "uptime_in_millis")]
                    public long UptimeInMilliseconds { get; internal set; }
                    [DataMember(Name = "load_average")]
                    public dynamic LoadAverage { get; internal set; }

                    public string LoadAverageString => LoadAverage?.ToString();
                }

                public class JVMStats : UptimeStats
                {
                    [DataMember(Name = "mem")]
                    public MemoryStats Memory { get; internal set; }
                    [DataMember(Name = "threads")]
                    public ThreadStats Threads { get; internal set; }
                    [DataMember(Name = "gc")]
                    public GCOverallStats GC { get; internal set; }
                    [DataMember(Name = "buffer_pools")]
                    public Dictionary<string, NodeBufferPool> BufferPools { get; internal set; }

                    public class MemoryStats
                    {
                        [DataMember(Name = "heap_used")]
                        public string HeapUsed { get; internal set; }
                        [DataMember(Name = "heap_used_in_bytes")]
                        public long HeapUsedInBytes { get; internal set; }
                        [DataMember(Name = "heap_committed")]
                        public string HeapCommitted { get; internal set; }
                        [DataMember(Name = "heap_committed_in_bytes")]
                        public long HeapCommittedInBytes { get; internal set; }
                        [DataMember(Name = "non_heap_used")]
                        public string NonHeapUsed { get; internal set; }
                        [DataMember(Name = "non_heap_used_in_bytes")]
                        public long NonHeapUsedInBytes { get; internal set; }
                        [DataMember(Name = "non_heap_committed")]
                        public string NonHeapCommitted { get; internal set; }
                        [DataMember(Name = "non_heap_committed_in_bytes")]
                        public long NonHeapCommittedInBytes { get; internal set; }
                        [DataMember(Name = "pools")]
                        public Dictionary<string, JVMPool> Pools { get; internal set; }
                        public class JVMPool
                        {
                            [DataMember(Name = "used")]
                            public string Used { get; internal set; }
                            [DataMember(Name = "")]
                            public long UsedInBytes { get; internal set; }
                            [DataMember(Name = "max")]
                            public string Max { get; internal set; }
                            [DataMember(Name = "max_in_bytes")]
                            public long MaxInBytes { get; internal set; }
                            [DataMember(Name = "peak_used")]
                            public string PeakUsed { get; internal set; }
                            [DataMember(Name = "peak_used_in_bytes")]
                            public long PeakUsedInBytes { get; internal set; }
                            [DataMember(Name = "peak_max")]
                            public string PeakMax { get; internal set; }
                            [DataMember(Name = "peak_max_in_bytes")]
                            public long PeakMaxInBytes { get; internal set; }
                        }
                    }

                    public class ThreadStats
                    {
                        [DataMember(Name = "count")]
                        public long Count { get; internal set; }
                        [DataMember(Name = "peak_count")]
                        public long PeakCount { get; internal set; }
                    }

                    public class GCOverallStats : GarbageCollectorStats
                    {
                        [DataMember(Name = "collectors")]
                        public Dictionary<string, GarbageCollectorStats> Collectors { get; internal set; }
                    }

                    public class GarbageCollectorStats
                    {
                        [DataMember(Name = "collection_count")]
                        public long CollectionCount { get; internal set; }
                        [DataMember(Name = "collection_time")]
                        public string CollectionTime { get; internal set; }
                        [DataMember(Name = "collection_time_in_millis")]
                        public long CollectionTimeInMilliseconds { get; internal set; }
                    }

                    public class NodeBufferPool
                    {
                        [DataMember(Name = "count")]
                        public long Count { get; internal set; }
                        [DataMember(Name = "used")]
                        public string Used { get; internal set; }
                        [DataMember(Name = "used_in_bytes")]
                        public long UsedInBytes { get; internal set; }
                        [DataMember(Name = "total_capacity")]
                        public string TotalCapacity { get; internal set; }
                        [DataMember(Name = "total_capacity_in_bytes")]
                        public long TotalCapacityInBytes { get; internal set; }
                    }
                }

                public class ThreadCountStats
                {
                    [DataMember(Name = "threads")]
                    public long Threads { get; internal set; }
                    [DataMember(Name = "queue")]
                    public long Queue { get; internal set; }
                    [DataMember(Name = "active")]
                    public long Active { get; internal set; }
                    [DataMember(Name = "rejected")]
                    public long Rejected { get; internal set; }
                    [DataMember(Name = "largest")]
                    public long Largest { get; internal set; }
                    [DataMember(Name = "completed")]
                    public long Completed { get; internal set; }
                }

                public class NetworkStats
                {
                    [DataMember(Name = "tcp")]
                    public TCPStats TCP { get; internal set; }

                    public class TCPStats
                    {
                        [DataMember(Name = "active_opens")]
                        public long ActiveOpens { get; internal set; }
                        [DataMember(Name = "passive_opens")]
                        public long PassiceOpens { get; internal set; }
                        [DataMember(Name = "curr_estab")]
                        public long CurrentEstablished { get; internal set; }
                        [DataMember(Name = "in_segs")]
                        public long InSegments { get; internal set; }
                        [DataMember(Name = "out_segs")]
                        public long OutSegments { get; internal set; }
                        [DataMember(Name = "retrans_segs")]
                        public long RetransmittedSegments { get; internal set; }
                        [DataMember(Name = "estab_resets")]
                        public long EstablishedResets { get; internal set; }
                        [DataMember(Name = "attempt_fails")]
                        public long AttemptFails { get; internal set; }
                        [DataMember(Name = "in_errs")]
                        public long InErrors { get; internal set; }
                        [DataMember(Name = "out_rsts")]
                        public long OutResets { get; internal set; }
                    }
                }

                public class FileSystemStats
                {
                    [DataMember(Name = "timestamp")]
                    public long Timestamp { get; internal set; }
                    [DataMember(Name = "data")]
                    public DatumStats[] Data { get; internal set; }

                    public class DatumStats
                    {
                        [DataMember(Name = "path")]
                        public string Path { get; internal set; }
                        [DataMember(Name = "mount")]
                        public string Mount { get; internal set; }
                        [DataMember(Name = "dev")]
                        public string Dev { get; internal set; }
                        [DataMember(Name = "total")]
                        public string Total { get; internal set; }
                        [DataMember(Name = "total_in_bytes")]
                        public long TotalInBytes { get; internal set; }
                        [DataMember(Name = "free")]
                        public string Free { get; internal set; }
                        [DataMember(Name = "free_in_bytes")]
                        public long FreeInBytes { get; internal set; }
                        [DataMember(Name = "available")]
                        public string Available { get; internal set; }
                        [DataMember(Name = "available_in_bytes")]
                        public long AvailableInBytes { get; internal set; }
                        [DataMember(Name = "disk_reads")]
                        public long DiskReads { get; internal set; }
                        [DataMember(Name = "disk_writes")]
                        public long DiskWrites { get; internal set; }
                        [DataMember(Name = "disk_read_size")]
                        public string DiskReadSize { get; internal set; }
                        [DataMember(Name = "disk_read_size_in_bytes")]
                        public long DiskReadSizeInBytes { get; internal set; }
                        [DataMember(Name = "disk_write_size")]
                        public string DiskWriteSize { get; internal set; }
                        [DataMember(Name = "disk_write_size_in_bytes")]
                        public long DiskWriteSizeInBytes { get; internal set; }
                        [DataMember(Name = "disk_queue")]
                        public string DiskQueue { get; internal set; }
                    }
                }
                
                public class TransportStats
                {
                    [DataMember(Name = "server_open")]
                    public int ServerOpen { get; internal set; }
                    [DataMember(Name = "rx_count")]
                    public long RXCount { get; internal set; }
                    [DataMember(Name = "rx_size")]
                    public string RXSize { get; internal set; }
                    [DataMember(Name = "rx_size_in_bytes")]
                    public long RXSizeInBytes { get; internal set; }
                    [DataMember(Name = "tx_count")]
                    public long TXCount { get; internal set; }
                    [DataMember(Name = "tx_size")]
                    public string TXSize { get; internal set; }
                    [DataMember(Name = "tx_size_in_bytes")]
                    public long TXSizeInBytes { get; internal set; }
                }

                public class HTTPStats
                {
                    [DataMember(Name = "current_open")]
                    public int CurrentOpen { get; internal set; }
                    [DataMember(Name = "total_opened")]
                    public long TotalOpened { get; internal set; }
                }
            }
        }
    }
}
