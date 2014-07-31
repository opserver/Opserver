using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StackExchange.Opserver.Data.HAProxy;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.HAProxy;

namespace StackExchange.Opserver.Controllers
{
    public partial class HAProxyController
    {
        [Route("haproxy/admin"), OnlyAllow(Roles.HAProxyAdmin)]
        public ActionResult HAProxyAdminDashboard(string group, bool norefresh = false)
        {
            var haGroup = HAProxyGroup.GetGroup(group);
            var proxies = haGroup != null ? haGroup.GetProxies() : HAProxyGroup.GetAllProxies();
            proxies.RemoveAll(p => !p.HasServers);

            var vd = new HAProxyModel
                {
                    SelectedGroup = haGroup,
                    Groups = haGroup != null ? new List<HAProxyGroup> { haGroup } : HAProxyGroup.AllGroups,
                    Proxies = proxies,
                    View = HAProxyModel.Views.Admin,
                    Refresh = !norefresh,
                    AdminMode = true
                };
            return View("HAProxy.Dashboard", vd);
        }

        [Route("haproxy/admin/proxy"), HttpPost, OnlyAllow(Roles.HAProxyAdmin)]
        public ActionResult HAProxyAdminProxy(string group, string proxy, string server, HAProxyAdmin.Action act)
        {
            var haGroup = HAProxyGroup.GetGroup(group);
            var proxies = (haGroup != null ? haGroup.GetProxies() : HAProxyGroup.GetAllProxies()).Where(pr => pr.Name == proxy);

            return Json(HAProxyAdmin.PerformProxyAction(proxies, server, act));
        }

        [Route("haproxy/admin/server"), HttpPost, OnlyAllow(Roles.HAProxyAdmin)]
        public ActionResult HAProxyAdminServer(string server, HAProxyAdmin.Action act)
        {
            return Json(HAProxyAdmin.PerformServerAction(server, act));
        }

        [Route("haproxy/admin/group"), HttpPost, OnlyAllow(Roles.HAProxyAdmin)]
        public ActionResult HAProxyAdminGroup(string group, HAProxyAdmin.Action act)
        {
            return Json(HAProxyAdmin.PerformGroupAction(group, act));
        }
    }
}
