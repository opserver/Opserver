using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisInstance
    {
        public static RedisInstance Get(RedisConnectionInfo info)
        {
            foreach (var i in RedisModule.Instances)
            {
                if (i.ConnectionInfo == info) return i;
            }
            return null;
        }

        public static RedisInstance Get(string connectionString)
        {
            if (connectionString.IsNullOrEmpty()) return null;
            if (connectionString.Contains(":"))
            {
                var parts = connectionString.Split(StringSplits.Colon);
                if (parts.Length != 2) return null;
                if (int.TryParse(parts[1], out int port)) return Get(parts[0], port);
            }
            else
            {
                return GetAll(connectionString).FirstOrDefault();
            }
            return null;
        }

        public static RedisInstance Get(string host, int port)
        {
            foreach (var ri in RedisModule.Instances)
            {
                if (ri.Host.HostName == host && ri.Port == port) return ri;
            }
            var shortHost = host.Split(StringSplits.Period)[0];
            foreach (var ri in RedisModule.Instances)
            {
                if (ri.Port == port && ri.ShortHost == shortHost) return ri;
            }
            return null;
        }

        public static RedisInstance Get(int port, IPAddress ipAddress)
        {
            foreach (var i in RedisModule.Instances)
            {
                if (i.ConnectionInfo.Port != port) continue;
                foreach (var ip in i.ConnectionInfo.IPAddresses)
                {
                    if (ip.Equals(ipAddress)) return i;
                }
            }
            return null;
        }

        public static List<RedisInstance> GetAll(string node)
        {
            return RedisModule.Instances.Where(ri => string.Equals(ri.Host.HostName, node, StringComparison.InvariantCultureIgnoreCase)).ToList();
        }
    }
}
