using System.Collections.Generic;

namespace StackExchange.Opserver.Data.Elastic
{
    public partial class ElasticCluster : IIssuesProvider
    {
        string IIssuesProvider.Name => "Elastic";

        public IEnumerable<Issue> GetIssues()
        {
            if (MonitorStatus != MonitorStatus.Good && LastPoll.HasValue)
            {
                yield return new Issue<ElasticCluster>(this, "Elastic", Name) { IsCluster = true };
            }
        }
    }
}
