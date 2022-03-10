using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Opserver.Data.Cloudflare
{
    public partial class CloudflareAPI
    {
        public Task<bool> PurgeFileAsync(string url)
        {
            var zone = GetZoneFromUrl(url);
            if (zone == null) return Task.FromResult(false);

            var otherUrl = url.StartsWith("http:")
                ? Regex.Replace(url, "^http:", "https:")
                : Regex.Replace(url, "^https:", "http:");

            return Module.PurgeFilesAsync(zone, new[] {url, otherUrl});
        }
    }
}
