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
        public override ISecurableSection SettingsSection => Current.Settings.CloudFlare;

        public override TopTab TopTab => new TopTab("CloudFlare", nameof(Dashboard), this, 40)
        {
            GetMonitorStatus = () => CloudFlareAPI.Instance.MonitorStatus
        };

        [Route("cloudflare")]
        public ActionResult Dashboard()
        {
            return RedirectToAction(nameof(DNS));
        }

        [Route("cloudflare/railgun")]
        public ActionResult Railguns() 
        {
            var vd = new RailgunsModel
                {
                    Railguns = RailgunInstance.AllInstances,
                    View = DashboardModel.Views.Railgun
                };
            return View(vd);
        }

        [Route("cloudflare/dns")]
        public ActionResult DNS()
        {
            CloudFlareAPI.Instance.WaitForFirstPoll(10000);
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
