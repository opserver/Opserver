using System.Collections.Generic;

namespace Opserver.Data.SQL
{
    public partial class SQLCluster : IIssuesProvider
    {
        string IIssuesProvider.Name => "SQL";

        public IEnumerable<Issue> GetIssues()
        {
            foreach (var ag in AvailabilityGroups.WithIssues())
            {
                yield return new Issue<SQLNode.AGInfo>(ag, "SQL Availability Group", ag.Name) { IsCluster = true };
            }
        }
    }
}
