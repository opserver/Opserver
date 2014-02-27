using System;
using System.Web.Mvc;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.Authenticated)]
    public class PollController : StatusController
    {
        [Route("poll")]
        public ActionResult JsonNodes(string type, string uniqueKey, Guid? guid = null)
        {
            if (type.IsNullOrEmpty())
                return JsonError("type is missing");
            if (uniqueKey.IsNullOrEmpty())
                return JsonError("uniqueKey is missing");
            try
            {
                var pollResult = PollingEngine.Poll(type, uniqueKey, guid, sync: true);
                return Json(pollResult);
            }
            catch (Exception e)
            {
                return JsonError("Error polling node: " + e.Message);
            }
        }
    }
}