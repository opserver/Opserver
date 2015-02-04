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
            return true;
        }

        /// <summary>
        /// Promote this instance to a master
        /// </summary>
        public string PromoteToMaster()
        {
            using (var log = new StringWriter())
            {
                this._connection.GetSingleServer().MakeMaster(ReplicationChangeOptions.Broadcast | ReplicationChangeOptions.SetTiebreaker, log);
                return log.ToString();
            }
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
