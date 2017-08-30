﻿using System.Collections.Generic;
using System.Linq;
using StackExchange.Profiling;
using StackExchange.Redis;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisInstance
    {
        private const int SlowLogCountToFetch = 200;
        private const string ConfigParamSlowLogThreshold = "slowlog-log-slower-than";
        private const string ConfigParamSlowLogMaxLength = "slowlog-max-len";

        /// <summary>
        /// Is Slow Log enabled on this instance, determined by checking the slow-log-slower-than config value
        /// </summary>
        /// <remarks>
        /// For setup instructions call <see cref="SetSlowLogThreshold"/> and <see cref="SetSlowLogMaxLength"/> or see: http://redis.io/commands/slowlog
        /// </remarks>
        public bool IsSlowLogEnabled =>
            Config.Data != null
            && Config.Data.TryGetValue(ConfigParamSlowLogThreshold, out string configVal)
            && int.TryParse(configVal, out int numVal)
            && numVal > 0;

        private Cache<List<CommandTrace>> _slowLog;
        public Cache<List<CommandTrace>> SlowLog =>
            _slowLog ?? (_slowLog = GetRedisCache(60.Seconds(), async () =>
            {
                //TODO: Remove when StackExchange.Redis gets profiling
                using (MiniProfiler.Current.CustomTiming("redis", "slowlog get " + SlowLogCountToFetch.ToString()))
                {
                    return (await Connection.GetSingleServer().SlowlogGetAsync(SlowLogCountToFetch).ConfigureAwait(false)).ToList();
                }
            }));

        private Cache<string> _tieBreaker;
        public Cache<string> Tiebreaker =>
            _tieBreaker ?? (_tieBreaker = GetRedisCache(10.Seconds(), () =>
            {
                using (MiniProfiler.Current.CustomTiming("redis", "tiebreaker fetch"))
                {
                    return GetSERedisTiebreakerAsync(Connection);
                }
            }));

        /// <summary>
        /// Sets the slow log threshold in milliseconds, note: 0 logs EVERY command, null or negative disables logging.
        /// </summary>
        /// <param name="minMilliseconds">Minimum milliseconds before a command is logged, null or 0 means disabled</param>
        public void SetSlowLogThreshold(int? minMilliseconds)
        {
            var value = minMilliseconds > 0 ? (minMilliseconds*1000).ToString() : null;
            SetConfigValue(ConfigParamSlowLogThreshold, value);
        }

        /// <summary>
        /// Sets the max retention of the slow log
        /// </summary>
        /// <param name="numItems">Max number of items to keep in the slow log</param>
        public void SetSlowLogMaxLength(int numItems)
        {
            SetConfigValue(ConfigParamSlowLogMaxLength, numItems.ToString());
        }

        /// <summary>
        /// Clears the SlowLog for this redis instance
        /// </summary>
        /// <remarks>
        /// </remarks>
        public void ClearSlowLog()
        {
            Connection.GetSingleServer().SlowlogReset();
        }
    }
}