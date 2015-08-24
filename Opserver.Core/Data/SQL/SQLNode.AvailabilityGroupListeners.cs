using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLNode
    {
        private Cache<List<AvailabilityGroupListener>> _availabilityGroupListeners;
        public Cache<List<AvailabilityGroupListener>> AvailabilityGroupListeners
        {
            get
            {
                return _availabilityGroupListeners ?? (_availabilityGroupListeners = new Cache<List<AvailabilityGroupListener>>
                {
                    CacheForSeconds = Cluster.RefreshInterval,
                    UpdateCache = UpdateFromSql("AvailabilityGroups", async conn =>
                        {
                            var sql = GetFetchSQL<AvailabilityGroupListener>() + "\n" +
                                      GetFetchSQL<AvailabilityGroupLisenerIPAddress>();

                            List<AvailabilityGroupListener> listeners;
                            List<AvailabilityGroupLisenerIPAddress> listnerIPs;
                            using (var multi = await conn.QueryMultipleAsync(sql))
                            {
                                listeners = multi.Read<AvailabilityGroupListener>().ToList();
                                listnerIPs = multi.Read<AvailabilityGroupLisenerIPAddress>().ToList();
                            }
                            listeners.ForEach(l =>
                                {
                                    l.Addresses = listnerIPs.Where(la => la.ListenerId == l.ListenerId).ToList();
                                });
                            return listeners;
                        })
                });
            }
        }

        private Cache<List<AvailabilityGroupLisenerIPAddress>> _availabilityGroupLisenerIPAddresses;

        public Cache<List<AvailabilityGroupLisenerIPAddress>> AvailabilityGroupLisenerIPAddresses =>
            _availabilityGroupLisenerIPAddresses ??
            (_availabilityGroupLisenerIPAddresses =
                SqlCacheList<AvailabilityGroupLisenerIPAddress>(Cluster.RefreshInterval));

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/hh231328.aspx
        /// </summary>
        public class AvailabilityGroupListener : ISQLVersionedObject
        {
            public Version MinVersion => SQLServerVersions.SQL2012.RTM;

            public Guid GroupId { get; internal set; }
            public string ListenerId { get; internal set; }
            public string DnsName { get; internal set; }
            public int Port { get; internal set; }
            public bool IsConformant { get; internal set; }
            public string IPConfigurationString { get; internal set; }

            public List<AvailabilityGroupLisenerIPAddress> Addresses { get; internal set; }

            internal const string FetchSQL = @"
Select group_id GroupId,
       listener_id ListenerId,
       dns_name DNSName,
       port Port,
       is_conformant IsConformant,
       ip_configuration_string_from_cluster IPConfigurationString
  From sys.availability_group_listeners
";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/hh213575.aspx
        /// </summary>
        public class AvailabilityGroupLisenerIPAddress : ISQLVersionedObject
        {
            public Version MinVersion => SQLServerVersions.SQL2012.RTM;

            public string ListenerId { get; internal set; }
            public string IPAddress { get; internal set; }
            public string IPSubnetMask { get; internal set; }
            public string NetworkSubnetIP { get; internal set; }
            public int? NetworkSubnetPrefixLength { get; internal set; }
            public string NetworkSubnetIPMask { get; internal set; }
            public ListenerStates? State { get; internal set; }

            internal const string FetchSQL = @"
Select listener_id ListenerId,
       ip_address IPAddress,
       ip_subnet_mask IPSubnetMask,
       network_subnet_ip NetworkSubnetIP,
       network_subnet_prefix_length NetworkSubnetPrefixLength,
       network_subnet_ipv4_mask NetworkSubnetIPMask,
       state State
  From sys.availability_group_listener_ip_addresses
";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }
    }
}