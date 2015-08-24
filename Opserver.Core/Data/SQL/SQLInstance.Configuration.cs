using System;
using System.Collections.Generic;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<SQLConfigurationOption>> _configuration;
        public Cache<List<SQLConfigurationOption>> Configuration
        {
            get
            {
                return _configuration ?? (_configuration = new Cache<List<SQLConfigurationOption>>
                {
                    CacheForSeconds = 2*60,
                    UpdateCache = UpdateFromSql("Configuration", async conn =>
                    {
                        var result = await conn.QueryAsync<SQLConfigurationOption>(SQLConfigurationOption.FetchSQL);
                        foreach (var r in result)
                        {
                            int defaultVal;
                            if (ConfigurationDefaults.TryGetValue(r.Name, out defaultVal))
                                r.Default = defaultVal;
                        }
                        return result;
                    })
                });
            }
        }

        private Dictionary<string, int> _configurationDefaults;
        public Dictionary<string, int> ConfigurationDefaults => _configurationDefaults ?? (_configurationDefaults = SQLConfigurationOption.GetDefaults(this));

        public class SQLConfigurationOption
        {
            public int ConfigurationId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public int Value { get; set; }
            public int ValueInUse { get; set; }
            public int Minimum { get; set; }
            public int Maximum { get; set; }
            public bool IsDyanmic { get; set; }
            public bool IsAdvanced { get; set; }
            public int Default { get; set; }

            public bool IsDefault => ValueInUse == Default || (Name == "min server memory (MB)" && ValueInUse == 16);

            internal const string FetchSQL = @"
 Select configuration_id ConfigurationId,
		name Name,
		description Description,
		Cast(value as int) value,
		Cast(minimum as int) Minimum,
		Cast(maximum as int) Maximum,
		Cast(value_in_use as int) ValueInUse,
		is_dynamic IsDynamic,
		is_advanced IsAdvanced
   From sys.configurations
Order By is_advanced, name";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }

            public static Dictionary<string, int> GetDefaults(SQLInstance i)
            {
                var dict = new Dictionary<string, int>
                {
                    {"access check cache bucket count", 0},
                    {"access check cache quota", 0},
                    {"Ad Hoc Distributed Queries", 0},
                    {"affinity I/O mask", 0},
                    {"affinity mask", 0},
                    {"affinity64 I/O mask", 0},
                    {"affinity64 mask", 0},
                    {"Agent XPs", 0},
                    {"allow updates", 0},
                    {"awe enabled", 0},
                    {"blocked process threshold", 0},
                    {"c2 audit mode", 0},
                    {"clr enabled", 0},
                    {"contained database authentication", 0},
                    {"cost threshold for parallelism", 5},
                    {"cross db ownership chaining", 0},
                    {"cursor threshold", -1},
                    {"Database Mail XPs", 0},
                    {"default full-text language", 1033},
                    {"default language", 0},
                    {"default trace enabled", 1},
                    {"disallow results from triggers", 0},
                    {"fill factor (%)", 0},
                    {"ft crawl bandwidth (max)", 100},
                    {"ft crawl bandwidth (min)", 0},
                    {"ft notify bandwidth (max)", 100},
                    {"ft notify bandwidth (min)", 0},
                    {"index create memory (KB)", 0},
                    {"in-doubt xact resolution", 0},
                    {"lightweight pooling", 0},
                    {"locks", 0},
                    {"max degree of parallelism", 0},
                    {"max full-text crawl range", 4},
                    {"max server memory (MB)", 2147483647},
                    {"max text repl size (B)", 65536},
                    {"max worker threads", 0},
                    {"media retention", 0},
                    {"min memory per query (KB)", 1024},
                    {"min server memory (MB)", 0},
                    {"nested triggers", 1},
                    {"network packet size (B)", 4096},
                    {"Ole Automation Procedures", 0},
                    {"open objects", 0},
                    {"optimize for ad hoc workloads", 0},
                    {"PH timeout (s)", 60},
                    {"precompute rank", 0},
                    {"priority boost", 0},
                    {"query governor cost limit", 0},
                    {"query wait (s)", -1},
                    {"recovery interval (min)", 0},
                    {"remote access", 1},
                    {"remote admin connections", 0},
                    {"remote login timeout (s)", 10},
                    {"remote proc trans", 0},
                    {"remote query timeout (s)", 600},
                    {"Replication XPs", 0},
                    {"RPC parameter data validation", 0},
                    {"scan for startup procs", 0},
                    {"server trigger recursion", 1},
                    {"set working set size", 0},
                    {"show advanced options", 0},
                    {"SMO and DMO XPs", 1},
                    {"SQL Mail XPs", 0},
                    {"transform noise words", 0},
                    {"two digit year cutoff", 2049},
                    {"user connections", 0},
                    {"user options", 0},
                    {"Web Assistant Procedures", 0},
                    {"xp_cmdshell", 0}
                };

                // some defaults were different before 2012
                if (i.Version < SQLServerVersions.SQL2012.RTM)
                {
                    dict["remote login timeout (s)"] = 20;
                }
                return dict;
            }
        }
    }
}
