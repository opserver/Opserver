using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jil;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.InternalRequest | Roles.ApiRequest)] // API Requests are internal only
    public class ApiController : StatusController
    {
        public ApiController(IOptions<OpserverSettings> _settings) : base(_settings) { }

        private Jil.Options JilOptions =>
            Request.Query.ContainsKey("pretty")
            ? Jil.Options.PrettyPrintExcludeNulls
            : Jil.Options.ExcludeNulls;

        [Route("api/node/roles")]
        public ActionResult NodeRoles(string node)
        {
            return Json(new NodeResults(node), JilOptions);
        }

        [Route("api/node/enable"), HttpPost]
        public async Task<ActionResult> NodeEnable(string node)
        {
            if (!Current.User.Is(Roles.ApiRequest)) return JsonError("Invalid API key");

            await NodeRole.EnableAllAsync(node).ConfigureAwait(false);
            return NodeRoles(node);
        }

        [Route("api/node/disable"), HttpPost]
        public async Task<ActionResult> NodeDisable(string node)
        {
            if (!Current.User.Is(Roles.ApiRequest)) return JsonError("Invalid API key");

            await NodeRole.DisableAllAsync(node).ConfigureAwait(false);
            return NodeRoles(node);
        }

        public class NodeResults
        {
            public int Active { get; set; }
            public int Inactive { get; set; }
            public List<NodeRole> Roles { get; set; }

            public NodeResults(string node)
            {
                Roles = NodeRole.Get(node).ToList();
                foreach (var r in Roles)
                {
                    if (r.Active) Active++;
                    else Inactive++;
                }
            }
        }
    }
}
