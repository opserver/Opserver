using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace StackExchange.Opserver.Data.CloudFlare
{
    public class DataCenters
    {
        private static Dictionary<string, List<IPNet>> _allDCs;

        public static Dictionary<string, List<IPNet>> All
        {
            get
            {
                return _allDCs ?? (_allDCs = Current.Settings.CloudFlare != null
                    ? Current.Settings.CloudFlare.DataCenters
                        .ToDictionary(
                            dc => dc.Name,
                            dc => dc.Ranges.Concat(dc.MaskedRanges).Select(r => IPNet.Parse(r)).ToList()
                        )
                    : new Dictionary<string, List<IPNet>>());
            }
        }

        private static List<IPNet> _maskedRanges;

        public static List<IPNet> MaskedRanges
        {
            get
            {
                return _maskedRanges ?? (_maskedRanges = Current.Settings.CloudFlare != null
                    ? Current.Settings.CloudFlare.DataCenters.SelectMany(dc => dc.MaskedRanges)
                        .Select(r => IPNet.Parse(r))
                        .ToList()
                    : new List<IPNet>());
            }
        }

        public static IEnumerable<string> GetMasked(List<IPAddress> addresses)
        {
            foreach (var ip in addresses)
            {
                string masked;
                if (_cached.TryGetValue(ip, out masked)) yield return masked;
                else yield return _cached[ip] = GetMasked(ip);
            }
        }

        private static readonly ConcurrentDictionary<IPAddress, string> _cached = new ConcurrentDictionary<IPAddress, string>();

        private static string GetMasked(IPAddress address)
        {
            foreach (var mr in MaskedRanges)
            {
                if (!mr.Contains(address)) continue;

                var bytes = address.GetAddressBytes();

                if (mr.CIDR >= 24)
                    return "*.*.*." + bytes[3];
                if (mr.CIDR >= 16)
                    return "*.*." + bytes[2] + "." + bytes[3];
                if (mr.CIDR >= 8)
                    return "*." + bytes[1] + "." + bytes[2] + "." + bytes[3];

                return address.ToString();
            }
            return address.ToString();
        }
    }
}
