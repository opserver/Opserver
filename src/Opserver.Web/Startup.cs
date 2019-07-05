using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Security;
using StackExchange.Profiling;

namespace StackExchange.Opserver
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<OpserverSettings>(_configuration);
            services.AddTransient(s => s.GetRequiredService<IOptions<OpserverSettings>>().Value);
            services.AddStatusModules(_configuration);
            services.AddResponseCaching();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
                        options.AccessDeniedPath = "/denied";
                        options.LoginPath = "/login";
                        options.LogoutPath = "/logout";
                    });
            services.AddHttpContextAccessor()
                    .AddMemoryCache()
                    .AddExceptional(
                _configuration.GetSection("Exceptional"),
                settings =>
                {
                    settings.UseExceptionalPageOnThrow = true;
                    settings.GetCustomData = (ex, data) =>
                    {
                        // everything below needs a context
                        if (Current.Context != null && Current.User != null)
                        {
                            data.Add("User", Current.User.AccountName);
                            data.Add("Roles", Current.User.Roles.ToString());
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
                    };
                });

            services.AddSingleton<IHostedService, PollingService>();
            services.AddSingleton<IConfigureOptions<MiniProfilerOptions>, MiniProfilerCacheStorageDefaults>();
            //services.AddMiniProfiler(options =>
            //{
            //    options.RouteBasePath = "/profiler/";
            //    options.PopupRenderPosition = RenderPosition.Left;
            //    options.PopupMaxTracesToShow = 5;
            //    options.ShouldProfile = req =>
            //    {
            //        var conn = req.HttpContext.Connection;
            //        switch (SiteSettings.ProfilingMode)
            //        {
            //            case SiteSettings.ProfilingModes.Enabled:
            //                return true;
            //            case SiteSettings.ProfilingModes.LocalOnly:
            //                return conn.RemoteIpAddress.Equals(conn.LocalIpAddress) || IPAddress.IsLoopback(conn.RemoteIpAddress);
            //            case SiteSettings.ProfilingModes.AdminOnly:
            //                return Current.User?.IsGlobalAdmin == true;
            //            default:
            //                return false;
            //        }
            //    };
            //    options.IgnorePath("/graph")
            //           .IgnorePath("/login")
            //           .IgnorePath("/spark")
            //           .IgnorePath("/top-refresh");
            //});
            services.Configure<SecuritySettings>(_configuration.GetSection("Security"));
            services.AddSingleton<SecurityManager>();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);
            services.AddMvc();
        }

        public void Configure(
            IApplicationBuilder appBuilder,
            IHostApplicationLifetime appLifetime,
            IOptions<OpserverSettings> settings,
            SecurityManager securityManager,
            IEnumerable<StatusModule> modules
        )
        {
            //appBuilder.UseStaticFiles()
            //          .UseExceptional()
            //          //.UseMiniProfiler()
            //          .UseAuthentication()
            //          .Use(async (httpContext, next)  =>
            //          {
            //              Current.SetContext(new Current.CurrentContext(securityManager.CurrentProvider, httpContext));
            //              await next();
            //          })
            //          .UseAuthorization()
            //          .UseRouting()
            //          .UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());

            appBuilder.UseStaticFiles()
                      .UseRouting()
                      .UseExceptional()
                      //.UseMiniProfiler()
                      .UseAuthentication()
                      .UseAuthorization()
                      .Use(async (httpContext, next) =>
                      {
                          Current.SetContext(new Current.CurrentContext(securityManager.CurrentProvider, httpContext));
                          await next();
                      })
                      .UseResponseCaching()
                      .UseEndpoints(endpoints => {
                          endpoints.MapDefaultControllerRoute();
                      });
            appLifetime.ApplicationStopping.Register(OnShutdown);
            NavTab.ConfigureAll(modules); // TODO: UseNavTabs() or something
            Cache.Configure(settings);
        }

        private void OnShutdown()
        {
            PollingEngine.StopPolling();
        }
    }
}
