using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;

namespace Opserver.Data.Cloudflare
{
    public class CloudflareResult<T> : CloudflareResult
    {
        [DataMember(Name = "result")]
        public T Result { get; set; }
    }

    public class CloudflareResult
    {
        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "messages")]
        public List<string> Messages { get; set; }

        [DataMember(Name = "errors")]
        public List<CloudflareError> Errors { get; set; }

        [DataMember(Name = "result_info")]
        public CloudflareResultInfo ResultInfo { get; set; }
    }

    public class CloudflareError
    {
        [DataMember(Name = "code")]
        public int Code { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }
    }

    public class CloudflareResultInfo
    {
        [DataMember(Name = "page")]
        public int Page { get; set; }

        [DataMember(Name = "per_page")]
        public int PerPage { get; set; }

        [DataMember(Name = "count")]
        public int Count { get; set; }

        [DataMember(Name = "total_count")]
        public int TotalCount { get; set; }
    }

    public class CloudflareZone : IMonitorStatus
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "status")]
        public ZoneStatus Status { get; set; }

        [DataMember(Name = "paused")]
        public bool Paused { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "development_mode")]
        public int DevelopmentMode { get; set; }

        [DataMember(Name = "name_servers")]
        public List<string> NameServers { get; set; }

        [DataMember(Name = "original_name_servers")]
        public List<string> OriginalNameServers { get; set; }

        [DataMember(Name = "original_registrar")]
        public string OriginalRegistrar { get; set; }

        [DataMember(Name = "original_dnshost")]
        public string OriginalDNSHost { get; set; }

        [DataMember(Name = "created_on")]
        public DateTimeOffset? CreatedOn { get; set; }

        [DataMember(Name = "modified_on")]
        public DateTimeOffset? ModifiedOn { get; set; }

        [DataMember(Name = "vanity_name_servers")]
        public List<string> VanityNameServers { get; set; }

        [DataMember(Name = "permissions")]
        public List<string> Permissons { get; set; }

        // TODO: Meta:
        //"meta": {
        //    "step": 4,
        //    "wildcard_proxiable": true,
        //    "custom_certificate_quota": "1",
        //    "page_rule_quota": "100",
        //    "phishing_detected": false,
        //    "multiple_railguns_allowed": false
        //}
        // TODO: Owner:
        //"owner": {
        //  "type": "organization",
        //  "id": "f510c13207fc4c7d068e3896525995c9",
        //  "name": "StackExchange"
        //}

        public MonitorStatus MonitorStatus
        {
            get
            {
                switch (Status)
                {
                    case ZoneStatus.Active:
                        return MonitorStatus.Good;
                    case ZoneStatus.Pending:
                    case ZoneStatus.Initializing:
                    case ZoneStatus.Moved:
                        return MonitorStatus.Warning;
                    case ZoneStatus.Deleted:
                        return MonitorStatus.Critical;
                    case ZoneStatus.Deactivated:
                        return MonitorStatus.Maintenance;
                    default:
                        return MonitorStatus.Unknown;
                }
            }
        }

        public string MonitorStatusReason => "Current status: " + Status;
    }

    public enum ZoneStatus
    {
        [DataMember(Name = "active")] Active = 0,
        [DataMember(Name = "Pending")] Pending = 1,
        [DataMember(Name = "initializing")] Initializing = 2,
        [DataMember(Name = "moved")] Moved = 3,
        [DataMember(Name = "deleted")] Deleted = 4,
        [DataMember(Name = "deactivated")] Deactivated = 5
    }

    public class CloudflareDNSRecord
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "type")]
        public DNSRecordType Type { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "content")]
        public string Content { get; set; }
        [DataMember(Name = "proxiable")]
        public bool Proxiable { get; set; }
        [DataMember(Name = "proxied")]
        public bool Proxied { get; set; }
        [DataMember(Name = "ttl")]
        public int TTL { get; set; }
        [DataMember(Name = "locked")]
        public bool Locked { get; set; }
        [DataMember(Name = "zone_id")]
        public string ZoneId { get; set; }
        [DataMember(Name = "zone_name")]
        public string ZoneName { get; set; }
        [DataMember(Name = "created_on")]
        public DateTimeOffset CreatedOn { get; set; }
        [DataMember(Name = "modified_on")]
        public DateTimeOffset ModifiedOn { get; set; }
        [DataMember(Name = "data")]
        public object Data { get; set; }

        public bool IsAutoTTL => TTL == 1; // 1 is auto in Cloudflare land

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
    }

    public enum DNSRecordType
    {
        A = 0,
        AAAA = 1,
        CNAME = 2,
        TXT = 3,
        SRV = 4,
        LOC = 5,
        MX = 6,
        NS = 7,
        SPF = 8
    }
}
