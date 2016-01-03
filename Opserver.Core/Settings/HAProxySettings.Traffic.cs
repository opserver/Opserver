using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public partial class HAProxySettings
    {
        public HAProxyTrafficSettings Traffic { get; set; } = new HAProxyTrafficSettings();

        public class HAProxyTrafficSettings
        {
            public bool Enabled => Connections.Any();

            /// <summary>
            /// Connections for
            /// </summary>
            public List<Connection> Connections { get; set; } = new List<Connection>();
        }

        public class Connection : ISettingsCollectionItem
        {
            /// <summary>
            /// The name that appears for this instance
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The connection string for this instance
            /// </summary>
            public string ConnectionString { get; set; }
        }
    }
}