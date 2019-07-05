using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Controllers
{
    public partial class SQLController
    {
        [Route("sql/remove-plan"), HttpPost, OnlyAllow(Roles.SQLAdmin)]
        public async Task<ActionResult> RemovePlan(string node, string handle)
        {
            var planHandle = WebEncoders.Base64UrlDecode(handle);
            var instance = Module.GetInstance(node);
            if (instance == null)
            {
                return JsonError("Could not find server " + node);
            }
            var result = await instance.RemovePlanAsync(planHandle).ConfigureAwait(false);

            return result != 0 ? Json(true) : JsonError("There was an error removing the plan from cache");
        }

        [Route("sql/toggle-agent-job"), HttpPost, OnlyAllow(Roles.SQLAdmin)]
        public async Task<ActionResult> ToggleAgentJob(string node, Guid guid, bool enable)
        {
            var instance = Module.GetInstance(node);
            return Json(await instance.ToggleJobAsync(guid, enable).ConfigureAwait(false));
        }

        [Route("sql/start-agent-job"), HttpPost, OnlyAllow(Roles.SQLAdmin)]
        public async Task<ActionResult> StartAgentJob(string node, Guid guid)
        {
            var instance = Module.GetInstance(node);
            return Json(await instance.StartJobAsync(guid).ConfigureAwait(false));
        }

        [Route("sql/stop-agent-job"), HttpPost, OnlyAllow(Roles.SQLAdmin)]
        public async Task<ActionResult> StopAgentJob(string node, Guid guid)
        {
            var instance = Module.GetInstance(node);
            return Json(await instance.StopJobAsync(guid).ConfigureAwait(false));
        }
    }
}
