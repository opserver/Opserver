using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.Dashboard
{
    public class DashboardIssues : IIssuesProvider
    {
        public string Name => "Dashboard";

        public IEnumerable<Issue> GetIssues()
        {
            var downNodes = DashboardData.AllNodes
                                         .Where(se => se.MonitorStatus != MonitorStatus.Good && se.MonitorStatus != MonitorStatus.Unknown)
                                         .OrderBy(n => n.Name);
            foreach (var n in downNodes)
            {
                if (!n.IsUnwatched)
                    yield return new Issue<Node>(n, n.Name);
            }
        }
    }
}
