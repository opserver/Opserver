using System.Collections.Generic;
using System.Linq;
using BookSleeve;

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
                    UpdateCache = GetFromRedis("Clients", rc => rc.Wait(rc.Server.ListClients()).ToList())
                });
            }
        }
    }
}
