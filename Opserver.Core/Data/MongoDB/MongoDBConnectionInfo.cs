using System.Collections.Generic;
using System.Net;
using StackExchange.Opserver.Helpers;

namespace StackExchange.Opserver.Data.MongoDB
{
    public partial class MongoDBConnectionInfo
    {
        public string Name => Settings.Name;
        public string Host { get; internal set; }
        public int Port => Settings.Port;
        public string Password => Settings.Password;

        internal MongoDBSettings.Instance Settings { get; set; }

        internal MongoDBConnectionInfo(string host, MongoDBSettings.Instance settings)
        {
            Settings = settings;
            Host = host;
        }

        public List<IPAddress> IPAddresses => AppCache.GetHostAddresses(Host);

        public override string ToString() => $"{Name} ({Host}:{Port.ToString()})";
    }
}
