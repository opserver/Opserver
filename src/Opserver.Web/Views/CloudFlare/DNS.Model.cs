using System.Collections.Generic;
using Opserver.Data;
using Opserver.Data.Cloudflare;

namespace Opserver.Views.Cloudflare
{
    public class DNSModel : DashboardModel
    {
        public override Views View => Views.DNS;

        public Dictionary<string, List<IPNet>> DataCenters { get; set; }
        public List<CloudflareZone> Zones { get; set; }
        public List<CloudflareDNSRecord> DNSRecords { get; set; }
    }
}