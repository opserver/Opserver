using System;
using System.Web.Mvc;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.GlobalAdmin)]
    public class CacheController : StatusController
    {
        [Route("cache/poll")]
        public ActionResult Poll(string type, string key, Guid? id)
        {
            if (!type.HasValue() || !key.HasValue())
                return ContentNotFound();

            var success = PollingEngine.Poll(type, key, id);

            return Json(new { success });
        }

        [Route("cache/poll/all")]
        public ActionResult PollAll(string type, string key)
        {
            if (type.IsNullOrEmpty() || key.IsNullOrEmpty())
                return ContentNotFound();

            PollingEngine.Poll(type, key);

            return Json(new { success = true });
        }
    }
}