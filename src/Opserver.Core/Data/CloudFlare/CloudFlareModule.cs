using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.Extensions.Options;

namespace StackExchange.Opserver.Data.CloudFlare
{
    public class CloudFlareModule : StatusModule<CloudFlareSettings>
    {
        public override bool Enabled => Settings.Enabled;

        public CloudFlareAPI API { get; }
        public Dictionary<string, List<IPNet>> AllDatacenters { get; }

        public CloudFlareModule(IOptions<CloudFlareSettings> settings) : base(settings)
        {
            API = new CloudFlareAPI(this);
            API.TryAddToGlobalPollers();
            AllDatacenters = settings.Value?.DataCenters
                        .ToDictionary(
                            dc => dc.Name,
                            dc => dc.Ranges.Concat(dc.MaskedRanges).Select(r => IPNet.Parse(r)).ToList()
                        ) ?? new Dictionary<string, List<IPNet>>();
        }

        public override MonitorStatus MonitorStatus => API.MonitorStatus;
        public override bool IsMember(string node) => false;

        public List<CloudFlareDNSRecord> GetDNSRecords(CloudFlareZone zone) =>
            API.DNSRecords.Data?.Where(r => r.ZoneId == zone.Id).ToList() ?? new List<CloudFlareDNSRecord>();

        private static readonly NameValueCollection _purgeAllParams = new NameValueCollection
        {
            ["purge_everything"] = "true"
        };

        public bool PurgeAllFiles(CloudFlareZone zone)
        {
            var result = API.Post<CloudFlareResult>($"zones/{zone.Id}/purge_cache", _purgeAllParams);
            return result.Success;
        }

        public bool PurgeFiles(CloudFlareZone zone, IEnumerable<string> files)
        {
            var nvc = new NameValueCollection { ["files"] = Jil.JSON.Serialize(files) };
            var result = API.Delete<CloudFlareResult>($"zones/{zone.Id}/purge_cache", nvc);
            return result.Success;
        }
    }
}
