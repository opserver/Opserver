using System.Collections.Generic;
using System.IO;
using System.Linq;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisInstance
    {
        private Cache<Dictionary<string, string>> _config;
        public Cache<Dictionary<string, string>> Config
        {
            get
            {
                return _config ?? (_config = new Cache<Dictionary<string, string>>
                {
                    CacheForSeconds = 120,
                    UpdateCache = GetFromRedisAsync("Config", async rc =>
                    {
                        //TODO: Remove when StackExchange.Redis gets profiling
                        using (MiniProfiler.Current.CustomTiming("redis", "CONFIG"))
                        {
                            return (await rc.GetSingleServer().ConfigGetAsync("*")).ToDictionary(x => x.Key, x => x.Value);
                        }
                    })
                });
            }
        }

        /// <summary>
        /// Sets a config value without needing a restart
        /// </summary>
        /// <param name="parameter">Config parameter to set</param>
        /// <param name="value">Value to set</param>
        public void SetConfigValue(string parameter, string value)
        {
            Connection.GetSingleServer().ConfigSet(parameter, value);
        }

        /// <summary>
        /// Gets the configuration for this instance's connection in a zip file format
        /// </summary>
        /// <returns>A byte array containing the zip's contents</returns>
        public byte[] GetConfigZip()
        {
            byte[] result;
            using (var ms = new MemoryStream())
            {
                _connection.ExportConfiguration(ms);
                ms.Seek(0, SeekOrigin.Begin);
                result = ms.ToArray();
            }
            return result;
        }
    }
}