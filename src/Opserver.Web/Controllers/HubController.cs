using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.Hub;

namespace StackExchange.Opserver.Controllers
{
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
