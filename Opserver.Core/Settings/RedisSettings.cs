using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public class RedisSettings : Settings<RedisSettings>
    {
        public override bool Enabled => Servers.Any();

        public List<Server> Servers { get; set; } = new List<Server>();

        public Server AllServers { get; set; } = new Server();

        public Server Defaults { get; set; } = new Server();

        public class Server : ISettingsCollectionItem
        {
            public List<Instance> Instances { get; set; } = new List<Instance>();

            /// <summary>
            /// The machine name for this Redis server - used to match against the dashboard
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Unused currently
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// How many seconds before polling this cluster for status again
            /// </summary>
            public int RefreshIntervalSeconds { get; set; } = 30;
        }

        public class Instance : ISettingsCollectionItem
        {
            /// <summary>
            /// The machine name for this node
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Connection for for this node
            /// </summary>
            public int Port { get; set; } = 6379;

            /// <summary>
            /// The password for this node
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// Regular expressions collection to crawl keys against, to break out Redis DB usage
            /// </summary>
            public Dictionary<string, string> AnalysisRegexes { get; set; } = new Dictionary<string, string>();
        }
    }
}
