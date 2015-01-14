using System;
using System.Web;
using System.Web.Mvc;
using StackExchange.Opserver.Data.SQL;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Controllers
{
    public partial class SQLController
    {
        [Route("sql/remove-plan"), HttpPost, OnlyAllow(Roles.SQLAdmin)]
        public ActionResult SQLRemovePlan(string node, string handle)
        {
            var planHandle = HttpServerUtility.UrlTokenDecode(handle);
            var instance = SQLInstance.Get(node);
            if (instance == null)
            {
                return JsonError("Could not find server " + node);
            }
            var result = instance.RemovePlan(planHandle);

            return result != 0 ? Json(true) : JsonError("There was an error removing the plan from cache");
        }

        [Route("sql/toggle-agent-job"), HttpPost, OnlyAllow(Roles.SQLAdmin)]
        public ActionResult ToggleAgentJob(string node, Guid guid, bool enable)
        {
            var instance = SQLInstance.Get(node);
            return Json(instance.ToggleJob(guid, enable));
        }

        [Route("sql/start-agent-job"), HttpPost, OnlyAllow(Roles.SQLAdmin)]
        public ActionResult StartAgentJob(string node, Guid guid)
        {
            var instance = SQLInstance.Get(node);
            return Json(instance.StartJob(guid));
        }

        [Route("sql/stop-agent-job"), HttpPost, OnlyAllow(Roles.SQLAdmin)]
        public ActionResult StopAgentJob(string node, Guid guid)
        {
            var instance = SQLInstance.Get(node);
            return Json(instance.StopJob(guid));
        }
    }
}