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
        public ActionResult JsonNodes(string type, string[] uniqueKey, Guid? guid = null)
        {
            if (type.IsNullOrEmpty())
                return JsonError("type is missing");
            if (!(uniqueKey?.Any() ?? false))
                return JsonError("uniqueKey is missing");
            try
            {
                if (uniqueKey.Length > 1)
                {
                    bool result = true;
                    Parallel.ForEach(uniqueKey, k =>
                    {
                        result &= PollingEngine.Poll(type, k, guid, sync: true);
                    });
                    return Json(result);
                }
                var pollResult = PollingEngine.Poll(type, uniqueKey[0], guid, sync: true);
                return Json(pollResult);
            }
            catch (Exception e)
            {
                return JsonError("Error polling node: " + e.Message);
            }
        }
    }
}