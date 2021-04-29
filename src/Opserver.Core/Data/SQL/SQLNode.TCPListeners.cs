using System;
using System.Collections.Generic;
using EnumsNET;

namespace Opserver.Data.SQL
{
    public partial class SQLNode
    {
        private Cache<List<TCPListenerState>> _tcpListeners;

        public Cache<List<TCPListenerState>> TCPListeners =>
            _tcpListeners ??= SqlCacheList<TCPListenerState>(Cluster.RefreshInterval);

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/hh245287.aspx
        /// </summary>
        public class TCPListenerState : ISQLVersioned, IMonitorStatus
        {
            Version IMinVersioned.MinVersion => SQLServerVersions.SQL2012.RTM;
            SQLServerEditions ISQLVersioned.SupportedEditions => SQLServerEditions.All;

            public MonitorStatus MonitorStatus =>
                State switch
                {
                    TCPListenerStates.Online => MonitorStatus.Good,
                    TCPListenerStates.PendingRestart => MonitorStatus.Maintenance,
                    _ => MonitorStatus.Unknown,
                };

            public string MonitorStatusReason => State == TCPListenerStates.Online ? null : State.AsString(EnumFormat.Description);

            public int ListenerId { get; internal set; }
            public string IPAddress { get; internal set; }
            public bool IsIPV4 { get; internal set; }
            public int Port { get; internal set; }
            public TCPListenerTypes Type { get; internal set; }
            public TCPListenerStates State { get; internal set; }
            public DateTime StartTime { get; internal set; }

            public string GetFetchSQL(in SQLServerEngine e) => @"
select listener_id ListenerId,
       ip_address IPAddress,
       is_ipv4 IsIPV4,
       port Port,
       type Type,
       state State,
       start_time StartTime
from sys.dm_tcp_listener_states";
        }
    }
}
