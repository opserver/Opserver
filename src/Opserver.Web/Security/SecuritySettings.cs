using System.Collections.Generic;

namespace Opserver.Security
{
    /// <summary>
    /// Default security settings for configuring a security provider that doesn't
    /// need any additional settings.
    /// </summary>
    public class SecuritySettings
    {
        /// <summary>
        /// Gets or sets the name of the Security Provider in the UI.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the Security Provider to use, e.g. "ActiveDirectory", "OIDC", etc.
        /// </summary>
        public string Provider { get; set; }
        /// <summary>
        /// Gets or sets an API key used to allow access to the Opserver API.
        /// </summary>
        public string ApiKey { get; set; }
        /// <summary>
        /// Gets or sets a list of <see cref="Network"/> objects representing the internal
        /// subnets that Opserver is monitoring.
        /// </summary>
        public List<Network> InternalNetworks { get; set; }

        /// <summary>
        /// Semilcolon delimited list of security groups that can see everything, but not perform actions
        /// </summary>
        public string ViewEverythingGroups { get; set; }

        /// <summary>
        /// Semilcolon delimited list of security groups that can do anything, including management actions
        /// </summary>
        public string AdminEverythingGroups { get; set; }

        public class Network
        {
            /// <summary>
            /// The name for this network.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The CIDR notation for this node
            /// </summary>
            public string CIDR { get; set; }

            /// <summary>
            /// The IP for this network, optionally used with Mask
            /// </summary>
            public string IP { get; set; }

            /// <summary>
            /// The subnet mask for this node
            /// </summary>
            public string Subnet { get; set; }
        }
    }
}
