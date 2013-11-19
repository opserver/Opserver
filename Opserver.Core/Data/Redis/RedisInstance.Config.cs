using System.Collections.Generic;

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
                    UpdateCache = GetFromRedis("Config", rc => rc.Wait(rc.Server.GetConfig("*")))
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
            using (var rc = GetConnection(allowAdmin: true))
            {
                rc.Open();
                rc.Server.SetConfig(parameter, value);
            }
        }
    }
}