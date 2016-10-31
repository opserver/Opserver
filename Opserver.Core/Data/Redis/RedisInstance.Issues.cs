using System.Collections.Generic;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisInstance : IIssuesProvider
    {
        string IIssuesProvider.Name => "Redis";

        public IEnumerable<Issue> GetIssues()
        {
            if (MonitorStatus != MonitorStatus.Good)
            {
                yield return new Issue<RedisInstance>(this, Name);
            }
        }
    }
}
