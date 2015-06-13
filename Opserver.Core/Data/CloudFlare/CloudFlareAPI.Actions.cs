using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Jil;

namespace StackExchange.Opserver.Data.CloudFlare
{
    public partial class CloudFlareAPI
    {
        public bool PurgeFile(string url)
        {
            var zone = GetZoneFromUrl(url);
            if (zone == null) return false;

            var otherUrl = url.StartsWith("http:")
                ? Regex.Replace(url, "^http:", "https:")
                : Regex.Replace(url, "^https:", "http:");

            return zone.PurgeFiles(new[] {url, otherUrl});
        }
    }

    public static partial class ZoneExtensions
    {
        private static readonly NameValueCollection _purgeAllParams = new NameValueCollection
        {
            {"purge_everything", "true"}
        };

        public static bool PurgeAllFiles(this CloudFlareZone zone)
        {
            var result = CloudFlareAPI.Instance.Post<CloudFlareResult>($"zones/{zone.Id}/purge_cache", _purgeAllParams);
            return result.Success;
        }
        public static bool PurgeFiles(this CloudFlareZone zone, IEnumerable<string> files)
        {
            var nvc = new NameValueCollection {{"files", JSON.Serialize(files)}};

            var result = CloudFlareAPI.Instance.Delete<CloudFlareResult>($"zones/{zone.Id}/purge_cache", nvc);
            return result.Success;
        }
    }
}
