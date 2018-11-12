using System.Collections.Generic;

namespace StackExchange.Opserver.Data.Redis
{
    public class RedisReplicationGroup
    {
        public string Name { get; }
        public List<RedisHost> Hosts { get; internal set; }

        public RedisReplicationGroup(string name, List<RedisHost> hosts)
        {
            Name = name;
            Hosts = hosts;

            foreach (var h in hosts)
            {
                h.ReplicationGroup = this;
            }
        }

        public override string ToString() => Name;
    }
}
