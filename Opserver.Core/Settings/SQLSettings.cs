using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public class SQLSettings : Settings<SQLSettings>
    {
        public override bool Enabled => Clusters.Any() || Instances.Any();

        public List<Cluster> Clusters { get; set; } = new List<Cluster>();

        public List<Instance> Instances { get; set; } = new List<Instance>();
        
        /// <summary>
        /// How many seconds before polling a node or cluster for status again
        /// If specified at the node or cluster level, that setting overrides
        /// </summary>
        public int RefreshIntervalSeconds { get; } = 60;
        
        /// <summary>
        /// The default connection string to use when connecting to servers, $ServerName$ will be parameterized
        /// </summary>
        public string DefaultConnectionString { get; set; }

        public class Cluster : ISettingsCollectionItem
        {
            public List<Instance> Nodes { get; set; } = new List<Instance>();
            
            /// <summary>
            /// The machine name for this SQL cluster
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Unused currently
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// How many seconds before polling a node for status again
            /// </summary>
            public int? RefreshIntervalSeconds { get; set; }
        }

        public class Instance : ISettingsCollectionItem
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

            /// <summary>
            /// How many seconds before polling this node for status again
            /// </summary>
            public int? RefreshIntervalSeconds { get; set; }
        }
    }
}
