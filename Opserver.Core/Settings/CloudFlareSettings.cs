using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public class CloudFlareSettings : Settings<CloudFlareSettings>
    {
        public override bool Enabled => Email.HasValue() && APIKey.HasValue();
        public List<DataCenter> DataCenters { get; set; } = new List<DataCenter>();

        /// <summary>
        /// Email for the CloudFlare account
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// APIKey for the CloudFlare account
        /// </summary>
        public string APIKey { get; set; }

        public class DataCenter : ISettingsCollectionItem
        {
            /// <summary>
            /// The name for this data center
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The IP ranges for this data center, in CIDR format
            /// </summary>
            public List<string> Ranges { get; set; } = new List<string>();

            /// <summary>
            /// The masked IP ranges for this data center, in CIDR format
            /// </summary>
            public List<string> MaskedRanges { get; set; } = new List<string>();
        }
    }
}
