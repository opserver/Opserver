using System.Collections.Generic;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance : IIssuesProvider
    {
        string IIssuesProvider.Name => "SQL";

        public IEnumerable<Issue> GetIssues()
        {
            if (MonitorStatus != MonitorStatus.Good)
            {
                yield return new Issue<SQLInstance>(this, Name);
            }
        }
    }
}
