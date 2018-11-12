using System.Web.Mvc;
using StackExchange.Exceptional;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.GlobalAdmin)]
    public class AdminController : StatusController
    {
        [Route("admin/security/purge-cache")]
        public ActionResult Dashboard()
        {
            Current.Security.PurgeCache();
            return TextPlain("Cache Purged");
        }

        /// <summary>
        /// Access our error log.
        /// </summary>
        [Route("admin/errors/{resource?}/{subResource?}"), AlsoAllow(Roles.LocalRequest)]
        public Task InvokeErrorHandler() => ExceptionalModule.HandleRequestAsync(System.Web.HttpContext.Current);
    }
}