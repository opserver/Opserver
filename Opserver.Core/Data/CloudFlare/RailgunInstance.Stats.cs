using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.CloudFlare
{
    public partial class RailgunInstance
    {
        private Cache<string> _stats;
        public Cache<string> Stats => _stats ?? (_stats = new Cache<string>
        {
            CacheForSeconds = 30,
            UpdateCache = GetFromRailgun("Stats", GetStats)
        });

        private async Task<string> GetStats(RailgunInstance i)
        {
            string json;
            var req = (HttpWebRequest) WebRequest.Create($"http://{i.Host}:{i.Port}/");
            using (var resp = await req.GetResponseAsync())
            using (var rs = resp.GetResponseStream())
            {
                if (rs == null) return null;
                using (var sr = new StreamReader(rs))
                {
                    json = sr.ReadToEnd();
                }
            }
            return json;
        }
    }
}
