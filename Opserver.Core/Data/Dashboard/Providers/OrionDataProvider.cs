using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Opserver.Helpers;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public class OrionDataProvider : DashboardDataProvider<OrionSettings>
    {
        public override bool HasData => NodeCache.HasData();
        public string Host => Settings.Host;
        public int QueryTimeoutMs => Settings.QueryTimeoutMs;
        public override int MinSecondsBetweenPolls => 5;
        public override string NodeType => "Orion";

        public override IEnumerable<Cache> DataPollers
        {
            get { yield return NodeCache; }
        }

        public OrionDataProvider(OrionSettings settings) : base(settings) { }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() { return null; }

        public override List<Node> AllNodes => NodeCache.Data ?? new List<Node>();

        private Cache<List<Node>> _nodeCache;
        public Cache<List<Node>> NodeCache => _nodeCache ?? (_nodeCache = ProviderCache(GetAllNodesAsync, 10));

        public async Task<List<Node>> GetAllNodesAsync()
        {
            using (MiniProfiler.Current.Step("Get Server Nodes"))
            {
                using (var conn = await GetConnectionAsync().ConfigureAwait(false))
                {
                    var nodes = await conn.QueryAsync<Node>(@"
Select Cast(n.NodeID as varchar(50)) as Id,
       Caption as Name,
       LastSync,
       MachineType,
       Cast(Status as int) Status,
       Cast(ChildStatus as int) ChildStatus,
       StatusDescription,
       LastBoot, 
       Coalesce(Cast(vm.CPULoad as smallint), n.CPULoad) as CPULoad,
       TotalMemory,
       MemoryUsed,
       IP_Address as Ip,
       PollInterval as PollIntervalSeconds,
       Cast(vmh.NodeID as varchar(50)) as VMHostID,
       Cast(IsNull(vh.HostID, 0) as Bit) IsVMHost,
       IsNull(UnManaged, 0) as IsUnwatched,
       hi.Manufacturer,
       hi.Model,
       hi.ServiceTag
  From Nodes n
       Left Join VIM_VirtualMachines vm On n.NodeID = vm.NodeID
       Left Join VIM_Hosts vmh On vm.HostID = vmh.HostID
       Left Join VIM_Hosts vh On n.NodeID = vh.NodeID
       Left Join APM_HardwareInfo hi On n.NodeID = hi.NodeID
 Where LastSync Is Not Null
 Order By Id, Caption", commandTimeout: QueryTimeoutMs).ConfigureAwait(false);

                    var interfaces = await conn.QueryAsync<Interface>(@"
Select Cast(InterfaceID as varchar(50)) as Id,
       Cast(NodeID as varchar(50)) as NodeId,
       LastSync,
       InterfaceName as Name,
       FullName,
       Caption,
       Comments,
       InterfaceAlias Alias,
       InterfaceTypeDescription TypeDescription,
       PhysicalAddress,
       Cast(Status as int) Status,
       InBps,
       OutBps,
       InPps,
       OutPps,
       InterfaceMTU as MTU,
       InterfaceSpeed as Speed
From Interfaces", commandTimeout: QueryTimeoutMs).ConfigureAwait(false);

                    var volumes = await conn.QueryAsync<Volume>(@"
Select Cast(VolumeID as varchar(50)) as Id,
       Cast(NodeID as varchar(50)) as NodeId,
       LastSync,
       VolumeIndex as [Index],
       FullName as Name,
       Caption,
       VolumeDescription as [Description],
       VolumeType as Type,
       VolumeSize as Size,
       VolumeSpaceUsed as Used,
       VolumeSpaceAvailable as Available,
       VolumePercentUsed as PercentUsed
  From Volumes
 Where VolumeType = 'Fixed Disk'", commandTimeout: QueryTimeoutMs).ConfigureAwait(false);

                    var apps = await conn.QueryAsync<Application>(@"
Select Cast(com.ApplicationID as varchar(50)) as Id, 
       Cast(NodeID as varchar(50)) as NodeId, 
       app.Name as AppName, 
       IsNull(app.Unmanaged, 0) as IsUnwatched,
       com.Name as ComponentName, 
       ccs.TimeStamp as LastUpdated,
       --pe.PID as ProcessID, 
       ccs.ProcessName,
       ccs.LastTimeUp, 
       ccs.PercentCPU as CurrentPercentCPU,
       ccs.PercentMemory as CurrentPercentMemory,
       ccs.MemoryUsed as CurrentMemoryUsed,
       ccs.VirtualMemoryUsed as CurrentVirtualMemoryUsed,
       pe.AvgPercentCPU as PercentCPU, 
       pe.AvgPercentMemory as PercentMemory, 
       pe.AvgMemoryUsed as MemoryUsed, 
       pe.AvgVirtualMemoryUsed as VirtualMemoryUsed,
       pe.ErrorMessage
From APM_Application app
     Inner Join APM_Component com
       On app.ID = com.ApplicationID
     Inner Join APM_CurrentComponentStatus ccs
       On com.ID = ccs.ComponentID
     Inner Join APM_ProcessEvidence pe
       On ccs.ComponentStatusID = pe.ComponentStatusID
Order By NodeID", commandTimeout: QueryTimeoutMs).ConfigureAwait(false);

                    foreach (var a in apps)
                    {
                        a.NiceName = a.ComponentName == "Process Monitor - WMI" || a.ComponentName == "Wrapper Process"
                            ? a.AppName
                            : (a.ComponentName ?? "").Replace(" IIS App Pool", "");
                    }

                    var ips = await GetNodeIPMapAsync(conn).ConfigureAwait(false);

                    foreach (var i in interfaces)
                    {
                        i.IPs = ips.Where(ip => i.Id == ip.InterfaceID && ip.IPNet != null).Select(ip => ip.IPNet).ToList();
                    }

                    var exclude = Current.Settings.Dashboard.ExcludePatternRegex;
                    if (exclude != null)
                    {
                        nodes = nodes.Where(n => !exclude.IsMatch(n.Name)).ToList();
                    }

                    foreach (var n in nodes)
                    {
                        n.DataProvider = this;
                        n.ManagementUrl = GetManagementUrl(n);
                        n.Interfaces = interfaces.Where(i => i.NodeId == n.Id).ToList();
                        n.Volumes = volumes.Where(v => v.NodeId == n.Id).ToList();
                        n.Apps = apps.Where(a => a.NodeId == n.Id).ToList();
                        n.VMs = nodes.Where(on => on.VMHostID == n.Id).ToList();
                        n.VMHost = nodes.FirstOrDefault(on => n.VMHostID == on.Id);
                        n.SetReferences();

                        if (Settings.ChildStatusForHealthy && n.Status == NodeStatus.Up && n.ChildStatus.HasValue)
                        {
                            n.Status = n.ChildStatus.Value;
                        }

                        if (n.Status != NodeStatus.Up)
                        {
                            n.Issues = new List<Issue<Node>>
                            {
                                new Issue<Node>(n)
                                {
                                    Date = n.LastSync ?? DateTime.UtcNow,
                                    Title = n.PrettyName,
                                    Description = n.StatusDescription,
                                    MonitorStatus = n.Status.ToMonitorStatus()
                                }
                            };
                        }
                    }

                    return nodes;
                }
            }
        }

        // ReSharper disable ClassNeverInstantiated.Local
        private class OrionIPMap
        {
            public string InterfaceID { get; set; }
            public string IPAddress { get; set; }
            public string SubnetMask { get; set; }
            public IPNet IPNet { get; set; }
        }
        // ReSharper restore ClassNeverInstantiated.Local

        private async Task<List<OrionIPMap>> GetNodeIPMapAsync(DbConnection conn)
        {
            var result = await conn.QueryAsync<OrionIPMap>(@"
Select Cast(i.InterfaceID as varchar(50)) as InterfaceID, ipa.IPAddress, ipa.SubnetMask
  From NodeIPAddresses ipa
       Join Interfaces i 
         On ipa.NodeID = i.NodeID
         And ipa.InterfaceIndex = i.InterfaceIndex",
                commandTimeout: QueryTimeoutMs).ConfigureAwait(false);
            
            foreach (var m in result)
            {
                IPNet net;
                if (IPNet.TryParse(m.IPAddress, m.SubnetMask, out net))
                {
                    m.IPNet = net;
                }
            }
            return result;
        }

        public override string GetManagementUrl(Node node)
        {
            return !Host.HasValue() ? null : $"{Host}Orion/NetPerfMon/NodeDetails.aspx?NetObject=N:{node.Id}";
        }

        public override async Task<List<GraphPoint>> GetCPUUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            const string allSql = @"
Select DateDiff(s, '1970-01-01 00:00:00', c.DateTime) as DateEpoch,
       c.AvgLoad
  From CPULoad c
 Where c.DateTime > @minDate
   And c.NodeID = @id
 Order By c.DateTime";

            const string sampledSql = @"
Select DateDiff(s, '1970-01-01 00:00:00', c.DateTime) as DateEpoch,
       c.AvgLoad
 From (Select c.DateTime,
              c.AvgLoad,
              Row_Number() Over(Order By c.DateTime) as RowNumber
         From CPULoad c
        Where {dateRange}
          And c.NodeID = @id) c
Where c.RowNumber % ((Select Count(*) + @intervals
                        From CPULoad c
                       Where {dateRange}
                         And c.NodeID = @id)/@intervals) = 0
Order By c.DateTime";

            return (await UtilizationQueryAsync<Node.CPUUtilization>(node.Id, allSql, sampledSql, "c.DateTime", start, end, pointCount).ConfigureAwait(false)).ToList<GraphPoint>();
        }

        public override async Task<List<GraphPoint>> GetMemoryUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            const string allSql = @"
Select DateDiff(s, '1970-01-01 00:00:00', c.DateTime) as DateEpoch,
       c.AvgMemoryUsed
  From CPULoad c
 Where c.DateTime > @minDate
   And c.NodeID = @id
 Order By c.DateTime";

            const string sampledSql = @"
Select DateDiff(s, '1970-01-01 00:00:00', c.DateTime) as DateEpoch,
       c.AvgMemoryUsed
 From (Select c.DateTime,
              c.AvgMemoryUsed,
              Row_Number() Over(Order By c.DateTime) as RowNumber
         From CPULoad c
        Where {dateRange}
          And c.NodeID = @id) c
Where c.RowNumber % ((Select Count(*) + @intervals
                        From CPULoad c
                       Where {dateRange}
                         And c.NodeID = @id)/@intervals) = 0
Order By c.DateTime";

            return (await UtilizationQueryAsync<Node.MemoryUtilization>(node.Id, allSql, sampledSql, "c.DateTime", start, end, pointCount).ConfigureAwait(false)).ToList<GraphPoint>();
        }

        public override async Task<List<DoubleGraphPoint>> GetNetworkUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            const string allSql = @"
Select DateDiff(s, '1970-01-01', itd.DateTime) as DateEpoch,
       Sum(itd.In_Averagebps) InAvgBps,
       Sum(itd.Out_Averagebps) OutAvgBps
  From InterfaceTraffic itd
 Where itd.InterfaceID In @Ids
   And {dateRange}
 Group By itd.DateTime
 Order By itd.DateTime
";

            const string sampledSql = @"
Select DateDiff(s, '1970-01-01', itd.DateTime) as DateEpoch,
       Sum(itd.InAvgBps) InAvgBps,
       Sum(itd.OutAvgBps) OutAvgBps
  From (Select itd.DateTime,
		       itd.In_Averagebps InAvgBps,
		       itd.Out_Averagebps OutAvgBps,
		       Row_Number() Over(Order By itd.DateTime) RowNumber
          From InterfaceTraffic itd
         Where itd.InterfaceID In @Ids
           And {dateRange}) itd
 Where itd.RowNumber % ((Select Count(*) + @intervals
						   From InterfaceTraffic itd
					      Where itd.InterfaceID In @Ids
                            And {dateRange})/@intervals) = 0
 Group By itd.DateTime
 Order By itd.DateTime";
            
            if (!node.PrimaryInterfaces.Any()) return new List<DoubleGraphPoint>();

            using (var conn = await GetConnectionAsync().ConfigureAwait(false))
            {
                var result = await conn.QueryAsync<Interface.InterfaceUtilization>(
                    (pointCount.HasValue ? sampledSql : allSql)
                        .Replace("{dateRange}", GetOptionalDateClause("itd.DateTime", start, end)),
                    new { Ids = node.PrimaryInterfaces.Select(i => int.Parse(i.Id)), start, end, intervals = pointCount }).ConfigureAwait(false);
                return result.ToList<DoubleGraphPoint>();
            }
        }

        public override async Task<List<GraphPoint>> GetUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            const string allSql = @"
Select DateDiff(s, '1970-01-01 00:00:00', v.DateTime) as DateEpoch,
       v.AvgDiskUsed
  From VolumeUsage v
 Where {dateRange}
   And v.VolumeID = @id
 Order By v.DateTime";

            const string sampledSql = @"
Select DateDiff(s, '1970-01-01 00:00:00', v.DateTime) as DateEpoch,
       v.AvgDiskUsed
  From (Select v.DateTime
               v.AvgDiskUsed,
               Row_Number() Over(Order By v.DateTime) as RowNumber
          From VolumeUsage v
         Where {dateRange}
           And v.VolumeID = @id) v
 Where v.RowNumber % ((Select Count(*) + @intervals
                         From VolumeUsage v
                        Where {dateRange}
                          And v.VolumeID = @id)/@intervals) = 0
 Order By v.DateTime";

            return (await UtilizationQueryAsync<Volume.VolumeUtilization>(volume.Id, allSql, sampledSql, "v.DateTime", start, end, pointCount).ConfigureAwait(false)).ToList<GraphPoint>();
        }

        public override async Task<List<DoubleGraphPoint>> GetUtilizationAsync(Interface nodeInteface, DateTime? start, DateTime? end, int? pointCount = null)
        {
            const string allSql = @"
Select DateDiff(s, '1970-01-01 00:00:00', itd.DateTime) as DateEpoch,
       itd.In_Averagebps InAvgBps,
       itd.Out_Averagebps OutAvgBps
  From InterfaceTraffic itd
 Where itd.InterfaceID = @Id
   And {dateRange}
 Order By itd.DateTime
";

            const string sampledSql = @"
Select DateDiff(s, '1970-01-01 00:00:00', itd.DateTime) as DateEpoch,
       itd.InAvgBps,
       itd.OutAvgBps
  From (Select itd.DateTime,
		       itd.In_Averagebps InAvgBps,
		       itd.Out_Averagebps OutAvgBps,
		       Row_Number() Over(Order By itd.DateTime) RowNumber
          From InterfaceTraffic itd
         Where itd.InterfaceID = @Id
           And {dateRange}) itd
 Where itd.RowNumber % ((Select Count(*) + @intervals
						   From InterfaceTraffic itd
					      Where itd.InterfaceID = @Id
                            And {dateRange})/@intervals) = 0
 Order By itd.DateTime";

            return (await UtilizationQueryAsync<Interface.InterfaceUtilization>(nodeInteface.Id, allSql, sampledSql, "itd.DateTime", start, end, pointCount).ConfigureAwait(false)).ToList<DoubleGraphPoint>();
        }

        public Task<DbConnection> GetConnectionAsync()
        {
            return Connection.GetOpenAsync(Settings.ConnectionString, QueryTimeoutMs);
        }

        private string GetOptionalDateClause(string field, DateTime? start, DateTime? end)
        {
            if (start.HasValue && end.HasValue) // start & end
                return $"{field} Between @start and @end";
            if (start.HasValue) // no end
                return $"{field} >= @start";
            if (end.HasValue)
                return $"{field} <= @end";
            return "1 = 1";
        }

        private async Task<List<T>> UtilizationQueryAsync<T>(string id, string allSql, string sampledSql, string dateField, DateTime? start, DateTime? end, int? pointCount) where T : IGraphPoint
        {
            using (var conn = await GetConnectionAsync().ConfigureAwait(false))
            {
                return await conn.QueryAsync<T>(
                    (pointCount.HasValue ? sampledSql : allSql)
                        .Replace("{dateRange}", GetOptionalDateClause(dateField, start, end)),
                    new { id = int.Parse(id), start, end, intervals = pointCount }).ConfigureAwait(false);
            }
        }
    }
}
