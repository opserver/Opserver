using System.Threading.Tasks;
using System.Web.Mvc;
using StackExchange.Opserver.Data.CloudFlare;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.CloudFlare;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.CloudFlare)]
    public class CloudFlareController : StatusController
    {
        public override ISecurableModule SettingsModule => Current.Settings.CloudFlare;

        public override TopTab TopTab => new TopTab("CloudFlare", nameof(Dashboard), this, 40)
        {
            GetMonitorStatus = () => CloudFlareAPI.Instance.MonitorStatus
        };

        [Route("cloudflare")]
        public ActionResult Dashboard()
        {
            return RedirectToAction(nameof(DNS));
        }

        [Route("cloudflare/dns")]
        public async Task<ActionResult> DNS()
        {
            await CloudFlareAPI.Instance.PollAsync();
            var vd = new DNSModel
            {
                View = DashboardModel.Views.DNS,
                Zones = CloudFlareAPI.Instance.Zones.SafeData(true),
                DNSRecords = CloudFlareAPI.Instance.DNSRecords.Data,
                DataCenters = DataCenters.All
            };
            return View(vd);
        }
    }
}
