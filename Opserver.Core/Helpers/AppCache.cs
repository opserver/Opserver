﻿using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace StackExchange.Opserver.Helpers
{
    public static class AppCache
    {
        #region IP Cache

        public static List<IPAddress> GetHostAddresses(string hostOrIp)
        {
            return Current.LocalCache.GetSet<List<IPAddress>>("host-ip-cache-" + hostOrIp, (old, ctx) =>
                {
                    try
                    {
                        return IPAddress.TryParse(hostOrIp, out IPAddress ip)
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
            return Current.LocalCache.GetSet<string>("host-name-cache-" + ip, (old, ctx) =>
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

        #endregion
    }
}