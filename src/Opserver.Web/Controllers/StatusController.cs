using System;
using System.Net;
using Jil;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Opserver.Data.Elastic;
using StackExchange.Opserver.Data.Exceptions;
using StackExchange.Opserver.Data.HAProxy;
using StackExchange.Opserver.Data.Redis;
using StackExchange.Opserver.Data.SQL;
using StackExchange.Opserver.Views.Shared;
using StackExchange.Profiling;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Models.Security;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Http.Extensions;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.Authenticated)]
    public partial class StatusController : Controller
    {
        public virtual ISecurableModule SettingsModule => null;
        public virtual TopTab TopTab => null;

        private readonly Func<string, IDisposable> _startStep = name => MiniProfiler.Current.Step(name);
        private readonly Action<IDisposable> _stopStep = s => s?.Dispose();

        public StatusController()
        {
            // TODO: Change how all this works
            TopTabs.SetCurrent(GetType());

            var iSettings = SettingsModule as ModuleSettings;
            if (iSettings?.Enabled == false)
                filterContext.Result = DefaultAction();

            
        }

        public ActionResult DefaultAction()
        {
            var s = Current.Settings;

            // TODO: Plugin registrations - middleware?
            // Order could be interesting here, needs to be tied to top tabs
            if (DashboardModule.Enabled && s.Dashboard.HasAccess())
                return RedirectToAction(nameof(DashboardController.Dashboard), "Dashboard");
            if (SQLModule.Enabled && s.SQL.HasAccess())
                return RedirectToAction(nameof(SQLController.Dashboard), "SQL");
            if (RedisModule.Enabled && s.Redis.HasAccess())
                return RedirectToAction(nameof(RedisController.Dashboard), "Redis");
            if (ElasticModule.Enabled && s.Elastic.HasAccess())
                return RedirectToAction(nameof(ElasticController.Dashboard), "Elastic");
            if (ExceptionsModule.Enabled && s.Exceptions.HasAccess())
                return RedirectToAction(nameof(ExceptionsController.Exceptions), "Exceptions");
            if (HAProxyModule.Enabled && s.HAProxy.HasAccess())
                return RedirectToAction(nameof(HAProxyController.Dashboard), "HAProxy");

            return View("NoConfiguration");
        }

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

        [Route("denied")]
        public ActionResult AccessDenied()
        {
            if (Current.User.IsAnonymous)
            {
                return RedirectToAction(nameof(LoginController.Login), "Login", new { returnUrl = Request.GetEncodedPathAndQuery() });
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

        public void SetTitle(string title)
        {
            title = title.HtmlEncode();
            ViewData[ViewDataKeys.PageTitle] = title.IsNullOrEmpty() ? SiteSettings.SiteName : string.Concat(title, " - ", SiteSettings.SiteName);
        }

        /// <summary>
        /// returns ContentResult with the parameter 'content' as its payload and "text/plain" as media type.
        /// </summary>
        /// <param name="content">The text content to render</param>
        protected ContentResult TextPlain(string content)
        {
            return new ContentResult { Content = content, ContentType = "text/plain" };
        }

        protected ContentResult ContentNotFound(string message = null)
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return Content(message ?? "404");
        }

        protected ContentResult NotFound(string message = null)
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return Content(message);
        }

        protected ContentResult JsonRaw(object content)
        {
            return new ContentResult { Content = content?.ToString(), ContentType = "application/json" };
        }

        protected ActionResult Json<T>(T data, Options options = null)
        {
            return new JsonJilResult<T> { Data = data, Options = options };
        }

        protected ActionResult JsonNotFound()
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return Json<object>(null);
        }

        protected ActionResult JsonNotFound<T>(T toSerialize = default(T))
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return Json(toSerialize);
        }

        protected ActionResult JsonError(string message, HttpStatusCode? status = null)
        {
            Response.StatusCode = (int)(status ?? HttpStatusCode.InternalServerError);
            return Json(new { ErrorMessage = message });
        }

        protected ActionResult JsonError<T>(T toSerialize, HttpStatusCode? status = null)
        {
            Response.StatusCode = (int)(status ?? HttpStatusCode.InternalServerError);
            return Json(toSerialize);
        }

        public class JsonJilResult<T> : ActionResult
        {
            public T Data { get; set; }
            public string ContentType { get; set; }
            public Options Options { get; set; }

            public override void ExecuteResult(ActionContext context)
            {
                if (context == null)
                    throw new ArgumentNullException(nameof(context));

                var response = context.HttpContext.Response;
                response.ContentType = ContentType.HasValue() ? ContentType : "application/json";

                using (var sw = new StreamWriter(response.Body))
                {
                    JSON.Serialize(sw, Options);
                }
            }
        }
    }
}
