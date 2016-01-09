using System.Linq;
using System.Web.Mvc;
using StackExchange.Opserver.Data.HAProxy;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Controllers
{
    public partial class HAProxyController
    {
        [Route("haproxy/admin/action"), HttpPost, OnlyAllow(Roles.HAProxyAdmin)]
        public ActionResult HAProxyAdminProxy(string group, string proxy, string server, HAProxyAdmin.Action act)
        {
            // Entire server
            if (proxy.IsNullOrEmpty() && group.IsNullOrEmpty() && server.HasValue())
                return Json(HAProxyAdmin.PerformServerActionAsync(server, act));
            // Entire group
            if (proxy.IsNullOrEmpty() && server.IsNullOrEmpty() && group.HasValue())
                return Json(HAProxyAdmin.PerformGroupActionAsync(group, act));
            
            var haGroup = HAProxyGroup.GetGroup(group);
            var proxies = (haGroup != null ? haGroup.GetProxies() : HAProxyGroup.GetAllProxies()).Where(pr => pr.Name == proxy);

            return Json(HAProxyAdmin.PerformProxyActionAsync(proxies, server, act));
        }
    }
}
