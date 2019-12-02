using System.Threading.Tasks;
using Opserver.Helpers;
using Opserver.Data.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace Opserver.Controllers
{
    public partial class DashboardController
    {
        [Route("dashboard/node/service/action"), HttpPost, OnlyAllow(DashboardRoles.Admin)]
        public async Task<ActionResult> ControlService(string node, string name, NodeService.Action serviceAction)
        {
            var n = Module.GetNodeByName(node);
            var s = n.GetService(name);
            var result = Json(await s.Update(serviceAction));
            await n.DataProvider.PollAsync(true);
            return result;
        }
    }
}
