using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.MongoDB
{
    public partial class MongoDBConnectionInfo
    {
        public static List<MongoDBConnectionInfo> AllConnections { get; } = LoadMongoDBConnections();
        
        private static List<MongoDBConnectionInfo> LoadMongoDBConnections()
        {
            var result = new List<MongoDBConnectionInfo>();
            if (!Current.Settings.MongoDB.Enabled) return result;

            var servers = Current.Settings.MongoDB.Servers;

            var defaultServerInstances = Current.Settings.MongoDB.Defaults.Instances;
            var allServerInstances = Current.Settings.MongoDB.AllServers.Instances;

            foreach (var s in servers)
            {
                var count = result.Count;
                // Add instances that belong to any servers
                allServerInstances?.ForEach(gi => result.Add(new MongoDBConnectionInfo(s.Name, gi)));

                // Add instances defined on this server
                if (s.Instances.Any())
                    s.Instances.ForEach(i => result.Add(new MongoDBConnectionInfo(s.Name, i)));

                // If we have no instances added at this point, defaults it is!
                if (defaultServerInstances != null && count == result.Count)
                    defaultServerInstances.ForEach(gi => result.Add(new MongoDBConnectionInfo(s.Name, gi)));
            }
            return result;
        }

        public static MongoDBConnectionInfo FromCache(string host, int port)
        {
            return AllConnections.FirstOrDefault(i => i.Host == host && i.Port == port);
        }
    }
}
