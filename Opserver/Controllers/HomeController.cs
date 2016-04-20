using System;
using System.Web.Mvc;
using StackExchange.Opserver.Views.Shared;
using StackExchange.Profiling;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.Home;

namespace StackExchange.Opserver.Controllers
{
    public class HomeController : StatusController
    {
        [Route("")]
        public ActionResult Home()
        {
            return DefaultAction();
        }

        [Route("top-refresh")]
        public ActionResult TopRefresh(string tab)
        {
            MiniProfiler.Stop(discardResults: true);
            TopTabs.CurrentTab = tab;

            var vd = new TopRefreshModel
                {
                    Tab = tab
                };
            return View(vd);
        }

        [Route("about"), AlsoAllow(Roles.InternalRequest)]
        public ActionResult About()
        {
            return View();
        }

        [Route("about/caches"), AlsoAllow(Roles.InternalRequest)]
        public ActionResult AboutCaches(string filter, bool refresh = true)
        {
            var vd = new AboutModel
                {
                    AutoRefresh = refresh,
                    Filter = filter
                };
            return View("About.Caches", vd);
        }

        [Route("set-theme"), HttpPost]
        public ActionResult SetTheme(string theme)
        {
            Theme.Set(theme);
            return Redirect(Request.UrlReferrer?.ToString());
        }

        [Route("debug"), AllowAnonymous]
        public ActionResult Debug()
        {
            var sb = StringBuilderCache.Get()
                .AppendFormat("Request IP: {0}\n", Current.RequestIP)
                .AppendFormat("Request User: {0}\n", Current.User.AccountName)
                .AppendFormat("Request Roles: {0}\n", Current.User.RawRoles)
                .AppendLine()
                .AppendLine("Headers:");
            foreach (string k in Request.Headers.Keys)
            {
                sb.AppendFormat("  {0}: {1}\n", k, Request.Headers[k]);
            }
            
            var ps = PollingEngine.GetPollingStatus();
            sb.AppendLine()
              .AppendLine("Polling Info:")
              .AppendLine(ps.GetPropertyNamesAndValues());
            return TextPlain(sb.ToStringRecycle());
        }

        [Route("error-test")]
        public ActionResult ErrorTestPage()
        {
            Current.LogException(new Exception("Test Exception via GlobalApplication.LogException()"));

            throw new NotImplementedException("I AM IMPLEMENTED, I WAS BORN TO THROW ERRORS!");
        }
    }
}
