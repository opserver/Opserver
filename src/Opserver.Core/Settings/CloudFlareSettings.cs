using System.Collections.Generic;
using Opserver.Data.Cloudflare;

namespace Opserver
{
    public class CloudflareSettings : ModuleSettings
    {
        public override bool Enabled => Email.HasValue() && APIKey.HasValue();
        public override string AdminRole => CloudflareRoles.Admin;
        public override string ViewRole => CloudflareRoles.Viewer;
        public List<DataCenter> DataCenters { get; set; } = new List<DataCenter>();

        /// <summary>
        /// Email for the Cloudflare account
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// APIKey for the Cloudflare account
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
