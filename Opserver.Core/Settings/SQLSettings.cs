using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public class SQLSettings : Settings<SQLSettings>
    {
        public override bool Enabled => Clusters.Any() || Instances.Any();

        public List<Cluster> Clusters { get; set; }

        public List<Instance> Instances { get; set; }

        public SQLSettings()
        {
            Clusters = new List<Cluster>();
            Instances = new List<Instance>();
        }

        /// <summary>
        /// The default connection string to use when connecting to servers, $ServerName$ will be parameterized
        /// </summary>
        public string DefaultConnectionString { get; set; }

        public class Cluster : ISettingsCollectionItem<Cluster>
        {
            public List<Instance> Nodes { get; set; }

            public Cluster()
            {
                Nodes = new List<Instance>();
            }

            /// <summary>
            /// The machine name for this SQL cluster
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

            public bool Equals(Cluster other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Nodes.SequenceEqual(other.Nodes)
                       && string.Equals(Name, other.Name)
                       && string.Equals(Description, other.Description)
                       && RefreshIntervalSeconds == other.RefreshIntervalSeconds;
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
                    return hashCode;
                }
            }

            public static bool operator ==(Cluster left, Cluster right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Cluster left, Cluster right)
            {
                return !Equals(left, right);
            }
        }

        public class Instance : ISettingsCollectionItem<Instance>
        {
            /// <summary>
            /// The machine name for this SQL instance
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Connection string for this instance
            /// </summary>
            public string ConnectionString { get; set; }

            /// <summary>
            /// Object Name for this instance
            /// </summary>
            public string ObjectName { get; set; }

            public bool Equals(Instance other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Name, other.Name) && string.Equals(ConnectionString, other.ConnectionString) && string.Equals(ObjectName, other.ObjectName);
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
                    return ((Name?.GetHashCode() ?? 0)*397) ^ (ConnectionString?.GetHashCode() ?? 0);
                }
            }
        }
    }
}
