using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Profiling;
using StackExchange.Redis;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisInstance
    {
        private Cache<List<ClientInfo>> _clients;
        public Cache<List<ClientInfo>> Clients
        {
            get
            {
                return _clients ?? (_clients = new Cache<List<ClientInfo>>
                {
                    CacheForSeconds = 60,
                    UpdateCache = GetFromRedisAsync("Clients", rc =>
                    {
                        //TODO: Remove when StackExchange.Redis gets profiling
                        using (MiniProfiler.Current.CustomTiming("redis", "CLIENT LIST"))
                        {
                            return Task.FromResult(rc.GetSingleServer().ClientList().ToList());
                        }
                    })
                });
            }
        }
    }
}
