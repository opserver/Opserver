using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;

namespace Opserver.Data.Cloudflare
{
    public partial class CloudflareAPI
    {
        private Cache<List<CloudflareZone>> _zones;
        public Cache<List<CloudflareZone>> Zones =>
            _zones ??= GetCloudflareCache(5.Minutes(), () => Get<List<CloudflareZone>>("zones"));

        private static readonly NameValueCollection _dnsRecordFetchParams = new NameValueCollection
        {
            ["per_page"] = "100"
        };

        private Cache<List<CloudflareDNSRecord>> _dnsRecords;

        public Cache<List<CloudflareDNSRecord>> DNSRecords =>
            _dnsRecords ??= GetCloudflareCache(5.Minutes(), async () =>
            {
                var records = new List<CloudflareDNSRecord>();
                var data = await Zones.GetData(); // wait on zones to load first...
                if (data == null) return records;
                foreach (var z in data)
                {
                    var zoneRecords =
                        await Get<List<CloudflareDNSRecord>>($"zones/{z.Id}/dns_records", _dnsRecordFetchParams);
                    records.AddRange(zoneRecords);
                }
                return records;
            });

        public CloudflareZone GetZoneFromHost(string host) =>
            Zones.Data?.FirstOrDefault(z => host.EndsWith(z.Name));

        public CloudflareZone GetZoneFromUrl(string url) =>
            !Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri) ? null : GetZoneFromHost(uri.Host);

        /// <summary>
        /// Get the IP Addresses for a given DNS record reponse.
        /// </summary>
        /// <param name="record">The DNS record to get an IP for</param>
        /// <returns>Root IP Addresses for this record.</returns>
        public List<IPAddress> GetIPs(CloudflareDNSRecord record)
        {
            switch (record.Type)
            {
                case DNSRecordType.A:
                case DNSRecordType.AAAA:
                    return new List<IPAddress> { record.IPAddress };
                case DNSRecordType.CNAME:
                    return GetIPs(record.Content);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Get the IP Addresses for a given fully qualified host (star records not supported), even through CNAME chains.
        /// </summary>
        /// <param name="host">The host to get an IP for.</param>
        /// <returns>Root IP Addresses for this host.</returns>
        public List<IPAddress> GetIPs(string host)
        {
            if (DNSRecords.Data == null)
                return null;
            var records = DNSRecords.Data.Where(r => string.Equals(host, r.Name, StringComparison.InvariantCultureIgnoreCase) && (r.Type == DNSRecordType.A || r.Type == DNSRecordType.AAAA || r.Type == DNSRecordType.CNAME)).ToList();
            var cNameRecord = records.Find(r => r.Type == DNSRecordType.CNAME);
            return cNameRecord != null ? GetIPs(cNameRecord.Content) : records.Select(r => r.IPAddress).ToList();
        }
    }
}
