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
        /// <param name="address">The address of the <see cref="RedisInstance"/> to slave to.</param>
        public async Task<bool> SlaveToAsync(string address)
        {
            var newMaster = EndPointCollection.TryParse(address);
            await _connection.GetSingleServer().SlaveOfAsync(newMaster).ConfigureAwait(false);
            var newMasterInstance = Get(address);
            if (newMasterInstance != null)
            {
                await newMasterInstance.PublishSERedisReconfigureAsync().ConfigureAwait(false);
            }
            return true;
        }

        /// <summary>
        /// Promote this instance to a master.
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
        /// Get the keys matching a pattern from this instance.
        /// </summary>
        /// <param name="db">The database ID to purge from.</param>
        /// <param name="key">The key to purge.</param>
        public async Task<int> KeyPurge(int db, string key)
        {
            if (db == -1)
            {
                // All databases...
                // TODO: This, maybe.
            }
            return await _connection.GetDatabase(db).KeyDeleteAsync(key).ConfigureAwait(false) ? 1 : 0;
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
                .StringSetAsync(tieBreakerKey, tieBreakerValue, flags: CommandFlags.NoRedirect)
                .ConfigureAwait(false);
            await Tiebreaker.PollAsync(true).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Gets the current value of the StackExchange.Redis tiebreaker key on this node.
        /// </summary>
        public Task<string> GetSERedisTiebreakerAsync() => GetSERedisTiebreakerAsync(_connection);

        private async Task<string> GetSERedisTiebreakerAsync(IConnectionMultiplexer conn)
        {
            RedisKey tieBreakerKey = ConfigurationOptions.Parse(conn.Configuration).TieBreaker;
            return await conn.GetDatabase()
                .StringGetAsync(tieBreakerKey, CommandFlags.NoRedirect)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Clears the StackExchange.Redis tiebreaker key from this node.
        /// </summary>
        public async Task<bool> ClearSERedisTiebreakerAsync()
        {
            RedisKey tieBreakerKey = SERedisTiebreakerKey;
            var result = await _connection.GetDatabase()
                .KeyDeleteAsync(tieBreakerKey, flags: CommandFlags.NoRedirect)
                .ConfigureAwait(false);
            await Tiebreaker.PollAsync(true).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Instructs the redis node to broadcast a reconfiguration request to all StackExchange.Redis clients
        /// </summary>
        public Task<long> PublishSERedisReconfigureAsync() => _connection.PublishReconfigureAsync();

        /// <summary>
        /// Kill a particular client's connection
        /// </summary>
        /// <param name="address">The address or the client to kill</param>
        public async Task<bool> KillClientAsync(string address)
        {
            var endpoint = EndPointCollection.TryParse(address);
            if (endpoint == null) return false;
            await _connection.GetSingleServer().ClientKillAsync(endpoint).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// List of targets this instance may be slaved to, does not include:
        ///  - Current Master
        ///  - Any slaves in the chain (circles bad, k?)
        ///  - Itself
        /// </summary>
        public List<RedisInstance> RecommendedMasterTargets =>
            RedisModule.Instances
            .Where(s => s.Port == Port && s.Name == Name && s.Host != Host && !GetAllSlavesInChain().Contains(s) && Master != s)
            .ToList();
    }
}
