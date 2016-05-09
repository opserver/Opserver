using System.Collections.Generic;

namespace StackExchange.Opserver.Data.Elastic
{
    public class ElasticIssues : IIssuesProvider
    {
        public bool Enabled => ElasticCluster.AllClusters.Count > 0;
        public string Name => "Elastic";

        public IEnumerable<Issue> GetIssues()
        {
            foreach (var c in ElasticCluster.AllClusters.WithIssues())
            {
                yield return new Issue<ElasticCluster>(c, c.Name) { IsCluster = true };
            }
        }
    }
}
