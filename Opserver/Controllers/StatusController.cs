using System;
using System.Net;
using System.Web.WebPages;
using System.Web.Mvc;
using Jil;
using StackExchange.Opserver.Views.Shared;
using StackExchange.Profiling;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Models.Security;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.Authenticated)]
    public partial class StatusController : Controller
    {
        protected virtual ISecurableSection SettingsSection => null;
        protected virtual string TopTab => null;

        private IDisposable _betweenInitializeAndActionExecuting,
                            _betweenActionExecutingAndExecuted,
                            _betweenActionExecutedAndResultExecuting,
                            _betweenResultExecutingAndExecuted;

        private readonly Func<string, IDisposable> _startStep = name => MiniProfiler.Current.Step(name);
        private readonly Action<IDisposable> _stopStep = s => s?.Dispose();
        
        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            _betweenInitializeAndActionExecuting = _startStep("Initialize");
            base.Initialize(requestContext);
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!filterContext.IsChildAction)
            {
                _stopStep(_betweenInitializeAndActionExecuting);
                _betweenActionExecutingAndExecuted = _startStep("OnActionExecuting");
                TopTabs.CurrentTab = TopTab;
            }

            var iSettings = SettingsSection as Settings;
            if (iSettings != null && !iSettings.Enabled)
                filterContext.Result = DefaultAction();
            else
                base.OnActionExecuting(filterContext);
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (!filterContext.IsChildAction)
            {
                _stopStep(_betweenActionExecutingAndExecuted);
                _betweenActionExecutedAndResultExecuting = _startStep("OnActionExecuted");
            }
            base.OnActionExecuted(filterContext);
        }
        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (!filterContext.IsChildAction)
            {
                _stopStep(_betweenActionExecutedAndResultExecuting);
                _betweenResultExecutingAndExecuted = _startStep("OnResultExecuting");
            }
            base.OnResultExecuting(filterContext);
        }

        protected override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            _stopStep(_betweenResultExecutingAndExecuted);

            using (MiniProfiler.Current.Step("OnResultExecuted"))
            {
                base.OnResultExecuted(filterContext);
            }
        }

        public ActionResult DefaultAction()
        {
            var s = Current.Settings;

            if (s.Dashboard.Enabled && s.Dashboard.HasAccess())
                return RedirectToAction("Dashboard", "Dashboard");
            if (s.SQL.Enabled && s.SQL.HasAccess())
                return RedirectToAction("Dashboard", "SQL");
            if (s.Redis.Enabled && s.Redis.HasAccess())
                return RedirectToAction("Dashboard", "Redis");
            if (s.Elastic.Enabled && s.Elastic.HasAccess())
                return RedirectToAction("Dashboard", "Elastic");
            if (s.Exceptions.Enabled && s.Exceptions.HasAccess())
                return RedirectToAction("Exceptions", "Exceptions");
            if (s.HAProxy.Enabled && s.HAProxy.HasAccess())
                return RedirectToAction("HAProxyDashboard", "HAProxy");

            return View("NoConfiguration");
        }

        [Route("change-view")]
        public RedirectResult SwitchView(bool mobile)
        {
            if (Request.Browser.IsMobileDevice == mobile)
                HttpContext.ClearOverriddenBrowser();
            else
                HttpContext.SetOverriddenBrowser(mobile ? BrowserOverride.Mobile : BrowserOverride.Desktop);
            return Redirect(Request.UrlReferrer.PathAndQuery);
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
                return Redirect("/login?ReturnUrl=" + Request.Url.PathAndQuery.UrlEncode());
            }

            Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return View("~/Views/Shared/AccessDenied.cshtml");
        }

        public void SetTitle(string title)
        {
            title = HtmlUtilities.Encode(title);
            var pageTitle = string.IsNullOrEmpty(title) ? SiteSettings.SiteName : string.Concat(title, " - ", SiteSettings.SiteName);
            ViewData[ViewDataKeys.PageTitle] = pageTitle;
        }
        
        /// <summary>
        /// returns ContentResult with the parameter 'content' as its payload and "text/plain" as media type.
        /// </summary>
        protected ContentResult TextPlain(object content)
        {
            return new ContentResult { Content = content.ToString(), ContentType = "text/plain" };
        }

        protected ContentResult ContentNotFound(string message = null)
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return Content(message ?? "404");
        }

        protected ContentResult NoContent()
        {
            Response.StatusCode = (int)HttpStatusCode.NoContent;
            return Content(null);
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

        public new JsonNetResult Json(object data)
        {
            return new JsonNetResult { Data = data, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        protected JsonResult JsonNotFound(object toSerialize = null)
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return Json(toSerialize);
        }

        protected JsonNetResult JsonError(string message, HttpStatusCode? status = null)
        {
            Response.StatusCode = (int)(status ?? HttpStatusCode.InternalServerError);
            return Json(new { ErrorMessage = message });
        }

        protected JsonNetResult JsonError(object toSerialize, HttpStatusCode? status = null)
        {
            Response.StatusCode = (int)(status ?? HttpStatusCode.InternalServerError);
            return Json(toSerialize);
        }

        public class JsonNetResult : JsonResult
        {
            public override void ExecuteResult(ControllerContext context)
            {
                if (context == null)
                    throw new ArgumentNullException(nameof(context));

                var response = context.HttpContext.Response;
                response.ContentType = ContentType.HasValue() ? ContentType : "application/json";
                if (ContentEncoding != null) response.ContentEncoding = ContentEncoding;

                var serializedObject = JSON.Serialize(Data);
                response.Write(serializedObject);
            }
        }
    }
}