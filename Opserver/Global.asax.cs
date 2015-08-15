using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using StackExchange.Exceptional;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Monitoring;
using StackExchange.Profiling;
using StackExchange.Opserver.Helpers;
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
            routes.MapRoute("", "{*url}", new { controller = "Error", action = "PageNotFound" });
        }

        private static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/scripts/plugins.js").IncludeDirectory("~/Content/js/plugins", "*.js"));
            bundles.Add(new ScriptBundle("~/scripts/scripts.js").Include("~/Content/js/Scripts*"));
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

            ErrorStore.GetCustomData = GetCustomErrorData;

            TaskScheduler.UnobservedTaskException += (sender, args) => Current.LogException(args.Exception);

            // enable custom model binder
            ModelBinders.Binders.DefaultBinder = new ProfiledModelBinder();
        }

        protected void Application_End()
        {
            PollingEngine.StopPolling();
        }

        private static void SetupMiniProfiler()
        {
            MiniProfiler.Settings.RouteBasePath = "~/profiler/";
            MiniProfiler.Settings.PopupRenderPosition = RenderPosition.Left;
            var paths = MiniProfiler.Settings.IgnoredPaths.ToList();
            paths.Add("/graph/");
            paths.Add("/login");
            MiniProfiler.Settings.IgnoredPaths = paths.ToArray();
            MiniProfiler.Settings.PopupMaxTracesToShow = 5;
            MiniProfiler.Settings.ProfilerProvider = new OpserverProfileProvider();
            OpserverProfileProvider.EnablePollerProfiling = SiteSettings.PollerProfiling;

            var copy = ViewEngines.Engines.ToList();
            ViewEngines.Engines.Clear();
            foreach (var item in copy)
            {
                ViewEngines.Engines.Add(new ProfilingViewEngine(item));
            }
        }

        protected void Application_BeginRequest()
        {
            Current.LogRequest();
            if (ShouldProfile())
                MiniProfiler.Start();
        }

        protected void Application_EndRequest()
        {
            if (ShouldProfile())
                MiniProfiler.Stop();
        }

        public override string GetVaryByCustomString(HttpContext context, string arg)
        {
            if (arg.ToLower() == "highDPI")
            {
                return Current.IsHighDPI.ToString();
            }
            return base.GetVaryByCustomString(context, arg);
        }

        private static void GetCustomErrorData(Exception ex, HttpContext context, Dictionary<string, string> data)
        {
            // everything below needs a context
            if (Current.Context != null)
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
                    return Current.User != null && Current.User.IsGlobalAdmin;
                default:
                    return false;
            }
        }
    }
}