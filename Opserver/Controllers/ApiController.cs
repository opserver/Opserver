using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Jil;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.LocalRequest | Roles.InternalRequest)] // API Requests are internal only
    public class ApiController : StatusController
    {
        private Options JilOptions = Options.ExcludeNulls;

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Request.QueryString["pretty"] != null)
            {
                JilOptions = Options.PrettyPrintExcludeNulls;
            }
            base.OnActionExecuting(filterContext);
        }

        [Route("api/node/roles")]
        public ActionResult NodeRoles(string node)
        {
            return Json(new NodeResults(node), JilOptions);
        }

        [Route("api/node/enable"), HttpPost]
        public async Task<ActionResult> NodeEnable(string node)
        {
            if (!Current.IsInRole(Roles.ApiRequest)) return JsonError("Invalid API key");

            await NodeRole.EnableAllAsync(node).ConfigureAwait(false);
            return NodeRoles(node);
        }

        [Route("api/node/disable"), HttpPost]
        public async Task<ActionResult> NodeDisable(string node)
        {
            if (!Current.IsInRole(Roles.ApiRequest)) return JsonError("Invalid API key");

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
