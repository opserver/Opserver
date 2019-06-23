using StackExchange.Opserver.Views.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using StackExchange.Opserver.Helpers;
using Microsoft.Extensions.Options;

namespace StackExchange.Opserver.Controllers
{
    public class MiscController : StatusController
    {
        public MiscController(IOptions<OpserverSettings> _settings) : base(_settings) { }

        [Route("no-config")]
        public ViewResult NoConfig()
        {
            return View("NoConfiguration");
        }

        [Route("404")]
        public ViewResult PageNotFound(string title = null, string message = null)
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;

            var vd = new PageNotFoundModel
            {
                Title = title,
                Message = message
            };
            return View("PageNotFound", vd);
        }

        [Route("denied"), AlsoAllow(Models.Roles.Anonymous)]
        public ActionResult AccessDenied()
        {
            if (Current.User.IsAnonymous)
            {
                return RedirectToAction(nameof(LoginController.Login), "Login"); //, new { returnUrl = Request.GetEncodedPathAndQuery() });
            }

            Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return View("~/Views/Shared/AccessDenied.cshtml");
        }

        [Route("error")]
        public ActionResult ErrorPage()
        {
            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return View("Error");
        }
    }
}
