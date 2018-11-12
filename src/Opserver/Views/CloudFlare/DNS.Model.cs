using System.Collections.Generic;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Data.CloudFlare;

namespace StackExchange.Opserver.Views.CloudFlare
{
    public class DNSModel : DashboardModel
    {
        public override Views View => Views.DNS;

        public Dictionary<string, List<IPNet>> DataCenters { get; set; }
        public List<CloudFlareZone> Zones { get; set; }
        public List<CloudFlareDNSRecord> DNSRecords { get; set; }
    }
}