using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Opserver.Data;
using Opserver.Helpers;
using Opserver.Models;

namespace Opserver.Controllers
{
    [OnlyAllow(Roles.InternalRequest), AlsoAllow(Roles.ApiRequest)] // API Requests are internal only
    public class ApiController : StatusController
    {
        private PollingService Poller { get; }

        public ApiController(IOptions<OpserverSettings> _settings, PollingService poller) : base(_settings) => Poller = poller;

        [Route("api/node/roles")]
        public ActionResult NodeRoles(string node)
        {
            var roles = Poller.GetNodeRoles(node).ToList();
            return Json(new NodeResults(roles));
        }

        [Route("api/node/enable"), HttpPost]
        public async Task<ActionResult> NodeEnable(string node)
        {
            if (!Current.User.Is(Roles.ApiRequest)) return JsonError("Invalid API key");

            await Poller.EnableAllNodeRolesAsync(node);
            return NodeRoles(node);
        }

        [Route("api/node/disable"), HttpPost]
        public async Task<ActionResult> NodeDisable(string node)
        {
            if (!Current.User.Is(Roles.ApiRequest)) return JsonError("Invalid API key");

            await Poller.DisableAllNodeRolesAsync(node);
            return NodeRoles(node);
        }

        public class NodeResults
        {
            public int Active { get; set; }
            public int Inactive { get; set; }
            public List<NodeRole> Roles { get; set; }

            public NodeResults(List<NodeRole> roles)
            {
                Roles = roles;
                foreach (var r in Roles)
                {
                    if (r.Active) Active++;
                    else Inactive++;
                }
            }
        }
    }
}
