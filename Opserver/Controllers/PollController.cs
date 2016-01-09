using System;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task<ActionResult> PollNodes(string type, string[] key, Guid? guid = null)
        {
            if (type.IsNullOrEmpty())
                return JsonError("type is missing");
            if (!(key?.Any() ?? false))
                return JsonError("key is missing");
            try
            {
                var polls = key.Select(k => PollingEngine.PollAsync(type, k, guid));
                var results = await Task.WhenAll(polls);
                return Json(results.Aggregate(true, (current, r) => current & r));
            }
            catch (Exception e)
            {
                return JsonError("Error polling node: " + e.Message);
            }
        }

        [Route("poll/all"), HttpPost, OnlyAllow(Roles.GlobalAdmin)]
        public ActionResult PollDown()
        {
            try
            {
                PollingEngine.PollAllAsync(true);
                return Json(true);
            }
            catch (Exception e)
            {
                return JsonError("Error polling all nodes: " + e.Message);
            }
        }
    }
}