using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public class ElasticSettings : Settings<ElasticSettings>
    {
        public override bool Enabled => Clusters?.Any() ?? false;

        /// <summary>
        /// elastic search clusters to monitor
        /// </summary>
        public List<Cluster> Clusters { get; set; } = new List<Cluster>();

        public class Cluster : ISettingsCollectionItem
        {
            /// <summary>
            /// Nodes in this cluster
            /// </summary>
            public List<string> Nodes { get; set; } = new List<string>();

            /// <summary>
            /// The machine name for this SQL cluster
            /// </summary>
            public string Name { get; set; }

            public string Description { get; set; }

            /// <summary>
            /// How many seconds before polling this cluster for status again
            /// </summary>
            public int RefreshIntervalSeconds { get; set; } = 120;

            /// <summary>
            /// How many seconds before polling this cluster for status again, if the cluster status is not green
            /// </summary>
            public int DownRefreshIntervalSeconds { get; set; } = 10;
        }
    }
}
