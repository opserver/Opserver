using System.Collections.Generic;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.HAProxy
{
    public partial class HAProxyInstance
    {
        public IEnumerable<NodeRole> GetRoles(string node)
        {
            var data = Proxies.Data;
            if (data == null) yield break;

            foreach (var p in data)
            {
                if (p.Servers == null) continue;
                Server found = null;
                var active = 0;
                var inactive = 0;
                foreach (var s in p.Servers)
                {
                    if (s.Name == node)
                    {
                        found = s;
                    }
                    if (s.MonitorStatus == MonitorStatus.Good)
                    {
                        active++;
                    }
                    else
                    {
                        inactive++;
                    }
                }
                if (found != null)
                {
                    yield return new NodeRole
                    {
                        Service = "HAProxy",
                        Description = $"{Name} - {Group?.Name ?? "(No Group)"} - {p.NiceName}",
                        Active = found.MonitorStatus == MonitorStatus.Good,
                        TotalActive = active,
                        TotalInactive = inactive,
                    };
                }
            }
        }

        public Task<bool> EnableAsync(string node) => HAProxyAdmin.PerformServerActionAsync(node, Action.Ready);
        public Task<bool> DisableAsync(string node) => HAProxyAdmin.PerformServerActionAsync(node, Action.Drain);
    }
}
