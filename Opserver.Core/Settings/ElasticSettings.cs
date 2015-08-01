using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public class ElasticSettings : Settings<ElasticSettings>
    {
        public override bool Enabled => Clusters.Any();

        /// <summary>
        /// elastic search clusters to monitor
        /// </summary>
        public List<Cluster> Clusters { get; set; }

        public ElasticSettings()
        {
            Clusters = new List<Cluster>();
        }

        public class Cluster : ISettingsCollectionItem<Cluster>
        {
            public Cluster()
            {
                // Defaults
                RefreshIntervalSeconds = 60;
                DownRefreshIntervalSeconds = 5;
                Nodes = new List<string>();
            }

            /// <summary>
            /// Nodes in this cluster
            /// </summary>
            public List<string> Nodes { get; set; }

            /// <summary>
            /// The machine name for this SQL cluster
            /// </summary>
            public string Name { get; set; }

            public string Description { get; set; }

            /// <summary>
            /// How many seconds before polling this cluster for status again
            /// </summary>
            public int RefreshIntervalSeconds { get; set; }

            /// <summary>
            /// How many seconds before polling this cluster for status again, if the cluster status is not green
            /// </summary>
            public int DownRefreshIntervalSeconds { get; set; }

            public bool Equals(Cluster other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Nodes.SequenceEqual(other.Nodes)
                       && string.Equals(Name, other.Name)
                       && string.Equals(Description, other.Description)
                       && RefreshIntervalSeconds == other.RefreshIntervalSeconds
                       && DownRefreshIntervalSeconds == other.DownRefreshIntervalSeconds;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Cluster) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = 0;
                    foreach (var n in Nodes)
                        hashCode = (hashCode*397) ^ n.GetHashCode();
                    hashCode = (hashCode*397) ^ (Name?.GetHashCode() ?? 0);
                    hashCode = (hashCode*397) ^ (Description?.GetHashCode() ?? 0);
                    hashCode = (hashCode*397) ^ RefreshIntervalSeconds;
                    hashCode = (hashCode*397) ^ DownRefreshIntervalSeconds;
                    return hashCode;
                }
            }
        }
    }
}
