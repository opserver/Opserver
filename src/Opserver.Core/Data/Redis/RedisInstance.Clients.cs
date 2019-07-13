using System.Collections.Generic;
using System.Linq;
using StackExchange.Profiling;
using StackExchange.Redis;

namespace Opserver.Data.Redis
{
    public partial class RedisInstance
    {
        private Cache<List<ClientInfo>> _clients;
        public Cache<List<ClientInfo>> Clients =>
            _clients ?? (_clients = GetRedisCache(60.Seconds(), async () =>
            {
                using (MiniProfiler.Current.CustomTiming("redis", "CLIENT LIST"))
                {
                    var result = await Connection.GetSingleServer().ClientListAsync();
                    return result.ToList();
                }
            }));
    }
}
