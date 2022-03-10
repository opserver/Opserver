using System.Collections.Generic;
using Opserver.Data.HAProxy;

namespace Opserver.Views.HAProxy
{
    public class HAProxyModel
    {
        public enum Views
        {
            Admin = 0,
            Dashboard = 1,
            Detailed = 2,
        }

        public Views View { get; set; }
        public bool AdminMode { get; set; }

        public bool IsInstanceFilterable
        {
            get
            {
                switch(View)
                {
                    case Views.Admin:
                    case Views.Dashboard:
                    case Views.Detailed:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool Refresh { get; set; }
        public List<HAProxyGroup> Groups { get; set; }
        public HAProxyGroup SelectedGroup { get; set; }
        public List<Proxy> Proxies { get; set; }
        public string WatchProxy { get; set; }

        public static string GetClass(Item s)
        {
            if (s.IsFrontend)
                return "frontend";
            if (s.IsBackend)
                return "backend";
            if (s.IsServer)
                return s.ProxyServerStatus.ToString();
            return "";
        }

        public string Host { get; set; }
        public IEnumerable<string> Hosts { get; set; }
    }
}
