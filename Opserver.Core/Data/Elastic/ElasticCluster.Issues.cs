using System.Collections.Generic;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster : IIssuesProvider
    {
        string IIssuesProvider.Name => "Elastic";

        public IEnumerable<Issue> GetIssues()
        {
            if (MonitorStatus != MonitorStatus.Good)
            {
                yield return new Issue<ElasticCluster>(this, Name) { IsCluster = true };
            }
        }
    }
}
