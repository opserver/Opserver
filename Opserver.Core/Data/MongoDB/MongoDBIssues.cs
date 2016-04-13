using System.Collections.Generic;

namespace StackExchange.Opserver.Data.MongoDB
{
    public class MongoDBIssues : IIssuesProvider
    {
        public string Name => "MongoDB";

        public IEnumerable<Issue> GetIssues()
        {
            foreach (var i in MongoDBInstance.AllInstances.WithIssues())
            {
                yield return new Issue<MongoDBInstance>(i, i.Name);
            }
        }
    }
}
