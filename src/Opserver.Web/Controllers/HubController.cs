using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.Hub;

namespace StackExchange.Opserver.Controllers
{
    public class HubController : StatusController
    {
        public HubController(IOptions<OpserverSettings> _settings) : base(_settings) { }

        public override ISecurableModule SettingsModule => Settings.Dashboard;

        [Route("hub"), Route("headsup"), AlsoAllow(Roles.InternalRequest)]
        public ActionResult Index()
        {
            var vd = new HubModel();
            return View(vd);
        }
    }
}
