using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
                return Json(await Module.Admin.PerformServerActionAsync(server, act).ConfigureAwait(false));
            // Entire group
            if (proxy.IsNullOrEmpty() && server.IsNullOrEmpty() && group.HasValue())
                return Json(await Module.Admin.PerformGroupActionAsync(group, act).ConfigureAwait(false));

            var haGroup = Module.GetGroup(group);
            var proxies = (haGroup != null ? haGroup.GetProxies() : Module.GetAllProxies()).Where(pr => pr.Name == proxy);

            return Json(await Module.Admin.PerformProxyActionAsync(proxies, server, act).ConfigureAwait(false));
        }
    }
}
