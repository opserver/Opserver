using System.Web.Mvc;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.Hub;

namespace StackExchange.Opserver.Controllers
{
    public class HubController : StatusController
    {
        public override ISecurableSection SettingsSection => Current.Settings.Dashboard;
        
        [Route("hub"), Route("headsup"), AlsoAllow(Roles.InternalRequest)]
        public ActionResult Index()
        {
            var vd = new HubModel();
            return View(vd);
        }
    }
}