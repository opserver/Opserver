using System;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLNode
    {
        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/hh213575.aspx
        /// </summary>
        public class AGLisenerIPAddress : ISQLVersioned, IMonitorStatus
        {
            public Version MinVersion => SQLServerVersions.SQL2012.RTM;

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
            public IPNet IPNet => _ipNet ?? (_ipNet = IPNet.Parse(IPAddress, NetworkSubnetPrefixLength));

            private IPNet _networkIPNet;
            public IPNet NetworkIPNet => _networkIPNet ?? (_networkIPNet = IPNet.Parse(NetworkSubnetIP, NetworkSubnetPrefixLength));

            public MonitorStatus MonitorStatus
            {
                get
                {
                    switch (State)
                    {
                        case ListenerStates.Online:
                            return MonitorStatus.Good;
                        case ListenerStates.OnlinePending:
                            return MonitorStatus.Warning;
                        case ListenerStates.Failed:
                            return MonitorStatus.Critical;
                        //case ListenerStates.Offline:
                        default:
                            return MonitorStatus.Unknown;
                    }
                }
            }

            public string MonitorStatusReason => State.ToString();

            public string GetFetchSQL(Version v) => @"
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
