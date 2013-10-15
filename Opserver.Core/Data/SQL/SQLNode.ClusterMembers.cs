using System;
using System.Collections.Generic;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLNode
    {
        private Cache<List<ClusterMemberInfo>> _clusterMembers;
        public Cache<List<ClusterMemberInfo>> ClusterMembers
        {
            get { return _clusterMembers ?? (_clusterMembers = SqlCacheList<ClusterMemberInfo>(Cluster.RefreshInterval)); }
        }

        public class ClusterMemberInfo : ISQLVersionedObject
        {
            public Version MinVersion { get { return SQLServerVersions.SQL2012.RTM; } }

            public string MemberName { get; internal set; }
            public ClusterMemberTypes Type { get; internal set; }
            public ClusterMemberStates State { get; internal set; }
            public int? Votes { get; internal set; }

            internal const string FetchSQL = @"
select member_name MemberName,
       member_type Type,
       member_state State,
       number_of_quorum_votes Votes
from sys.dm_hadr_cluster_members";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }
    }
}