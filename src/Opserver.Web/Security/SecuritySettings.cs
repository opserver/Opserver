using System.Collections.Generic;

namespace Opserver.Security
{
    public class SecuritySettings
    {
        /// <summary>
        /// Security Provider to use, e.g. "ActiveDirectory"
        /// </summary>
        public string Provider { get; set; }
        public string Server { get; set; }
        public string AuthUser { get; set; }
        public string AuthPassword { get; set; }
        public string ApiKey { get; set; }
        public List<Network> InternalNetworks { get; set; }

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
