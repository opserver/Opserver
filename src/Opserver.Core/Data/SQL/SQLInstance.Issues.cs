using System.Collections.Generic;

namespace Opserver.Data.SQL
{
    public partial class SQLInstance : IIssuesProvider
    {
        string IIssuesProvider.Name => "SQL";

        public IEnumerable<Issue> GetIssues()
        {
            if (MonitorStatus != MonitorStatus.Good)
            {
                yield return new Issue<SQLInstance>(this, "SQL Server", Name);
            }
        }
    }
}
