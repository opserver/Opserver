﻿using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.Dashboard
{
    public class DashboardIssues : IIssuesProvider
    {
        public IEnumerable<Issue> GetIssues()
        {
            var downNodes = DashboardData.Current.AllNodes
                                         .Where(se => se.MonitorStatus != MonitorStatus.Good && se.MonitorStatus != MonitorStatus.Unknown)
                                         .OrderBy(n => n.Name);
            foreach (var n in downNodes)
            {
                if (!n.IsSilenced)
                    yield return new Issue<Node>(n, n.Name);
            }
        }
    }
}
