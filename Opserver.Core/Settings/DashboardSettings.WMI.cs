using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public class WMISettings : IProviderSettings
    {
        public bool Enabled => Nodes.Any();
        public string Name => "WMI";

        /// <summary>
        /// List of hostnames to poll via WMI
        /// </summary>
        public List<string> Nodes { get; set; }

        /// <summary>
        /// Timeout in seconds for cache with more or less static data, like node name or volume size.
        /// </summary>
        public int StaticDataTimeoutSeconds { get; set; }

        /// <summary>
        /// Timeout in seconds for dynamic data like CPU load.
        /// </summary>
        public int DynamicDataTimeoutSeconds { get; set; }

        /// <summary>
        /// Amount of history to keep
        /// </summary>
        public int HistoryHours { get; set; }

        /// <summary>
        /// Username to use when polling (for non-domain testing)
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Password to use when polling (for non-domain testing)
        /// </summary>
        public string Password { get; set; }

        public WMISettings()
        {
            Nodes = new List<string>();
            StaticDataTimeoutSeconds = 60 * 5;
            DynamicDataTimeoutSeconds = 5;
            HistoryHours = 2;
        }

        public void Normalize() {}
    }
}
