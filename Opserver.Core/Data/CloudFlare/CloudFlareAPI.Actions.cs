using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver.Data.CloudFlare
{
    public partial class CloudFlareAPI
    {
        public void PurgeAllFiles(Zone zone)
        {
            if (zone == null) return;

            var nvc = new NameValueCollection {{"v", "1"}};

            GetFromCloudFlare("fpurge_ts", CheckForSuccess, zone.ZoneName, nvc);
        }

        public void PurgeFile(string url)
        {
            var zone = GetZoneFromUrl(url);
            if (zone == null) return;

            var otherUrl = url.StartsWith("http:")
                ? Regex.Replace(url, "^http:", "https:")
                : Regex.Replace(url, "^https:", "http:");

            PurgeFile(zone, url);
            PurgeFile(zone, otherUrl);
        }

        private bool PurgeFile(Zone zone, string url)
        {
            var nvc = new NameValueCollection { { "url", url } };

            return GetFromCloudFlare("zone_file_purge", CheckForSuccess, zone.ZoneName, nvc);
        }
    }
}
