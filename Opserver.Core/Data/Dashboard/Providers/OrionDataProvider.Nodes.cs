using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public partial class OrionDataProvider
    {
        public override List<Node> AllNodes => NodeCache.Data ?? new List<Node>();

        private Cache<List<Node>> _nodeCache;
        public Cache<List<Node>> NodeCache => _nodeCache ?? (_nodeCache = ProviderCache(GetAllNodes, 10));

        private Dictionary<int, List<IPAddress>> _nodeToIPList;
        private Dictionary<IPAddress, List<int>> _ipToNodeList;

        private Cache<List<Tuple<int, IPAddress>>> _nodeIPCache;
        private Cache<List<Tuple<int, IPAddress>>> NodeIPCache
        {
            get { return _nodeIPCache ?? (_nodeIPCache = ProviderCache(async () =>
                {
                    var result = await GetNodeIPMap();

                    var nodeToIP = new Dictionary<int, List<IPAddress>>();
                    foreach (var ip in result)
                    {
                        if (nodeToIP.ContainsKey(ip.Item1))
                            nodeToIP[ip.Item1].Add(ip.Item2);
                        else
                            nodeToIP[ip.Item1] = new List<IPAddress> { ip.Item2 };
                    }
                    _nodeToIPList = nodeToIP;

                    var ipToNode = new Dictionary<IPAddress, List<int>>();
                    foreach (var ip in result)
                    {
                        if (ipToNode.ContainsKey(ip.Item2))
                            ipToNode[ip.Item2].Add(ip.Item1);
                        else
                            ipToNode[ip.Item2] = new List<int> { ip.Item1 };
                    }
                    _ipToNodeList = ipToNode;

                    return result;
                }, 60)); }
        }

        public async Task<List<Node>> GetAllNodes()
        {
            using (MiniProfiler.Current.Step("Get Server Nodes"))
            {
                using (var conn = await GetConnectionAsync())
                {
                    const string sql = @"
Select n.NodeID as Id, 
    Caption as Name, 
    LastSync, 
    MachineType, 
    Cast(Status as int) Status,
    LastBoot, 
    Coalesce(Cast(vm.CPULoad as smallint), n.CPULoad) as CPULoad, 
    TotalMemory, 
    MemoryUsed, 
    IP_Address as Ip, 
    PollInterval as PollIntervalSeconds,
    vmh.NodeID as VMHostID, 
    Cast(IsNull(vh.HostID, 0) as Bit) IsVMHost,
    IsNull(UnManaged, 0) as IsUnwatched,
    UnManageFrom as UnwatchedFrom,
    UnManageUntil as UnwatchedUntil,
    hi.Manufacturer,
    hi.Model,
    hi.ServiceTag
From Nodes n
     Left Join VIM_VirtualMachines vm On n.NodeID = vm.NodeID
     Left Join VIM_Hosts vmh On vm.HostID = vmh.HostID
     Left Join VIM_Hosts vh On n.NodeID = vh.NodeID
     Left Join APM_HardwareInfo hi On n.NodeID = hi.NodeID
Order By Id, Caption";
                    var nodes = await conn.QueryAsync<Node>(sql, commandTimeout: QueryTimeoutMs);
                    nodes.ForEach(n => n.DataProvider = this);
                    return nodes;
                }
            }
        }

        public async Task<List<Tuple<int, IPAddress>>> GetNodeIPMap()
        {
            List<Tuple<int, string>> ipList;
            using (MiniProfiler.Current.Step("Get Server IPs"))
            using (var conn = await GetConnectionAsync())
            {
                ipList = await conn.QueryAsync<int, string, Tuple<int, string>>(
                    @"Select NodeID, IPAddress From NodeIPAddresses",
                    commandTimeout: QueryTimeoutMs,
                    map: Tuple.Create,
                    splitOn: "IPAddress");
            }

            var result = new List<Tuple<int, IPAddress>>();
            foreach (var entry in ipList)
            {
                IPAddress addr;
                if (!IPAddress.TryParse(entry.Item2, out addr)) continue;
                result.Add(Tuple.Create(entry.Item1, addr));
            }
            return result;
        }

        public override IEnumerable<Node> GetNodesByIP(IPAddress ip)
        {
            List<int> nodes;
            return _ipToNodeList != null && _ipToNodeList.TryGetValue(ip, out nodes) ? nodes.Select(GetNode).ToList() : new List<Node>();
        }

        public override IEnumerable<IPAddress> GetIPsForNode(Node node)
        {
            List<IPAddress> ips;
            return node != null && _nodeToIPList != null && _nodeToIPList.TryGetValue(node.Id, out ips) ? ips : new List<IPAddress>();
        }

        public override string GetManagementUrl(Node node)
        {
            return !Host.HasValue() ? null : $"http://{Host}/Orion/NetPerfMon/NodeDetails.aspx?NetObject=N:{node.Id}";
        }
        
        public override Task<IEnumerable<Node.CPUUtilization>> GetCPUUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            const string allSql = @"
Select c.DateTime,
        c.AvgLoad,
        c.MaxLoad,
        Row_Number() Over(Order By c.DateTime) as RowNumber
    From CPULoad c
Where c.DateTime > @minDate
    And c.NodeID = @id";

            const string sampledSql = @"
Select * 
 From (Select c.DateTime,
              c.AvgLoad,
              c.MaxLoad,
              Row_Number() Over(Order By c.DateTime) as RowNumber
         From CPULoad c
        Where {dateRange}
          And c.NodeID = @id) c
Where c.RowNumber % ((Select Count(*) + @intervals
                        From CPULoad c
                       Where {dateRange}
                         And c.NodeID = @id)/@intervals) = 0
Order By c.DateTime";

            return UtilizationQuery<Node.CPUUtilization>(node.Id, allSql, sampledSql, "c.DateTime", start, end, pointCount);
        }

        public override Task<IEnumerable<Node.MemoryUtilization>> GetMemoryUtilization(Node node, DateTime? start, DateTime? end, int? pointCount = null)
        {
            const string allSql = @"
Select c.DateTime,
        c.AvgMemoryUsed, 
        c.MaxMemoryUsed,
        c.TotalMemory,
        Row_Number() Over(Order By c.DateTime) as RowNumber
    From CPULoad c
Where c.DateTime > @minDate
    And c.NodeID = @id";

            const string sampledSql = @"
Select * 
 From (Select c.DateTime,
              c.AvgMemoryUsed,  
              c.MaxMemoryUsed,
              c.TotalMemory,
              Row_Number() Over(Order By c.DateTime) as RowNumber
         From CPULoad c
        Where {dateRange}
          And c.NodeID = @id) c
Where c.RowNumber % ((Select Count(*) + @intervals
                        From CPULoad c
                       Where {dateRange}
                         And c.NodeID = @id)/@intervals) = 0
Order By c.DateTime";

            return UtilizationQuery<Node.MemoryUtilization>(node.Id, allSql, sampledSql, "c.DateTime", start, end, pointCount);
        }
    }
}
