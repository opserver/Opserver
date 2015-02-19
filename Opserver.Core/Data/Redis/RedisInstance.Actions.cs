using System.Collections.Generic;
using System.IO;
using System.Linq;
using StackExchange.Redis;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisInstance
    {
        /// <summary>
        /// Slave this instance to another instance
        /// </summary>
        public bool SlaveTo(string address)
        {
            var newMaster = EndPointCollection.TryParse(address);
            this._connection.GetSingleServer().SlaveOf(newMaster);
            var newMasterInstance = GetInstance(address);
            if (newMasterInstance != null)
            {
                newMasterInstance.PublishSERedisReconfigure();
            }
            return true;
        }

        /// <summary>
        /// Promote this instance to a master
        /// </summary>
        public string PromoteToMaster()
        {
            using (var log = new StringWriter())
            {
                this._connection.GetSingleServer().MakeMaster(ReplicationChangeOptions.Broadcast, log);
                return log.ToString();
            }
        }

        /// <summary>
        /// The StackExchange.Redis tiebreaker key this node is currently using.
        /// </summary>
        /// <remarks>If this doesn't match the key (likely default) used in the other clients, it will have little or no effect</remarks>
        public string SERedisTiebreakerKey
        {
            get { return ConfigurationOptions.Parse(_connection.Configuration).TieBreaker; }
        }

        /// <summary>
        /// Sets the StackExchange.Redis tiebreaker key on this node.
        /// </summary>
        public bool SetSERedisTiebreaker()
        {
            RedisKey tieBreakerKey = SERedisTiebreakerKey;

            var myEndPoint = this._connection.GetEndPoints().FirstOrDefault();
            RedisValue tieBreakerValue = EndPointCollection.ToString(myEndPoint);

            var result = this._connection.GetDatabase()
                .StringSet(tieBreakerKey, tieBreakerValue, flags: CommandFlags.NoRedirect | CommandFlags.HighPriority);
            Tiebreaker.Poll(true);
            return result;
        }

        /// <summary>
        /// Gets the current value of the StackExchange.Redis tiebreaker key on this node.
        /// </summary>
        public string GetSERedisTiebreaker()
        {
            return GetSERedisTiebreaker(_connection);
        }

        private string GetSERedisTiebreaker(ConnectionMultiplexer conn)
        {
            RedisKey tieBreakerKey = ConfigurationOptions.Parse(conn.Configuration).TieBreaker;
            return conn.GetDatabase().StringGet(tieBreakerKey, CommandFlags.NoRedirect);
        }

        /// <summary>
        /// Clears the StackExchange.Redis tiebreaker key from this node.
        /// </summary>
        public bool ClearSERedisTiebreaker()
        {
            RedisKey tieBreakerKey = SERedisTiebreakerKey;
            var result = this._connection.GetDatabase()
                .KeyDelete(tieBreakerKey, flags: CommandFlags.NoRedirect | CommandFlags.HighPriority);
            Tiebreaker.Poll(true);
            return result;
        }

        /// <summary>
        /// Instructs the redis node to broadcast a reconfiguration request to all StackExchange.Redis clients.
        /// </summary>
        public long PublishSERedisReconfigure()
        {
            return this._connection.PublishReconfigure();
        }

        /// <summary>
        /// Kill a particular client's connection
        /// </summary>
        public bool KillClient(string address)
        {
            var endpoint = EndPointCollection.TryParse(address);
            if (endpoint == null) return false;
            this._connection.GetSingleServer().ClientKill(endpoint);
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
