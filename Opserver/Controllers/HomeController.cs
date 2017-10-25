﻿using System;
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
            TopTabs.CurrentTab = tab;

            var vd = new TopRefreshModel
                {
                    Tab = tab
                };
            return View(vd);
        }

        [Route("issues")]
        public ActionResult Issues() => View();

        [Route("about"), AlsoAllow(Roles.InternalRequest)]
        public ActionResult About() => View();

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
                .AppendLine("Request Info")
                .Append("  IP: ").AppendLine(Current.RequestIP)
                .Append("  User: ").AppendLine(Current.User.AccountName)
                .Append("  Roles: ").AppendLine(Current.User.Role.ToString())
                .AppendLine()
                .AppendLine("Headers");
            foreach (string k in Request.Headers.Keys)
            {
                sb.AppendFormat("  {0}: {1}\n", k, Request.Headers[k]);
            }

            var ps = PollingEngine.GetPollingStatus();
            sb.AppendLine()
              .AppendLine("Polling Info")
              .AppendLine(ps.GetPropertyNamesAndValues(prefix: "  "));
            return TextPlain(sb.ToStringRecycle());
        }

        [Route("error-test")]
        public ActionResult ErrorTestPage()
        {
            Current.LogException(new Exception("Test Exception via GlobalApplication.LogException()"));

#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
            throw new NotImplementedException("I AM IMPLEMENTED, I WAS BORN TO THROW ERRORS!");
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.
        }
    }
}
