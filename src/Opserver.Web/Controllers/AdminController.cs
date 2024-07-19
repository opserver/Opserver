using StackExchange.Exceptional;
using Opserver.Helpers;
using Opserver.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Opserver.Controllers
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
