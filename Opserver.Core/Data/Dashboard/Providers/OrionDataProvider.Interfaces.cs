using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public partial class OrionDataProvider
    {
        public override List<Interface> AllInterfaces => InterfaceCache.Data ?? new List<Interface>();

        private Cache<List<Interface>> _interfaceCache;
        public Cache<List<Interface>> InterfaceCache => _interfaceCache ?? (_interfaceCache = ProviderCache(GetAllInterfaces, 10));

        public async Task<List<Interface>> GetAllInterfaces()
        {
            using (MiniProfiler.Current.Step("Get Interfaces"))
            {
                // TODO: IsUnwatched
                using (var conn = await GetConnectionAsync())
                {
                    const string sql = @"
Select InterfaceID as Id,
       NodeID as NodeId,
       InterfaceIndex [Index],
       LastSync,
       InterfaceName as Name,
       FullName,
       Caption,
       Comments,
       InterfaceAlias Alias,
       IfName,
       InterfaceTypeDescription TypeDescription,
       PhysicalAddress,
       IsNull(UnManaged, 0) as IsUnwatched,
       UnManageFrom as UnwatchedFrom,
       UnManageUntil as UnwatchedUntil,
       Cast(Status as int) Status,
       InBps,
       OutBps,
       InPps,
       OutPps,
       InPercentUtil,
       OutPercentUtil,
       InterfaceMTU as MTU,
       InterfaceSpeed as Speed
From Interfaces";
                    var interfaces = await conn.QueryAsync<Interface>(sql, commandTimeout: QueryTimeoutMs);
                    interfaces.ForEach(i => i.DataProvider = this);
                    return interfaces;
                }
            }
        }

        public override Task<IEnumerable<Interface.InterfaceUtilization>> GetUtilization(Interface nodeInteface, DateTime? start, DateTime? end, int? pointCount = null)
        {
            const string allSql = @"
Select itd.DateTime,
		       itd.In_Maxbps InMaxBps,
		       itd.In_Averagebps InAvgBps,
		       itd.Out_Maxbps OutMaxBps,
		       itd.Out_Averagebps OutAvgBps,
		       Row_Number() Over(Order By itd.DateTime) RowNumber
          From InterfaceTraffic itd
         Where itd.InterfaceID = @Id
           And {dateRange}
";

                const string sampledSql = @"
Select * 
  From (Select itd.DateTime,
		       itd.In_Maxbps InMaxBps,
		       itd.In_Averagebps InAvgBps,
		       itd.Out_Maxbps OutMaxBps,
		       itd.Out_Averagebps OutAvgBps,
		       Row_Number() Over(Order By itd.DateTime) RowNumber
          From InterfaceTraffic itd
         Where itd.InterfaceID = @Id
           And {dateRange}) itd
 Where itd.RowNumber % ((Select Count(*) + @intervals
						   From InterfaceTraffic itd
					      Where itd.InterfaceID = @Id
                            And {dateRange})/@intervals) = 0";

            return UtilizationQuery<Interface.InterfaceUtilization>(nodeInteface.Id, allSql, sampledSql, "itd.DateTime", start, end, pointCount);
        }
    }
}
