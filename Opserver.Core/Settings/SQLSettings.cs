using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StackExchange.Opserver
{
    public class SQLSettings : Settings<SQLSettings>, IAfterLoadActions
    {
        public override bool Enabled { get { return Clusters.Any() || Instances.Any(); } }

        public ObservableCollection<Cluster> Clusters { get; set; }
        public event EventHandler<Cluster> ClusterAdded = delegate { };
        public event EventHandler<List<Cluster>> ClustersChanged = delegate { };
        public event EventHandler<Cluster> ClusterRemoved = delegate { };

        public ObservableCollection<Instance> Instances { get; set; }
        public event EventHandler<Instance> InstanceAdded = delegate { };
        public event EventHandler<List<Instance>> InstancesChanged = delegate { };
        public event EventHandler<Instance> InstanceRemoved = delegate { };

        public SQLSettings()
        {
            Clusters = new ObservableCollection<Cluster>();
            Instances = new ObservableCollection<Instance>();
        }

        public void AfterLoad()
        {
            Clusters.AddHandlers(this, ClusterAdded, ClustersChanged, ClusterRemoved);
            Instances.AddHandlers(this, InstanceAdded, InstancesChanged, InstanceRemoved);
            Clusters.ForEach(c => c.AfterLoad());
        }

        /// <summary>
        /// The default connection string to use when connecting to servers, $ServerName$ will be parameterized
        /// </summary>
        public string DefaultConnectionString { get; set; }

        public class Cluster : IAfterLoadActions, ISettingsCollectionItem<Cluster>
        {
            public ObservableCollection<Instance> Nodes { get; set; }
            public event EventHandler<Instance> NodeAdded = delegate { };
            public event EventHandler<List<Instance>> NodesChanged = delegate { };
            public event EventHandler<Instance> NodeRemoved = delegate { };

            public Cluster()
            {
                Nodes = new ObservableCollection<Instance>();
            }

            public void AfterLoad()
            {
                Nodes.AddHandlers(this, NodeAdded, NodesChanged, NodeRemoved);
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
                    hashCode = (hashCode*397) ^ (Name != null ? Name.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (Description != null ? Description.GetHashCode() : 0);
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

            public bool Equals(Instance other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Name, other.Name) && string.Equals(ConnectionString, other.ConnectionString);
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
                    return ((Name != null ? Name.GetHashCode() : 0)*397) ^ (ConnectionString != null ? ConnectionString.GetHashCode() : 0);
                }
            }
        }
    }
}
