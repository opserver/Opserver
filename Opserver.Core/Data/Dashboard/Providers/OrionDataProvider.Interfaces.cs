using System.Collections.Generic;
using System.Linq;
using Dapper;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public partial class OrionDataProvider
    {
        public override List<Interface> AllInterfaces
        {
            get { return InterfaceCache.Data ?? new List<Interface>(); }
        }

        private Cache<List<Interface>> _interfaceCache;
        public Cache<List<Interface>> InterfaceCache
        {
            get { return _interfaceCache ?? (_interfaceCache = ProviderCache(GetAllInterfaces, 10)); }
        }

        public List<Interface> GetAllInterfaces()
        {
            using (MiniProfiler.Current.Step("Get Interfaces"))
            {
                // TODO: IsUnwatched
                using (var conn = GetConnection())
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
                    var interfaces = conn.Query<Interface>(sql, commandTimeout: QueryTimeoutMs).ToList();
                    interfaces.ForEach(i => i.DataProvider = this);
                    return interfaces;
                }
            }
        }

        public override IEnumerable<Interface.InterfaceUtilization> GetUtilization(Interface nodeInteface, System.DateTime? start, System.DateTime? end, int? pointCount = null)
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
