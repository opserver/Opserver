using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StackExchange.Opserver
{
    /// <summary>
    /// Settings for WmiDataProvider are in the form:
    /// nodes["name1", "name2", ..., "nameN"]
    /// </summary>
    public class WmiProviderSettings : Settings<WmiProviderSettings>
    {
        public override bool Enabled { get { return Nodes.Any(); } }

        public List<string> Nodes { get; set; }
        
        /// <summary>
        /// Timeout in seconds for cache with more or less static data,
        /// like node name or volume size.
        /// </summary>
        public int StaticDataTimeoutSec { get; set; }
        
        /// <summary>
        /// Timeout in seconds for dynamic data like CPU load.
        /// </summary>
        public int DynamicDataTimeoutSec { get; set; }

        public int HoursToKeepHistory { get; set; }

        public WmiProviderSettings()
        {
            Nodes = new List<string>();
            StaticDataTimeoutSec = 60*5;
            DynamicDataTimeoutSec = 5;
            HoursToKeepHistory = 2;
        }
    }
}
