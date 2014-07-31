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
        protected override ISecurableSection SettingsSection
        {
            get { return Current.Settings.CloudFlare; }
        }

        [Route("cloudflare")]
        public ActionResult Dashboard()
        {
            return Redirect("/cloudflare/dns");
        }

        [Route("cloudflare/railgun")]
        public ActionResult Railguns() 
        {
            var vd = new DashboardModel
                {
                    Railguns = RailgunInstance.AllInstances,
                    View = DashboardModel.Views.Railgun
                };
            return View(vd);
        }

        [Route("cloudflare/dns")]
        public ActionResult DNS()
        {
            var vd = new DashboardModel
            {
                View = DashboardModel.Views.DNS
            };
            return View(vd);
        }
    }
}
