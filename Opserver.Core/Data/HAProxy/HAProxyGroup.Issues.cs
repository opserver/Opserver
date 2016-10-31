using System.Collections.Generic;

namespace StackExchange.Opserver.Data.HAProxy
{
    public partial class HAProxyGroup : IIssuesProvider
    {
        string IIssuesProvider.Name => "HAProxy";

        public IEnumerable<Issue> GetIssues()
        {
            if (MonitorStatus != MonitorStatus.Good)
            {
                yield return new Issue<HAProxyGroup>(this, Name);
            }
        }
    }
}
