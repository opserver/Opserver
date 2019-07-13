using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Opserver.Helpers
{
    public static class AppCache
    {
        public static List<IPAddress> GetHostAddresses(string hostOrIp)
        {
            return Current.LocalCache.GetSet<List<IPAddress>>("host-ip-cache-" + hostOrIp, (_, __) =>
                {
                    try
                    {
                        return IPAddress.TryParse(hostOrIp, out var ip)
                                   ? new List<IPAddress> { ip }
                                   : Dns.GetHostAddresses(hostOrIp).ToList();
                    }
                    catch
                    {
                        return new List<IPAddress>();
                    }
                }, 10.Hours(), 24.Hours());
        }

        public static string GetHostName(string ip)
        {
            return Current.LocalCache.GetSet<string>("host-name-cache-" + ip, (_, __) =>
            {
                try
                {
                    var entry = Dns.GetHostEntry(ip);
                    return entry != null ? entry.HostName.Split(StringSplits.Period)[0] : "Unknown";
                }
                catch
                {
                    return "Unknown";
                }
            }, 10.Minutes(), 24.Hours());
        }
    }
}
