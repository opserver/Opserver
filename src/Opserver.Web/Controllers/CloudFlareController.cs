using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Opserver.Data.CloudFlare;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.CloudFlare;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.CloudFlare)]
    public class CloudFlareController : StatusController
    {
        private CloudFlareModule Module { get; }
        public CloudFlareController(IOptions<OpserverSettings> _settings, CloudFlareModule module) : base(_settings)
        {
            Module = module;
        }

        public override ISecurableModule SettingsModule => Settings.CloudFlare;

        public override TopTab TopTab => new TopTab("CloudFlare", nameof(Dashboard), this, 40)
        {
            GetMonitorStatus = () => Module.MonitorStatus
        };

        [Route("cloudflare")]
        public ActionResult Dashboard()
        {
            return RedirectToAction(nameof(DNS));
        }

        [Route("cloudflare/dns")]
        public async Task<ActionResult> DNS()
        {
            await Module.API.PollAsync().ConfigureAwait(false);
            var vd = new DNSModel
            {
                View = DashboardModel.Views.DNS,
                Zones = Module.API.Zones.SafeData(true),
                DNSRecords = Module.API.DNSRecords.Data,
                DataCenters = Module.AllDatacenters
            };
            return View(vd);
        }
    }
}
