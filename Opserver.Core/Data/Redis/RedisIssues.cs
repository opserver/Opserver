using System.Collections.Generic;

namespace StackExchange.Opserver.Data.Redis
{
    public class RedisIssues : IIssuesProvider
    {
        public bool Enabled => RedisInstance.AllInstances.Count > 0;
        public string Name => "Redis";

        public IEnumerable<Issue> GetIssues()
        {
            foreach (var i in RedisInstance.AllInstances.WithIssues())
            {
                yield return new Issue<RedisInstance>(i, i.Name);
            }
        }
    }
}
