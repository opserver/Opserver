using System;
using System.Collections.Generic;
using System.Linq;

namespace Opserver.Data.SQL
{
    public partial class SQLNode
    {
        private Cache<List<AGInfo>> _availabilityGroups;
        public Cache<List<AGInfo>> AvailabilityGroups =>
            _availabilityGroups ?? (_availabilityGroups = GetSqlCache(nameof(AvailabilityGroups), async conn =>
            {
                PerfCounterRecord getCounter(string cn, string n) => GetPerfCounter("Availability Replica", cn, n);
                var sql = QueryLookup.GetOrAdd(Tuple.Create(nameof(AvailabilityGroups), Version), k =>
                        GetFetchSQL<AGInfo>(k.Item2) + "\n" +
                        GetFetchSQL<AGReplica>(k.Item2) + "\n" +
                        GetFetchSQL<AGDatabaseReplica>(k.Item2) + "\n" +
                        GetFetchSQL<AGListener>(k.Item2) + "\n" +
                        GetFetchSQL<AGLisenerIPAddress>(k.Item2)
                );

                List<AGInfo> ags;
                using (var multi = await conn.QueryMultipleAsync(sql, commandTimeout: 1200).ConfigureAwait(false))
                {
                    ags = await multi.ReadAsync<AGInfo>().AsList();
                    var replicas = await multi.ReadAsync<AGReplica>().AsList();
                    var databases = await multi.ReadAsync<AGDatabaseReplica>().AsList();
                    var listeners = await multi.ReadAsync<AGListener>().AsList();
                    var listenerIPs = await multi.ReadAsync<AGLisenerIPAddress>().AsList();

                    // Databases to replicas...
                    foreach (var r in replicas)
                    {
                        r.Databases = databases.Where(db => db.GroupId == r.GroupId && db.ReplicaId == r.ReplicaId).ToList();

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
            }));
    }
}
