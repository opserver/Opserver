using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public class CloudFlareSettings : Settings<CloudFlareSettings>
    {
        public override bool Enabled => (Email.HasValue() && APIKey.HasValue()) || Railguns.Any();
        public List<Railgun> Railguns { get; set; } = new List<Railgun>();
        public List<DataCenter> DataCenters { get; set; } = new List<DataCenter>();

        /// <summary>
        /// Email for the CloudFlare account
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// APIKey for the CloudFlare account
        /// </summary>
        public string APIKey { get; set; }

        public class Railgun : ISettingsCollectionItem
        {
            /// <summary>
            /// The name of this railgun instance
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Host for this instance
            /// </summary>
            public string Host { get; set; }

            /// <summary>
            /// Connection port for this instance
            /// </summary>
            public int Port { get; set; } = 24088;

            /// <summary>
            /// Unused currently
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// How many seconds before polling the railgun status endpoint for status again
            /// </summary>
            public int RefreshIntervalSeconds { get; set; } = 30;
        }
        
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
