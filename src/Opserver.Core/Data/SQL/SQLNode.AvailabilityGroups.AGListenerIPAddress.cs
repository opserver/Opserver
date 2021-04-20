using System;
using System.Collections.Generic;

namespace Opserver.Data.SQL
{
    public partial class SQLNode
    {
        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/hh213575.aspx
        /// </summary>
        public class AGListenerIPAddress : ISQLVersioned, IMonitorStatus
        {
            Version IMinVersioned.MinVersion => SQLServerVersions.SQL2012.RTM;
            ISet<SQLServerEdition> ISQLVersioned.SupportedEditions => SQLServerEditions.All;

            public string ListenerId { get; internal set; }
            public string IPAddress { get; internal set; }
            public string IPSubnetMask { get; internal set; }
            public string NetworkSubnetIP { get; internal set; }
            public int NetworkSubnetPrefixLength { get; internal set; }
            public string NetworkSubnetIPMask { get; internal set; }
            public ListenerStates? State { get; internal set; }

            // TODO: IsLocal based on ClusterNetworks where MemberName = Server Name 
            // and ClusterNetwork bits match here ()

            private IPNet _ipNet;
            public IPNet IPNet => _ipNet ??= IPNet.Parse(IPAddress, (byte)NetworkSubnetPrefixLength);

            private IPNet _networkIPNet;
            public IPNet NetworkIPNet => _networkIPNet ??= IPNet.Parse(NetworkSubnetIP, (byte)NetworkSubnetPrefixLength);

            public MonitorStatus MonitorStatus =>
                State switch
                {
                    ListenerStates.Online => MonitorStatus.Good,
                    ListenerStates.OnlinePending => MonitorStatus.Warning,
                    ListenerStates.Failed => MonitorStatus.Critical,
                    //case ListenerStates.Offline:
                    _ => MonitorStatus.Unknown,
                };

            public string MonitorStatusReason => State.ToString();

            public string GetFetchSQL(in SQLServerEngine e) => @"
Select listener_id ListenerId,
       ip_address IPAddress,
       ip_subnet_mask IPSubnetMask,
       network_subnet_ip NetworkSubnetIP,
       network_subnet_prefix_length NetworkSubnetPrefixLength,
       network_subnet_ipv4_mask NetworkSubnetIPMask,
       state State
  From sys.availability_group_listener_ip_addresses;";
        }
    }
}
