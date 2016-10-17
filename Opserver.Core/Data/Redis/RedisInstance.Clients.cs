using System.Collections.Generic;
using System.Linq;
using StackExchange.Profiling;
using StackExchange.Redis;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisInstance
    {
        private Cache<List<ClientInfo>> _clients;
        public Cache<List<ClientInfo>> Clients => 
            _clients ?? (_clients = GetRedisCache(1.Minutes(), async () =>
            {
                using (MiniProfiler.Current.CustomTiming("redis", "CLIENT LIST"))
                {
                    var result = await Connection.GetSingleServer().ClientListAsync().ConfigureAwait(false);
                    return result.ToList();
                }
            }));
    }
}
