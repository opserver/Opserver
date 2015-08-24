using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisInstance
    {
        /// <summary>
        /// Slave this instance to another instance
        /// </summary>
        public async Task<bool> SlaveToAsync(string address)
        {
            var newMaster = EndPointCollection.TryParse(address);
            await _connection.GetSingleServer().SlaveOfAsync(newMaster);
            var newMasterInstance = GetInstance(address);
            await newMasterInstance?.PublishSERedisReconfigureAsync();
            return true;
        }

        /// <summary>
        /// Promote this instance to a master
        /// </summary>
        public string PromoteToMaster()
        {
            using (var log = new StringWriter())
            {
                _connection.GetSingleServer().MakeMaster(ReplicationChangeOptions.Broadcast, log);
                return log.ToString();
            }
        }

        /// <summary>
        /// The StackExchange.Redis tiebreaker key this node is currently using.
        /// </summary>
        /// <remarks>If this doesn't match the key (likely default) used in the other clients, it will have little or no effect</remarks>
        public string SERedisTiebreakerKey => ConfigurationOptions.Parse(_connection.Configuration).TieBreaker;

        /// <summary>
        /// Sets the StackExchange.Redis tiebreaker key on this node.
        /// </summary>
        public async Task<bool> SetSERedisTiebreakerAsync()
        {
            RedisKey tieBreakerKey = SERedisTiebreakerKey;

            var myEndPoint = _connection.GetEndPoints().FirstOrDefault();
            RedisValue tieBreakerValue = EndPointCollection.ToString(myEndPoint);

            var result = await _connection.GetDatabase()
                .StringSetAsync(tieBreakerKey, tieBreakerValue, flags: CommandFlags.NoRedirect | CommandFlags.HighPriority);
            Tiebreaker.Poll(true);
            return result;
        }

        /// <summary>
        /// Gets the current value of the StackExchange.Redis tiebreaker key on this node.
        /// </summary>
        public Task<string> GetSERedisTiebreakerAsync()
        {
            return GetSERedisTiebreakerAsync(_connection);
        }

        private async Task<string> GetSERedisTiebreakerAsync(ConnectionMultiplexer conn)
        {
            RedisKey tieBreakerKey = ConfigurationOptions.Parse(conn.Configuration).TieBreaker;
            return await conn.GetDatabase().StringGetAsync(tieBreakerKey, CommandFlags.NoRedirect);
        }

        /// <summary>
        /// Clears the StackExchange.Redis tiebreaker key from this node.
        /// </summary>
        public async Task<bool> ClearSERedisTiebreakerAsync()
        {
            RedisKey tieBreakerKey = SERedisTiebreakerKey;
            var result = await _connection.GetDatabase()
                .KeyDeleteAsync(tieBreakerKey, flags: CommandFlags.NoRedirect | CommandFlags.HighPriority);
            Tiebreaker.Poll(true);
            return result;
        }

        /// <summary>
        /// Instructs the redis node to broadcast a reconfiguration request to all StackExchange.Redis clients.
        /// </summary>
        public Task<long> PublishSERedisReconfigureAsync()
        {
            return _connection.PublishReconfigureAsync();
        }

        /// <summary>
        /// Kill a particular client's connection
        /// </summary>
        public async Task<bool> KillClientAsync(string address)
        {
            var endpoint = EndPointCollection.TryParse(address);
            if (endpoint == null) return false;
            await _connection.GetSingleServer().ClientKillAsync(endpoint);
            return true;
        }

        /// <summary>
        /// List of targets this instance may be slaved to, does not include:
        ///  - Current Master
        ///  - Any slaves in the chain (circles bad, k?)
        ///  - Itself
        /// </summary>
        public List<RedisInstance> RecommendedMasterTargets
        {
            get
            {
                return AllInstances.Where(s => s.Port == Port && s.Host != Host && !GetAllSlavesInChain().Contains(s) && Master != s).ToList();
            }
        }
    }
}
