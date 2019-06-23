using System.Threading.Tasks;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Data.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace StackExchange.Opserver.Controllers
{
    public partial class DashboardController
    {
        public DashboardController(IOptions<OpserverSettings> _settings) : base(_settings) { }

        [Route("dashboard/node/service/action"), HttpPost, OnlyAllow(Roles.DashboardAdmin)]
        public async Task<ActionResult> ControlService(string node, string name, NodeService.Action serviceAction)
        {
            var n = DashboardModule.GetNodeByName(node);
            var s = n.GetService(name);
            var result = Json(await s.Update(serviceAction).ConfigureAwait(false));
            await n.DataProvider.PollAsync(true).ConfigureAwait(false);
            return result;
        }
    }
}
