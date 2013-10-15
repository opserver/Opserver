using System.Collections.Generic;
using System.Linq;
using Dapper;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public partial class OrionDataProvider
    {
        public override List<Application> AllApplications
        {
            get { return ApplicationCache.Data ?? new List<Application>(); }
        }

        private Cache<List<Application>> _applicationCache;
        public Cache<List<Application>> ApplicationCache
        {
            get { return _applicationCache ?? (_applicationCache = ProviderCache(GetAllApplications, 10)); }
        }

        public List<Application> GetAllApplications()
        {
            using (MiniProfiler.Current.Step("Get Applications"))
            {
                using (var conn = GetConnection())
                {
                    const string sql =
                        @"
Select com.ApplicationID as Id, 
       NodeID as NodeId, 
       app.Name as AppName, 
       IsNull(app.Unmanaged, 0) as IsUnwatched,
       app.UnManageFrom as UnwatchedFrom,
       app.UnManageUntil as UnwatchedUntil,
       com.Name as ComponentName, 
       ccs.TimeStamp as LastUpdated,
       pe.PID as ProcessID, 
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
Where pe.ID In (Select Max(ID)
                  From APM_ProcessEvidence
                 Where ComponentStatusID In (Select ComponentStatusID From APM_CurrentComponentStatus)
              Group By ComponentStatusID)
Order By NodeID";

                    var results = conn.Query<Application>(sql, commandTimeout: QueryTimeoutMs).ToList();
                    foreach (var r in results)
                    {
                        r.DataProvider = this;
                        if (r.ComponentName == "Process Monitor - WMI" || r.ComponentName == "Wrapper Process")
                            r.NiceName = r.AppName;
                        else
                            r.NiceName = (r.ComponentName ?? "").Replace(" IIS App Pool", "");
                    }
                    return results;
                }
            }
        }
    }
}
