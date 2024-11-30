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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "ASP0018:Unused route parameter", Justification = "Known unused, matching handler patterns")]
        public Task InvokeErrorHandler() => ExceptionalMiddleware.HandleRequestAsync(HttpContext);
    }
}
