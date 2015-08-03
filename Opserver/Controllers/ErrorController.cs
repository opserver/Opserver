using System;
using System.Net;
using System.Web.Mvc;
using StackExchange.Exceptional;

namespace StackExchange.Opserver.Controllers
{
    public class ErrorController : StatusController
    {
        [Route("error")]
        public ActionResult ErrorPage()
        {
            Response.StatusCode = (int) HttpStatusCode.InternalServerError;
            return View("Error");
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

        [Route("error-test")]
        public ActionResult ErrorTestPage()
        {
            Current.LogException(new Exception("Test Exception via GlobalApplication.LogException()"));

            throw new NotImplementedException("I AM IMPLEMENTED, I WAS BORN TO THROW ERRORS!");
        }
    }
}
