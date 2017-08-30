using System;
using System.Collections.Generic;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLNode
    {
        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/hh231328.aspx
        /// </summary>
        public class AGListener : ISQLVersioned
        {
            public Version MinVersion => SQLServerVersions.SQL2012.RTM;

            public Guid GroupId { get; internal set; }
            public string ListenerId { get; internal set; }
            public string DnsName { get; internal set; }
            public int Port { get; internal set; }
            public bool IsConformant { get; internal set; }
            public string IPConfigurationString { get; internal set; }

            public List<AGLisenerIPAddress> Addresses { get; internal set; }
            public List<AGLisenerIPAddress> LocalAddresses { get; internal set; }

            public string GetFetchSQL(Version v) => @"
Select group_id GroupId,
       listener_id ListenerId,
       dns_name DNSName,
       port Port,
       is_conformant IsConformant,
       ip_configuration_string_from_cluster IPConfigurationString
  From sys.availability_group_listeners;";
        }
    }
}
