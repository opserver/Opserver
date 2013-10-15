using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StackExchange.Opserver
{
    public class ElasticSettings : Settings<ElasticSettings>, IAfterLoadActions
    {
        public override bool Enabled { get { return Clusters.Any(); } }

        /// <summary>
        /// elastic search clusters to monitor
        /// </summary>
        public ObservableCollection<Cluster> Clusters { get; set; }
        public event EventHandler<Cluster> ClusterAdded = delegate { };
        public event EventHandler<List<Cluster>> ClustersChanged = delegate { };
        public event EventHandler<Cluster> ClusterRemoved = delegate { };

        public ElasticSettings()
        {
            Clusters = new ObservableCollection<Cluster>();
        }

        public void AfterLoad()
        {
            Clusters.AddHandlers(this, ClusterAdded, ClustersChanged, ClusterRemoved);
            Clusters.ForEach(c => c.AfterLoad());
        }

        public class Cluster : IAfterLoadActions, ISettingsCollectionItem<Cluster>
        {
            public Cluster()
            {
                // Defaults
                RefreshIntervalSeconds = 60;
                DownRefreshIntervalSeconds = 5;
                Nodes = new ObservableCollection<string>();
            }

            public void AfterLoad()
            {
                Nodes.AddHandlers(this, NodeAdded, NodesChanged, NodeRemoved);
            }

            /// <summary>
            /// Nodes in this cluster
            /// </summary>
            public ObservableCollection<string> Nodes { get; set; }
            public event EventHandler<string> NodeAdded = delegate { };
            public event EventHandler<List<string>> NodesChanged = delegate { };
            public event EventHandler<string> NodeRemoved = delegate { };

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
                    hashCode = (hashCode*397) ^ (Name != null ? Name.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (Description != null ? Description.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ RefreshIntervalSeconds;
                    hashCode = (hashCode*397) ^ DownRefreshIntervalSeconds;
                    return hashCode;
                }
            }
        }
    }
}
