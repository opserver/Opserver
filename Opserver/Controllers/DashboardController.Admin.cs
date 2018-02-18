using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Monitoring;
using StackExchange.Opserver.Data.Dashboard.Providers;
using StackExchange.Opserver.Data.Dashboard;

namespace StackExchange.Opserver.Controllers
{
    public partial class DashboardController
    {

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
