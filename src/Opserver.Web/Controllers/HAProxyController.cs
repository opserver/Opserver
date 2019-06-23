using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Opserver.Data.HAProxy;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.HAProxy;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.HAProxy)]
    public partial class HAProxyController : StatusController
    {
        public HAProxyModule Module { get; }

        public override ISecurableModule SettingsModule => Settings.HAProxy;

        public override TopTab TopTab => new TopTab("HAProxy", nameof(Dashboard), this, 60)
        {
            GetMonitorStatus = () => Module.MonitorStatus
        };

        public HAProxyController(IOptions<OpserverSettings> _settings, HAProxyModule module) : base(_settings)
        {
            Module = module;
        }

        [Route("haproxy")]
        [Route("haproxy/dashboard")]
        public ActionResult Dashboard(string group, string node, string watch = null, bool norefresh = false)
        {
            var haGroup = Module.GetGroup(group ?? node);
            var proxies = haGroup != null ? haGroup.GetProxies() : Module.GetAllProxies();
            proxies.RemoveAll(p => !p.HasServers);

            var vd = new HAProxyModel
            {
                SelectedGroup = haGroup,
                Groups = haGroup != null ? new List<HAProxyGroup> { haGroup } : Module.Groups,
                Proxies = proxies,
                View = HAProxyModel.Views.Dashboard,
                Refresh = !norefresh,
                WatchProxy = watch
            };
            return View("HAProxy.Dashboard", vd);
        }
    }
}
