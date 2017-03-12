using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.HAProxy
{
    public partial class HAProxyGroup : IIssuesProvider
    {
        string IIssuesProvider.Name => "HAProxy";

        public IEnumerable<Issue> GetIssues()
        {
            if (MonitorStatus != MonitorStatus.Good && Instances.Any(i => i.LastPoll.HasValue))
            {
                yield return new Issue<HAProxyGroup>(this, "HAProxy", Name);
            }
        }
    }
}
