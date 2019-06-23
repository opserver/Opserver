using System;
using System.IO;
using System.Net;
using Jil;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Opserver.Data.Elastic;
using StackExchange.Opserver.Data.Exceptions;
using StackExchange.Opserver.Data.HAProxy;
using StackExchange.Opserver.Data.Redis;
using StackExchange.Opserver.Data.SQL;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Models.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.Authenticated)]
    public partial class StatusController : Controller
    {
        public virtual ISecurableModule SettingsModule => null;
        public virtual TopTab TopTab => null;
        protected OpserverSettings Settings { get; }

        public StatusController(IOptions<OpserverSettings> _settings)
        {
            Settings = _settings.Value;
            // TODO: Change how all this works
            TopTabs.SetCurrent(GetType());

            // TODO: Figure out enabled/disabled (maybe we handle it in route registration instead, or a filter?)
            //var iSettings = SettingsModule as ModuleSettings;
            //if (iSettings?.Enabled == false)
            //    filterContext.Result = DefaultAction();
        }

        public ActionResult DefaultAction()
        {
            var s = Settings;

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

        public void SetTitle(string title)
        {
            title = title.HtmlEncode();
            ViewData[ViewDataKeys.PageTitle] = title.IsNullOrEmpty() ? SiteSettings.SiteName : string.Concat(title, " - ", SiteSettings.SiteName);
        }

        /// <summary>
        /// returns ContentResult with the parameter 'content' as its payload and "text/plain" as media type.
        /// </summary>
        /// <param name="content">The text content to render</param>
        protected ContentResult TextPlain(string content) =>
            new ContentResult { Content = content, ContentType = "text/plain" };

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

        protected ContentResult JsonRaw(object content) =>
            new ContentResult { Content = content?.ToString(), ContentType = "application/json" };

        protected ActionResult Json<T>(T data, Jil.Options options = null) =>
            new JsonJilResult<T> { Data = data, Options = options };

        protected ActionResult JsonNotFound()
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return Json<object>(null);
        }

        protected ActionResult JsonNotFound<T>(T toSerialize = default)
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
            public Jil.Options Options { get; set; }

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
