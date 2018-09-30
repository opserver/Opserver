using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Helpers;
using StackExchange.Profiling;
using StackExchange.Profiling.Mvc;

namespace StackExchange.Opserver
{
    public class GlobalApplication : HttpApplication
    {
        /// <summary>
        /// The time this application was spun up.
        /// </summary>
        public static readonly DateTime StartDate = DateTime.UtcNow;

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{*allaspx}", new { allaspx = @".*\.aspx(/.*)?" });

            // any controller methods that are decorated with our attribute will be registered
            routes.MapMvcAttributeRoutes();

            // MUST be the last route as a catch-all!
            routes.MapRoute("", "{*url}", new { controller = "Home", action = "PageNotFound" });
        }

        private static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(
                new ScriptBundle("~/scripts/plugins.js")
                    .Include("~/Content/bootstrap/js/bootstrap.min.js")
                    .IncludeDirectory("~/Content/js/plugins", "*.js"));
            bundles.Add(
                new ScriptBundle("~/scripts/scripts.js")
                    .Include("~/Content/js/Scripts*"));
        }

        public override void Init()
        {
            base.Init();

            OpserverCore.Init();
        }

        protected void Application_Start()
        {
            // disable the X-AspNetMvc-Version: header
            MvcHandler.DisableMvcResponseHeader = true;

            AreaRegistration.RegisterAllAreas();
            RegisterRoutes(RouteTable.Routes);
            RegisterBundles(BundleTable.Bundles);
            //BundleTable.EnableOptimizations = true;

            SetupMiniProfiler();

            Exceptional.Exceptional.Settings.GetCustomData = GetCustomErrorData;

            TaskScheduler.UnobservedTaskException += (sender, args) => Current.LogException(args.Exception);

            // enable custom model binder
            ModelBinders.Binders.DefaultBinder = new ProfiledModelBinder();

            // When settings change, reload the app pool
            Current.Settings.OnChanged += HttpRuntime.UnloadAppDomain;

            PollingEngine.Configure(t => HostingEnvironment.QueueBackgroundWorkItem(_ => t()));
        }

        protected void Application_End()
        {
            PollingEngine.StopPolling();
        }

        private static void SetupMiniProfiler()
        {
            var options = MiniProfiler.Configure(new MiniProfilerOptions()
            {
                RouteBasePath = "~/profiler/",
                PopupRenderPosition = RenderPosition.Left,
                PopupMaxTracesToShow = 5,
                Storage = new MiniProfilerCacheStorage(TimeSpan.FromMinutes(10)),
                ProfilerProvider = new AspNetRequestProvider(true)
            }.IgnorePath("/graph")
             .IgnorePath("/login")
             .IgnorePath("/spark")
             .IgnorePath("/top-refresh")
             .AddViewProfiling()
            );

            Cache.EnableProfiling = SiteSettings.PollerProfiling;
            Cache.LogExceptions = SiteSettings.LogPollerExceptions;
        }

        protected void Application_BeginRequest()
        {
            if (ShouldProfile())
                MiniProfiler.StartNew();
        }

        protected void Application_EndRequest()
        {
            MiniProfiler.Current?.Stop();
        }

        private static void GetCustomErrorData(Exception ex, Dictionary<string, string> data)
        {
            // everything below needs a context
            if (Current.Context != null && Current.User != null)
            {
                data.Add("User", Current.User.AccountName);
                data.Add("Roles", Current.User.RawRoles.ToString());
            }

            while (ex != null)
            {
                foreach (DictionaryEntry de in ex.Data)
                {
                    var key = de.Key as string;
                    if (key.HasValue() && key.StartsWith(ExtensionMethods.ExceptionLogPrefix))
                    {
                        data.Add(key.Replace(ExtensionMethods.ExceptionLogPrefix, ""), de.Value?.ToString() ?? "");
                    }
                }
                ex = ex.InnerException;
            }
        }

        private static bool ShouldProfile()
        {
            switch (SiteSettings.ProfilingMode)
            {
                case SiteSettings.ProfilingModes.Enabled:
                    return true;
                case SiteSettings.ProfilingModes.LocalOnly:
                    return HttpContext.Current.Request.IsLocal;
                case SiteSettings.ProfilingModes.AdminOnly:
                    return Current.User?.IsGlobalAdmin == true;
                default:
                    return false;
            }
        }
    }
}
