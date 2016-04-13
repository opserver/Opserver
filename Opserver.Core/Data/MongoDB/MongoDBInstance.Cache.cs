using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace StackExchange.Opserver.Data.MongoDB
{
    public partial class MongoDBInstance
    {
        public static List<MongoDBInstance> AllInstances { get; } =
            MongoDBConnectionInfo.AllConnections
                .Select(rci => new MongoDBInstance(rci))
                .Where(rsi => rsi.TryAddToGlobalPollers())
                .ToList();

        public static MongoDBInstance GetInstance(MongoDBConnectionInfo info)
        {
            foreach (var i in AllInstances)
            {
                if (i.ConnectionInfo == info) return i;
            }
            return null;
        }
        public static MongoDBInstance GetInstance(string connectionString)
        {
            if (connectionString.IsNullOrEmpty()) return null;
            if (connectionString.Contains(":"))
            {
                var parts = connectionString.Split(StringSplits.Colon);
                if (parts.Length != 2) return null;
                int port;
                if (int.TryParse(parts[1], out port)) return GetInstance(parts[0], port);
            }
            else
            {
                return GetInstances(connectionString).FirstOrDefault();
            }
            return null;
        }
        public static MongoDBInstance GetInstance(string host, int port)
        {
            foreach (var ri in AllInstances)
            {
                if (ri.Host == host && ri.Port == port) return ri;
            }
            var shortHost = host.Split(StringSplits.Period).First();
            foreach (var ri in AllInstances)
            {
                if (ri.Port == port && ri.ShortHost == shortHost) return ri;
            }
            return null;
        }

        public static MongoDBInstance GetInstance(int port, IPAddress ipAddress)
        {
            foreach (var i in AllInstances)
            {
                if (i.ConnectionInfo.Port != port) continue;
                foreach (var ip in i.ConnectionInfo.IPAddresses)
                {
                    if (ip.Equals(ipAddress)) return i;
                }
            }
            return null;
        }

        public static List<MongoDBInstance> GetInstances(string node)
        {
            return AllInstances.Where(ri => string.Equals(ri.Host, node, StringComparison.InvariantCultureIgnoreCase)).ToList();
        }

        public static bool IsMongoDBServer(string node)
        {
            foreach (var i in AllInstances)
            {
                // TODO: Dictionary
                if (string.Equals(i.Host, node, StringComparison.InvariantCultureIgnoreCase)) return true;
            }
            return false;
        }
    }
}
