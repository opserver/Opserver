using System.IO;
using System.Net;

namespace StackExchange.Opserver.Data.CloudFlare
{
    public partial class RailgunInstance
    {
        private Cache<string> _stats;
        public Cache<string> Stats
        {
            get
            {
                return _stats ?? (_stats = new Cache<string>
                {
                    CacheForSeconds = 30,
                    UpdateCache = GetFromRailgun("Stats", GetStats)
                });
            }
        }

        private string GetStats(RailgunInstance i)
        {
            string json;
            var req = (HttpWebRequest) WebRequest.Create(string.Format("http://{0}:{1}/", i.Host, i.Port));
            using (var resp = req.GetResponse())
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
