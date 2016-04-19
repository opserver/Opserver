using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using StackExchange.Opserver.Data.HAProxy;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.HAProxy;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.HAProxy)]
    public partial class HAProxyController : StatusController
    {
        public override ISecurableSection SettingsSection => Current.Settings.HAProxy;

        public override TopTab TopTab => new TopTab("HAProxy", nameof(Dashboard), this, 60)
        {
            GetMonitorStatus = () => HAProxyGroup.AllGroups.GetWorstStatus()
        };

        [Route("haproxy")]
        [Route("haproxy/dashboard")]
        public ActionResult Dashboard(string group, string node, string watch = null, bool norefresh = false)
        {
            var haGroup = HAProxyGroup.GetGroup(group ?? node);
            var proxies = haGroup != null ? haGroup.GetProxies() : HAProxyGroup.GetAllProxies();
            proxies.RemoveAll(p => !p.HasServers);

            var vd = new HAProxyModel
            {
                SelectedGroup = haGroup,
                Groups = haGroup != null ? new List<HAProxyGroup> { haGroup } : HAProxyGroup.AllGroups,
                Proxies = proxies,
                View = HAProxyModel.Views.Dashboard,
                Refresh = !norefresh,
                WatchProxy = watch
            };
            return View("HAProxy.Dashboard", vd);
        }

        [Route("haproxy/traffic")]
        public async Task<ActionResult> Traffic(string host)
        {
            if (!Current.Settings.HAProxy.Traffic.Enabled)
                return DefaultAction();
            
            var hosts = HAProxyTraffic.GetHostsAsync();
            var topRoutes = HAProxyTraffic.GetTopPageRotuesAsync(30, host);

            await Task.WhenAll(hosts, topRoutes);

            var vd = new HAProxyModel
            {
                Host = host,
                Hosts = hosts.Result.OrderBy(h => h != "stackoverflow.com").ThenBy(h => h),
                TopRoutes = topRoutes.Result,
                View = HAProxyModel.Views.Traffic
            };
            return View("HAProxy.Traffic", vd);
        }
    }
}