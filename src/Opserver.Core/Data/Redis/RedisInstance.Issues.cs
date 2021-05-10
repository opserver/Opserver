using System.Collections.Generic;

namespace Opserver.Data.Redis
{
    public partial class RedisInstance : IIssuesProvider
    {
        string IIssuesProvider.Name => "Redis";

        public IEnumerable<Issue> GetIssues()
        {
            if (MonitorStatus != MonitorStatus.Good && LastPoll.HasValue)
            {
                yield return new Issue<RedisInstance>(this, "Redis", Name);
            }
        }
    }
}
