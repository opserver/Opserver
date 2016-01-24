using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.SQL
{
    public class SQLIssues : IIssuesProvider
    {
        public string Name => "SQL";

        public IEnumerable<Issue> GetIssues()
        {
            foreach (var ag in SQLCluster.AllClusters.SelectMany(c => c.AvailabilityGroups).WithIssues())
            {
                yield return new Issue<SQLNode.AGInfo>(ag, ag.Name) { IsService = true };
            }
            foreach (var instance in SQLInstance.AllInstances.WithIssues())
            {
                yield return new Issue<SQLInstance>(instance, instance.Name);
            }
        }
    }
}
