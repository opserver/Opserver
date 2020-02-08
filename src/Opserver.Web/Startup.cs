using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Opserver.Data;
using Opserver.Helpers;
using Opserver.Security;
using StackExchange.Profiling;

namespace Opserver
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
            // Register Opserver.Core config and polling
            services.AddCoreOpserverServices(_configuration);

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
                    settings.DataIncludeRegex = new Regex("^(Redis|Elastic|ErrorLog|Jil)", RegexOptions.Singleline | RegexOptions.Compiled);
                    settings.GetCustomData = (ex, data) =>
                    {
                        // everything below needs a context
                        // Don't *init* a user here, since that'll stack overflow when it errors
                        var u = Current.Context?.UserIfExists;
                        if (u != null)
                        {
                            data.Add("User", u.AccountName);
                            data.Add("Roles", u.Roles.ToString());
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

            services.AddSingleton<IConfigureOptions<MiniProfilerOptions>, MiniProfilerCacheStorageDefaults>();
            services.AddMiniProfiler(options =>
            {
                //options.RouteBasePath = "/profiler/";
                options.PopupRenderPosition = RenderPosition.Left;
                options.PopupMaxTracesToShow = 5;
                options.ShouldProfile = _ =>
                {
                    return true;
                    //switch (SiteSettings.ProfilingMode)
                    //{
                    //    case ProfilingModes.Enabled:
                    //        return true;
                    //    case SiteSettings.ProfilingModes.LocalOnly:
                    //        return Current.User?.Is(Models.Roles.LocalRequest) == true;
                    //    case SiteSettings.ProfilingModes.AdminOnly:
                    //        return Current.User?.IsGlobalAdmin == true;
                    //    default:
                    //        return false;
                    //}
                };
                options.EnableServerTimingHeader = true;
                options.IgnorePath("/graph")
                       .IgnorePath("/login")
                       .IgnorePath("/spark")
                       .IgnorePath("/top-refresh");
            });
            services.Configure<SecuritySettings>(_configuration.GetSection("Security"));
            services.AddSingleton<SecurityManager>();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);
            services.AddMvc();
        }

        public void Configure(
            IApplicationBuilder appBuilder,
            IOptions<OpserverSettings> settings,
            SecurityManager securityManager,
            IEnumerable<StatusModule> modules
        )
        {
            appBuilder.UseStaticFiles()
                      .UseExceptional()
                      .UseRouting()
                      .UseMiniProfiler()
                      .UseAuthentication()
                      .UseAuthorization()
                      .Use((httpContext, next) =>
                      {
                          Current.SetContext(new Current.CurrentContext(securityManager.CurrentProvider, httpContext, modules));
                          return next();
                      })
                      .UseResponseCaching()
                      .UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());
            NavTab.ConfigureAll(modules); // TODO: UseNavTabs() or something
            Cache.Configure(settings);
        }
    }
}
