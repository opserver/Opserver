using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using StackExchange.Opserver.Data.HAProxy;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Controllers
{
    public partial class HAProxyController
    {
        [Route("haproxy/admin/action"), HttpPost, OnlyAllow(Roles.HAProxyAdmin)]
        public async Task<ActionResult> HAProxyAdminProxy(string group, string proxy, string server, Action act)
        {
            // Entire server
            if (proxy.IsNullOrEmpty() && group.IsNullOrEmpty() && server.HasValue())
                return Json(await HAProxyAdmin.PerformServerActionAsync(server, act));
            // Entire group
            if (proxy.IsNullOrEmpty() && server.IsNullOrEmpty() && group.HasValue())
                return Json(await HAProxyAdmin.PerformGroupActionAsync(group, act));
            
            var haGroup = HAProxyGroup.GetGroup(group);
            var proxies = (haGroup != null ? haGroup.GetProxies() : HAProxyGroup.GetAllProxies()).Where(pr => pr.Name == proxy);

            return Json(await HAProxyAdmin.PerformProxyActionAsync(proxies, server, act));
        }
    }
}
