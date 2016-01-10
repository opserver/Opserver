using System.Collections.Generic;
using System.Runtime.Serialization;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster
    {
        private Cache<IndexStatsInfo> _indexStats;
        public Cache<IndexStatsInfo> IndexStats => _indexStats ?? (_indexStats = new Cache<IndexStatsInfo>
        {
            CacheForSeconds = RefreshInterval,
            UpdateCache = UpdateFromElastic(nameof(IndexStats),
                async () => (await GetAsync<IndexStatsInfo>("_stats").ConfigureAwait(false)))
        });

        public class IndexStatsInfo
        {
            [DataMember(Name = "_shards")]
            public ShardCountStats Shards { get; internal set; }

            [DataMember(Name = "_all")]
            public IndexStats All { get; set; }

            [DataMember(Name = "indices")]
            public Dictionary<string, IndexStats> Indexes { get; set; }

            public class ShardCountStats
            {
                [DataMember(Name = "total")]
                public int Total { get; internal set; }

                [DataMember(Name = "successful")]
                public int Successful { get; internal set; }

                [DataMember(Name = "failed")]
                public int Failed { get; internal set; }
            }

            public class IndexStats
            {
                [DataMember(Name = "primaries")]
                public IndexShardStats Primaries { get; set; }
                [DataMember(Name = "total")]
                public IndexShardStats Total { get; set; }

                public class IndexShardStats
                {
                    [DataMember(Name = "docs")]
                    public DocStats Documents { get; set; }
                    [DataMember(Name = "store")]
                    public StoreStats Store { get; set; }
                    [DataMember(Name = "indexing")]
                    public IndexingStats Indexing { get; set; }
                    [DataMember(Name = "get")]
                    public GetStats Get { get; set; }
                    [DataMember(Name = "search")]
                    public SearchStats Search { get; set; }
                    [DataMember(Name = "merges")]
                    public MergesStats Merges { get; set; }
                    [DataMember(Name = "refresh")]
                    public TimeStats Refresh { get; set; }
                    [DataMember(Name = "flush")]
                    public TimeStats Flush { get; set; }

                    public class StoreStats
                    {
                        [DataMember(Name = "size")]
                        public string Size { get; set; }
                        [DataMember(Name = "size_in_bytes")]
                        public double SizeInBytes { get; set; }
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

                    public class MergesStats : TimeStats
                    {
                        [DataMember(Name = "current")]
                        public long Current { get; set; }
                        [DataMember(Name = "current_docs")]
                        public long CurrentDocuments { get; set; }
                        [DataMember(Name = "current_size")]
                        public string CurrentSize { get; set; }
                        [DataMember(Name = "current_size_in_bytes")]
                        public double CurrentSizeInBytes { get; set; }

                        [DataMember(Name = "total_docs")]
                        public long TotalDocuments { get; set; }
                        [DataMember(Name = "total_size")]
                        public string TotalSize { get; set; }
                        [DataMember(Name = "total_size_in_bytes")]
                        public long TotalSizeInBytes { get; set; }
                    }

                    public class TimeStats
                    {
                        [DataMember(Name = "total")]
                        public long Total { get; set; }
                        [DataMember(Name = "total_time")]
                        public string TotalTime { get; set; }
                        [DataMember(Name = "total_time_in_millis")]
                        public double TotalTimeInMilliseconds { get; set; }
                    }
                }
            }
        }
    }
}
