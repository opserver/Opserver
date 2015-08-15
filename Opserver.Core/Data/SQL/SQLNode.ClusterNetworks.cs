using System;
using System.Collections.Generic;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLNode
    {
        private Cache<List<ClusterNetworkInfo>> _clusterNetworks;
        public Cache<List<ClusterNetworkInfo>> ClusterNetworks => _clusterNetworks ?? (_clusterNetworks = SqlCacheList<ClusterNetworkInfo>(Cluster.RefreshInterval));

        public class ClusterNetworkInfo : ISQLVersionedObject
        {
            public Version MinVersion => SQLServerVersions.SQL2012.RTM;

            public string MemberName { get; internal set; }
            public string NetworkSubnetIP { get; internal set; }
            public string NetworkSubnetIPMask { get; internal set; }
            public int? NetworkSubnetPrefixLength { get; internal set; }
            public bool IsPublic { get; internal set; }
            public bool IsIPV4 { get; internal set; }

            internal const string FetchSQL = @"
select member_name MemberName,
       network_subnet_ip NetworkSubnetIP,
       network_subnet_ipv4_mask NetworkSubnetIPMask,
       network_subnet_prefix_length NetworkSubnetPrefixLength,
       is_public IsPublic,
       is_ipv4 IsIPV4
  from sys.dm_hadr_cluster_networks";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }
    }
}