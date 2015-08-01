using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public class RedisSettings : Settings<RedisSettings>
    {
        public override bool Enabled => Servers.Any();

        public List<Server> Servers { get; set; }

        private Server _allServers;
        public Server AllServers
        {
            get { return _allServers; }
            set
            {
                _allServers = value;
                AllServersChanged(this, value);
            }
        }
        public event EventHandler<Server> AllServersChanged = delegate { };
        
        public Server Defaults { get; set; }

        public RedisSettings()
        {
            Servers = new List<Server>();
            AllServers = new Server();
            Defaults = new Server();
        }

        public class Server : ISettingsCollectionItem<Server>
        {
            public List<Instance> Instances { get; set; }

            public Server()
            {
                // Defaults
                RefreshIntervalSeconds = 10;
                Instances = new List<Instance>();
            }

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
            public int RefreshIntervalSeconds { get; set; }

            public bool Equals(Server other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Instances.SequenceEqual(other.Instances)
                    && string.Equals(Name, other.Name) 
                    && string.Equals(Description, other.Description) 
                    && RefreshIntervalSeconds == other.RefreshIntervalSeconds;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Server) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = 0;
                    foreach (var i in Instances)
                        hashCode = (hashCode*397) ^ i.GetHashCode();
                    hashCode = (hashCode*397) ^ (Name?.GetHashCode() ?? 0);
                    hashCode = (hashCode*397) ^ (Description?.GetHashCode() ?? 0);
                    hashCode = (hashCode*397) ^ RefreshIntervalSeconds;
                    return hashCode;
                }
            }
        }

        public class Instance : ISettingsCollectionItem<Instance>
        {
            public Instance()
            {
                // Defaults
                Port = 6379;
                AnalysisRegexes = new Dictionary<string, string>();
            }

            /// <summary>
            /// The machine name for this node
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Connection for for this node
            /// </summary>
            public int Port { get; set; }

            /// <summary>
            /// The password for this node
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// Regular expressions collection to crawl keys against, to break out Redis DB usage
            /// </summary>
            public Dictionary<string, string> AnalysisRegexes { get; set; }

            public bool Equals(Instance other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Name, other.Name)
                       && Port == other.Port;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Instance) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    // TODO: Regexes should move
                    int hashCode = Name?.GetHashCode() ?? 0;
                    hashCode = (hashCode*397) ^ Port;
                    return hashCode;
                }
            }
        }
    }
}
