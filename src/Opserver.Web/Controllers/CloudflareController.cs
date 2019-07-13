using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Opserver.Data.Cloudflare;
using Opserver.Helpers;
using Opserver.Models;
using Opserver.Views.Cloudflare;

namespace Opserver.Controllers
{
    [OnlyAllow(Roles.Cloudflare)]
    public class CloudflareController : StatusController<CloudflareModule>
    {
        public CloudflareController(CloudflareModule module, IOptions<OpserverSettings> settings) : base(module, settings) { }

        [DefaultRoute("cloudflare")]
        public ActionResult Dashboard() => RedirectToAction(nameof(DNS));

        [Route("cloudflare/dns")]
        public async Task<ActionResult> DNS()
        {
            await Module.API.PollAsync();
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
