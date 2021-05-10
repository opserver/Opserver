using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Opserver.Data.Dashboard;
using Opserver.Helpers;
using Opserver.Models;
using Opserver.Views.Hub;

namespace Opserver.Controllers
{
    [OnlyAllow(Roles.Authenticated)]
    public class HubController : StatusController<DashboardModule>
    {
        public HubController(DashboardModule module, IOptions<OpserverSettings> settings) : base(module, settings) { }

        [Route("hub"), Route("headsup"), AlsoAllow(Roles.InternalRequest)]
        public ActionResult Index()
        {
            var vd = new HubModel();
            return View(vd);
        }
    }
}
