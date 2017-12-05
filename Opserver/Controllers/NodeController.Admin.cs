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
    public partial class NodeController
    {

        [Route("service/start"), HttpPost, OnlyAllow(Roles.GlobalAdmin)]
        public async Task<ActionResult> ServiceStart(string node, string name)
        {
            var n = DashboardModule.GetNodeByName(node);
            var s = n.GetService(name);
            var result = Json(await s.Update(NodeService.Action.Start).ConfigureAwait(false));
            await n.DataProvider.PollAsync(true).ConfigureAwait(false);
            return result;
        }

        [Route("service/stop"), HttpPost, OnlyAllow(Roles.GlobalAdmin)]
        public async Task<ActionResult> ServiceStop(string node, string name) 
        {
            var n = DashboardModule.GetNodeByName(node);
            var s = n.GetService(name);
            var result = Json(await s.Update(NodeService.Action.Stop).ConfigureAwait(false));
            await n.DataProvider.PollAsync(true).ConfigureAwait(false);
            return result;
        }
    }
}
