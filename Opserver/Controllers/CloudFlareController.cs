using System.Web.Mvc;
using StackExchange.Opserver.Data.CloudFlare;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.CloudFlare;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.CloudFlare)]
    public partial class CloudFlareController : StatusController
    {
        protected override ISecurableSection SettingsSection => Current.Settings.CloudFlare;

        protected override string TopTab => TopTabs.BuiltIn.CloudFlare;

        [Route("cloudflare")]
        public ActionResult Dashboard()
        {
            return Redirect("/cloudflare/dns");
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
