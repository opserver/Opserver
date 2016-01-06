using System.Web.Mvc;
using StackExchange.Exceptional;
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

        /// <summary>
        /// Access our error log.
        /// </summary>
        [Route("admin/errors/{resource?}/{subResource?}")]
        public ActionResult InvokeErrorHandler(string resource, string subResource)
        {
            var context = System.Web.HttpContext.Current;
            var factory = new HandlerFactory();

            var page = factory.GetHandler(context, Request.RequestType, Request.Url.ToString(), Request.PathInfo);
            page.ProcessRequest(context);

            return null;
        }
    }
}