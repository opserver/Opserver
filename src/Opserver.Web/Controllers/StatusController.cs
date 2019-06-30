using System.Net;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace StackExchange.Opserver.Controllers
{
    public class StatusController<T> : StatusController where T : StatusModule
    {
        public override ISecurableModule SettingsModule => Module.SecuritySettings;
        protected virtual T Module { get; }

        public StatusController(T module, IOptions<OpserverSettings> settings) : base(settings)
        {
            Module = module;
        }
    }

    [OnlyAllow(Roles.Authenticated)]
    public partial class StatusController : Controller
    {
        public virtual ISecurableModule SettingsModule => null;
        public virtual NavTab NavTab => null;
        protected OpserverSettings Settings { get; }

        public StatusController(IOptions<OpserverSettings> settings)
        {
            Settings = settings.Value;
            if (NavTab != null)
            {
                Current.NavTab = NavTab;
            }

            // TODO: Figure out enabled/disabled (maybe we handle it in route registration instead, or a filter?)
            //var iSettings = SettingsModule as ModuleSettings;
            //if (iSettings?.Enabled == false)
            //    filterContext.Result = DefaultAction();
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

        protected ActionResult JsonNotFound()
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return Json(null);
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
    }
}
