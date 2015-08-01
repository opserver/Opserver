using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public partial class HAProxySettings
    {
        private HAProxyTrafficSettings _traffic;
        public HAProxyTrafficSettings Traffic
        {
            get { return _traffic ?? (_traffic = new HAProxyTrafficSettings()); }
            set
            {
                _traffic = value;
                TrafficChanged(this, value);
            }
        }
        public event EventHandler<HAProxyTrafficSettings> TrafficChanged = delegate { };

        public class HAProxyTrafficSettings
        {
            public bool Enabled => Connections.Any();

            /// <summary>
            /// Connections for
            /// </summary>
            public List<Connection> Connections { get; set; }

            public HAProxyTrafficSettings()
            {
                Connections = new List<Connection>();
            }
        }

        public class Connection : ISettingsCollectionItem<Connection>
        {
            /// <summary>
            /// The name that appears for this instance
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The connection string for this instance
            /// </summary>
            public string ConnectionString { get; set; }

            public bool Equals(Connection other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Name, other.Name)
                       && string.Equals(ConnectionString, other.ConnectionString);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Connection) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Name?.GetHashCode() ?? 0)*397)
                           ^ (ConnectionString?.GetHashCode() ?? 0);
                }
            }
        }
    }
}