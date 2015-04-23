using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StackExchange.Opserver.Data.CloudFlare
{
    public partial class CloudFlareAPI
    {
        private Cache<List<Zone>> _zones;
        public Cache<List<Zone>> Zones
        {
            get
            {
                return _zones ?? (_zones = new Cache<List<Zone>>
                {
                    CacheForSeconds = 60 * 60,
                    UpdateCache = GetFromCloudFlare(
                        opName: "Fetch Zones",
                        getFromConnection: api => api.GetFromCloudFlare("zone_load_multi", getFromJson:
                            response =>
                            {
                                var parsedResponse = JObject.Parse(response);
                                var zoneObjects = parsedResponse["response"]["zones"]["objs"].Children().ToList();
                                return
                                    zoneObjects.Select(z => JsonConvert.DeserializeObject<Zone>(z.ToString())).ToList();
                            }))
                });
            }
        }
        
        private Cache<List<DNSRecord>> _dnsRecords;
        public Cache<List<DNSRecord>> DNSRecords
        {
            get
            {
                return _dnsRecords ?? (_dnsRecords = new Cache<List<DNSRecord>>
                {
                    CacheForSeconds = 60*60,
                    UpdateCache = GetFromCloudFlare(
                        opName: "Fetch DNSRecords",
                        getFromConnection: api =>
                        {
                            var records = new List<DNSRecord>();
                            foreach (var z in Zones.SafeData(true))
                            {
                                // dirty hack to populate both!
                                var dnsRecords = api.GetFromCloudFlare("rec_load_all", zone: z.ZoneName, getFromJson:
                                    response =>
                                    {
                                        var parsedResponse = JObject.Parse(response);
                                        var recordObjects =
                                            parsedResponse["response"]["recs"]["objs"].Children().ToList();
                                        return
                                            recordObjects.Select(
                                                r => JsonConvert.DeserializeObject<DNSRecord>(r.ToString())).ToList();
                                    });
                                records.AddRange(dnsRecords);
                            }
                            return records;
                        })
                });
            }
        }

        public Zone GetZoneFromHost(string host)
        {
            return Zones.SafeData(true).FirstOrDefault(z => host.EndsWith(z.ZoneName));
        }

        public Zone GetZoneFromUrl(string url)
        {
            Uri uri;
            return !Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri) ? null : GetZoneFromHost(uri.Host);
        }

        /// <summary>
        /// Get the IP Addresses for a given fully qualified host (star records not supported), even through CNAME chains
        /// </summary>
        /// <param name="host">The host to get an IP for</param>
        /// <returns>Root IP Addresses for this host</returns>
        public List<IPAddress> GetIPs(string host)
        {
            if (DNSRecords.Data == null) 
                return null;
            var records = DNSRecords.Data.Where(r => string.Equals(host, r.FullyQualified, StringComparison.InvariantCultureIgnoreCase) && (r.Type == "A" || r.Type == "AAAA" || r.Type == "CNAME")).ToList();
            var cNameRecord = records.FirstOrDefault(r => r.Type == "CNAME");
            return cNameRecord != null ? GetIPs(cNameRecord.Content) : records.Select(r => r.IPAddress).ToList();
        }

        public class Zone
        {
            [JsonProperty("zone_id")]
            public string ZoneId { get; set; }

            [JsonProperty("user_id")]
            public string UserId { get; set; }

            [JsonProperty("zone_name")]
            public string ZoneName { get; set; }

            [JsonProperty("zone_status")]
            public string ZoneStatus { get; set; }

            [JsonProperty("zone_mode")]
            public string ZoneMode { get; set; }

            [JsonProperty("host_id")]
            public string HostId { get; set; }

            [JsonProperty("zone_type")]
            public string ZoneType { get; set; }

            [JsonProperty("fqdns")]
            public List<string> DNSServers { get; set; }

            [JsonProperty("zone_status_desc")]
            public string ZoneStatusDescription { get; set; }

            [JsonProperty("orig_registrar")]
            public string OriginRegistrar { get; set; }

            [JsonProperty("orig_dnshost")]
            public string OriginDNSHost { get; set; }

            [JsonProperty("orig_ns_names")]
            public string OriginNameservers { get; set; }

            [JsonProperty("allow")]
            public List<string> Allow { get; set; }

            [JsonProperty("ns_vanity_map"), JsonConverter(typeof(CloudFlareBeingRetardedJsonConverter<Dictionary<string, string>>))]
            public Dictionary<string, string> NameServerVanityMap { get; set; }
            
            public bool IsGood
            {
                get { return ZoneStatus == "V"; }
            }

            public List<DNSRecord> DNSRecords
            {
                get { return CloudFlareAPI.Instance.DNSRecords.SafeData(true).Where(r => r.ZoneName == ZoneName).ToList(); }
            }
        }

        public class DNSRecord
        {
            [JsonProperty("rec_id")]
            public string RecordId { get; set; }

            [JsonProperty("rec_tag")]
            public string RecordTag { get; set; }

            [JsonProperty("zone_name")]
            public string ZoneName { get; set; }
            
            [JsonProperty("display_name")]
            public string DisplayName { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("content")]
            public string Content { get; set; }

            [JsonProperty("ttl")]
            public string TTL { get; set; }

            [JsonProperty("ttl_ceil")]
            public long TTLCeiling { get; set; }

            [JsonProperty("ssl_id")]
            public string SSLId { get; set; }

            [JsonProperty("ssl_status")]
            public string SSLStatus { get; set; }

            [JsonProperty("auto_ttl")]
            public bool AutoTTL { get; set; }

            [JsonProperty("service_mode")]
            public string ServiceMode { get; set; }

            [JsonProperty("props")]
            public Dictionary<string, bool> Properties { get; set; }

            private string _fullyQualified;

            public string FullyQualified
            {
                get { return _fullyQualified ?? (_fullyQualified = DisplayName + (DisplayName != ZoneName ? "." + ZoneName : "")); }
            }

            private IPAddress _ipAddress;
            public IPAddress IPAddress
            {
                get
                {
                    if (_ipAddress == null && !IPAddress.TryParse(Content, out _ipAddress))
                        _ipAddress = IPAddress.None;
                    return _ipAddress;
                }
            }

            public List<IPAddress> RootIPAddresses
            {
                get
                {
                    switch (Type)
                    {
                        case "A":
                        case "AAAA":
                            return new List<IPAddress> {IPAddress};
                        case "CNAME":
                            return CloudFlareAPI.Instance.GetIPs(Content);
                        default:
                            return null;
                    }
                }
            }

            public bool IsCloudOn
            {
                get
                {
                    bool enabled;
                    return Properties != null && Properties.TryGetValue("cloud_on", out enabled) && enabled;
                }
            }
        }
    }

    internal class CloudFlareBeingRetardedJsonConverter<T> : JsonConverter where T: new()
    {
        public override bool CanConvert(Type objectType) { return true; }
        public override bool CanRead { get { return true; } }
        public override bool CanWrite { get { return false; } }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var d = JToken.Load(reader) as JObject;
            return d != null ? d.ToObject<T>() : new T();
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { throw new NotImplementedException(); }
    }
}
