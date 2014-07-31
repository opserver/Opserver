using System.Web.Mvc;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.GlobalAdmin)]
    public class AdminController : StatusController
    {
        [Route("admin/purge-security-cache")]
        public ActionResult Dashboard(string view)
        {
            Current.Security.PurgeCache();
            return TextPlain("Cache Purged");
        }
    }
}