using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Opserver.Data.Cloudflare;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.Cloudflare;

namespace StackExchange.Opserver.Controllers
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
