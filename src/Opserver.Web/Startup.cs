using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
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

            services.AddResponseCompression(
                options =>
                {
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "image/svg+xml" });
                    options.Providers.Add<GzipCompressionProvider>();
                    options.EnableForHttps = true;
                }
            );

            services
                .AddHttpContextAccessor()
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
                    }
                );

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

        private static readonly StringValues DefaultCacheControl = new CacheControlHeaderValue
        {
            Private = true
        }.ToString();

        private static readonly StringValues StaticContentCacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromDays(365)
        }.ToString();

        public void Configure(
            IApplicationBuilder appBuilder,
            IOptions<OpserverSettings> settings,
            SecurityManager securityManager,
            IEnumerable<StatusModule> modules
        )
        {
            appBuilder.UseResponseCompression()
                      .UseStaticFiles(new StaticFileOptions
                      {
                          OnPrepareResponse = ctx =>
                          {
                              if (ctx.Context.Request.Query.ContainsKey("v")) // If cache-breaker versioned, cache for a year
                              {
                                  ctx.Context.Response.Headers[HeaderNames.CacheControl] = StaticContentCacheControl;
                              }
                          }
                      })
                      .UseExceptional()
                      .UseRouting()
                      .UseMiniProfiler()
                      .UseAuthentication()
                      .UseAuthorization()
                      .Use((httpContext, next) =>
                      {
                          httpContext.Response.Headers[HeaderNames.CacheControl] = DefaultCacheControl;

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
