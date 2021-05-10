using System.Collections.Generic;
using Opserver.Data.SQL;

namespace Opserver
{
    public class SQLSettings : ModuleSettings
    {
        public override bool Enabled => Clusters.Count > 0 || Instances.Count > 0;
        public override string AdminRole => SQLRoles.Admin;
        public override string ViewRole => SQLRoles.Viewer;

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
            /// The machine name for this SQL cluster.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Description for the cluster, shown in the UI.
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// How many seconds before polling a node for status again.
            /// </summary>
            public int? RefreshIntervalSeconds { get; set; }
        }

        public class Instance : ISettingsCollectionItem
        {
            /// <summary>
            /// The machine name for this SQL instance.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Description for the cluster, shown in the UI.
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// Connection string for this instance.
            /// </summary>
            public string ConnectionString { get; set; }

            /// <summary>
            /// Object Name for this instance.
            /// </summary>
            public string ObjectName { get; set; }

            /// <summary>
            /// How many seconds before polling this node for status again.
            /// </summary>
            public int? RefreshIntervalSeconds { get; set; }

            /// <summary>
            /// Gets or sets the <see cref="InstanceType"/>. When this is <see cref="InstanceType.Azure"/>
            /// the list of databases is looked up at runtime and used to generate a list of <see cref="SQLInstance"/>
            /// objects representing each database.
            /// </summary>
            public InstanceType Type { get; set; } = InstanceType.Default;
        }

        public enum InstanceType
        {
            Default = 0,
            Azure = 1,
        }
    }
}
