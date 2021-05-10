using System;
using Opserver.Views.Shared;
using Opserver.Data;
using Opserver.Helpers;
using Opserver.Models;
using Opserver.Views.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Opserver.Data.Dashboard;
using Opserver.Data.SQL;
using Opserver.Data.Redis;
using Opserver.Data.Exceptions;
using Opserver.Data.Elastic;
using Opserver.Data.HAProxy;
using System.Collections.Generic;

namespace Opserver.Controllers
{
    [OnlyAllow(Roles.Authenticated)]
    public class HomeController : StatusController
    {
        private PollingService Poller { get; }
        private IEnumerable<StatusModule> Modules { get; }
        private DashboardModule Dashboard { get; }
        private SQLModule Sql { get; }
        private RedisModule Redis { get; }
        private ElasticModule Elastic { get; }
        private ExceptionsModule Exceptions { get; }
        private HAProxyModule HAProxy { get; }

        public HomeController(
            IOptions<OpserverSettings> _settings,
            PollingService poller,
            IEnumerable<StatusModule> modules,
            DashboardModule dashboard,
            SQLModule sql,
            RedisModule redis,
            ElasticModule elastic,
            ExceptionsModule exceptions,
            HAProxyModule haproxy
            ) : base(_settings)
        {
            Poller = poller;
            Modules = modules;
            Dashboard = dashboard;
            Sql = sql;
            Redis = redis;
            Elastic = elastic;
            Exceptions = exceptions;
            HAProxy = haproxy;
        }

        [DefaultRoute("")]
        public ActionResult Home()
        {
            // TODO: Order
            foreach (var m in Modules)
            {
                //if (m.Enabled && m.SecuritySettings)
                //    return RedirectToAction()...
            }

            static bool AllowMeMaybe(StatusModule m) => m.Enabled && Current.User.HasAccess(m);

            if (AllowMeMaybe(Dashboard))
                return RedirectToAction(nameof(DashboardController.Dashboard), "Dashboard");
            if (AllowMeMaybe(Sql))
                return RedirectToAction(nameof(SQLController.Dashboard), "SQL");
            if (AllowMeMaybe(Redis))
                return RedirectToAction(nameof(RedisController.Dashboard), "Redis");
            if (AllowMeMaybe(Elastic))
                return RedirectToAction(nameof(ElasticController.Dashboard), "Elastic");
            if (AllowMeMaybe(Exceptions))
                return RedirectToAction(nameof(ExceptionsController.Exceptions), "Exceptions");
            if (AllowMeMaybe(HAProxy))
                return RedirectToAction(nameof(HAProxyController.Dashboard), "HAProxy");

            return View("NoConfiguration");
        }

        [Route("ping"), HttpGet, HttpHead, AllowAnonymous, AlsoAllow(Roles.InternalRequest)]
        public ActionResult Ping()
        {
            return Ok();
        }

        [Route("top-refresh")]
        public ActionResult TopRefresh(string tab)
        {
            Current.NavTab = NavTab.GetByName(tab);

            var vd = new TopRefreshModel
                {
                    Tab = tab
                };
            return PartialView(vd);
        }

        [Route("issues")]
        public ActionResult Issues() => PartialView();

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
            Theme.Set(theme, Response);
            return RedirectToAction(nameof(About));
        }

        [Route("debug"), AllowAnonymous]
        public ActionResult Debug()
        {
            var sb = StringBuilderCache.Get()
                .AppendLine("Request Info")
                .Append("  IP: ").AppendLine(Current.RequestIP)
                .Append("  User: ").AppendLine(Current.User.AccountName)
                .Append("  Roles: ").AppendLine(Current.User.Roles.ToString())
                .AppendLine()
                .AppendLine("Headers");
            foreach (string k in Request.Headers.Keys)
            {
                sb.AppendFormat("  {0}: {1}\n", k, Request.Headers[k]);
            }

            var ps = Poller.GetPollingStatus();
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
