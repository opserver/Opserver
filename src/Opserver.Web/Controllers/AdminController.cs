using StackExchange.Exceptional;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.GlobalAdmin)]
    public class AdminController : StatusController
    {
        public AdminController(IOptions<OpserverSettings> _settings) : base(_settings) {}

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
        public Task InvokeErrorHandler() => ExceptionalMiddleware.HandleRequestAsync(HttpContext);
    }
}
