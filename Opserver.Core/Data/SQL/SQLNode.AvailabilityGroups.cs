using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLNode
    {
        private Cache<List<AGInfo>> _availabilityGroups;
        public Cache<List<AGInfo>> AvailabilityGroups
        {
            get
            {
                return _availabilityGroups ?? (_availabilityGroups = new Cache<List<AGInfo>>
                {
                    CacheForSeconds = Cluster.RefreshInterval,
                    UpdateCache = UpdateFromSql(nameof(AvailabilityGroups), async conn =>
                    {
                        Func<string, string, PerfCounterRecord> getCounter = (cn, n) => GetPerfCounter("Availability Replica", cn, n);
                        var sql = GetFetchSQL<AGInfo>() + "\n" +
                                  GetFetchSQL<AGReplica>() + "\n" +
                                  GetFetchSQL<AGClusterNetworkInfo>() + "\n" +
                                  GetFetchSQL<AGDatabaseReplica>() + "\n" +
                                  GetFetchSQL<AGListener>() + "\n" +
                                  GetFetchSQL<AGLisenerIPAddress>();
                        
                        List<AGInfo> ags;
                        using (var multi = await conn.QueryMultipleAsync(sql))
                        {
                            ags = await multi.ReadAsync<AGInfo>().AsList();
                            var replicas = await multi.ReadAsync<AGReplica>().AsList();
                            var databases = await multi.ReadAsync<AGDatabaseReplica>().AsList();
                            var listeners = await multi.ReadAsync<AGListener>().AsList();
                            var listenerIPs = await multi.ReadAsync<AGLisenerIPAddress>().AsList();
                            
                            // Databases to replicas...
                            foreach (var r in replicas)
                            {
                                r.Databases = databases.Where(db => db.GroupId == r.GroupId && db.ReplicaId == r.ReplicaId)
                                        .ToList();

                                var instanceName = r.AvailabilityGroupName + ":" + r.ReplicaServerName;
                                var sc = getCounter("Bytes Sent to Transport/sec", instanceName);
                                if (sc != null)
                                {
                                    r.BytesSentPerSecond = sc.CalculatedValue;
                                    r.BytesSentTotal = sc.CurrentValue;
                                }
                                var rc = getCounter("Bytes Received from Replica/sec", instanceName);
                                if (rc != null)
                                {
                                    r.BytesReceivedPerSecond = rc.CalculatedValue;
                                    r.BytesReceivedTotal = rc.CurrentValue;
                                }
                            }

                            // Listners IPs to listeners
                            foreach (var l in listeners)
                            {
                                l.Addresses = listenerIPs.Where(la => la.ListenerId == l.ListenerId).ToList();
                            }

                            // Replicas to availability groups
                            foreach (var ag in ags)
                            {
                                ag.Node = this;
                                ag.ClusterName = Cluster.Name;
                                ag.Replicas = replicas.Where(r => r.GroupId == ag.GroupId).ToList();
                                ag.Listeners = listeners.Where(l => l.GroupId == ag.GroupId).ToList();
                            }
                        }
                        return ags;
                    })
                });
            }
        }
    }
}