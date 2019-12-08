using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Opserver.Data.Cloudflare
{
    public class CloudflareModule : StatusModule<CloudflareSettings>
    {
        public override string Name => "Cloudflare";
        public override bool Enabled => Settings.Enabled;

        public CloudflareAPI API { get; }
        public Dictionary<string, List<IPNet>> AllDatacenters { get; }

        public CloudflareModule(IConfiguration config, PollingService poller) : base(config, poller)
        {
            if (Settings.Enabled)
            {
                API = new CloudflareAPI(this);
                API.TryAddToGlobalPollers();
                AllDatacenters = Settings?.DataCenters
                            .ToDictionary(
                                dc => dc.Name,
                                dc => dc.Ranges.Concat(dc.MaskedRanges).Select(r => IPNet.Parse(r)).ToList()
                            ) ?? new Dictionary<string, List<IPNet>>();
            }
        }

        public override MonitorStatus MonitorStatus => API.MonitorStatus;
        public override bool IsMember(string node) => false;

        public List<CloudflareDNSRecord> GetDNSRecords(CloudflareZone zone) =>
            API.DNSRecords.Data?.Where(r => r.ZoneId == zone.Id).ToList() ?? new List<CloudflareDNSRecord>();

        private static readonly NameValueCollection _purgeAllParams = new NameValueCollection
        {
            ["purge_everything"] = "true"
        };

        public async Task<bool> PurgeAllFilesAsync(CloudflareZone zone)
        {
            var result = await API.PostAsync<CloudflareResult>($"zones/{zone.Id}/purge_cache", _purgeAllParams);
            return result.Success;
        }

        public async Task<bool> PurgeFilesAsync(CloudflareZone zone, IEnumerable<string> files)
        {
            var nvc = new NameValueCollection { ["files"] = Jil.JSON.Serialize(files) };
            var result = await API.DeleteAsync<CloudflareResult>($"zones/{zone.Id}/purge_cache", nvc);
            return result.Success;
        }

        private List<IPNet> _maskedRanges;
        public List<IPNet> MaskedRanges
        {
            get
            {
                return _maskedRanges ?? (_maskedRanges = Settings.DataCenters?.SelectMany(dc => dc.MaskedRanges)
                        .Select(r => IPNet.Parse(r))
                        .ToList()
                    ?? new List<IPNet>());
            }
        }

        public IEnumerable<string> GetMaskedIPs(List<IPAddress> addresses)
        {
            foreach (var ip in addresses)
            {
                if (_cached.TryGetValue(ip, out string masked)) yield return masked;
                else yield return _cached[ip] = GetMaskedIP(ip);
            }
        }

        private readonly ConcurrentDictionary<IPAddress, string> _cached = new ConcurrentDictionary<IPAddress, string>();

        private string GetMaskedIP(IPAddress address)
        {
            foreach (var mr in MaskedRanges)
            {
                if (!mr.Contains(address)) continue;

                var bytes = address.GetAddressBytes();

                if (mr.CIDR >= 24)
                    return "*.*.*." + bytes[3].ToString();
                if (mr.CIDR >= 16)
                    return "*.*." + bytes[2].ToString() + "." + bytes[3].ToString();
                if (mr.CIDR >= 8)
                    return "*." + bytes[1].ToString() + "." + bytes[2].ToString() + "." + bytes[3].ToString();

                return address.ToString();
            }
            return address.ToString();
        }
    }
}
