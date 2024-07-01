﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            services.Configure<ActiveDirectorySecuritySettings>(_configuration.GetSection("Security"));
            services.Configure<OIDCSecuritySettings>(_configuration.GetSection("Security"));
            services.Configure<ForwardedHeadersOptions>(_configuration.GetSection("ForwardedHeaders"));
            services.PostConfigure<ForwardedHeadersOptions>(
                options =>
                {
                    // what's all this mess I hear you cry? well ForwardedHeadersOptions
                    // has a bunch of read-only list props that can't be bound from configuration
                    // so we need to go populate them ourselves
                    var forwardedHeaders = _configuration.GetSection("ForwardedHeaders");
                    var allowedHosts = forwardedHeaders.GetSection(nameof(ForwardedHeadersOptions.AllowedHosts)).Get<List<string>>();
                    if (allowedHosts != null)
                    {
                        options.AllowedHosts.Clear();
                        foreach (var allowedHost in allowedHosts)
                        {
                            options.AllowedHosts.Add(allowedHost);
                        }
                    }

                    var knownProxies = forwardedHeaders.GetSection(nameof(ForwardedHeadersOptions.KnownProxies)).Get<List<string>>();
                    var knownNetworks = forwardedHeaders.GetSection(nameof(ForwardedHeadersOptions.KnownNetworks)).Get<List<string>>();
                    if (knownNetworks != null || knownProxies != null)
                    {
                        options.KnownProxies.Clear();
                        options.KnownNetworks.Clear();
                    }

                    if (knownProxies != null)
                    {
                        foreach (var knownProxy in knownProxies)
                        {
                            options.KnownProxies.Add(IPAddress.Parse(knownProxy));
                        }
                    }


                    if (knownNetworks != null)
                    {
                        foreach (var knownNetwork in knownNetworks)
                        {
                            var ipNet = IPNet.Parse(knownNetwork);
                            options.KnownNetworks.Add(new IPNetwork(ipNet.IPAddress, ipNet.CIDR));
                        }
                    }
                }
            );
            services.AddSingleton<SecurityManager>();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);
            services.AddMvc();
            services.AddHealthChecks();
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
            appBuilder
                      .UseForwardedHeaders()
                      .UseResponseCompression()
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
                      .UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute())
                      .UseHealthChecks("/health-checks/ready",
                        // Readiness:
                        // Signal that the application has started and is ready to accept traffic.
                        // This also allows the application to signal that it is live but currently
                        // cannot accept new requests because for example it's currently overloaded.
                        // Kubernetes will not restart the application if this endpoint returns unhealthy.
                        new HealthCheckOptions
                        {
                            AllowCachingResponses = false,
                            Predicate = registration => registration.Tags.Contains("ready")
                        })
                        // Liveliness:
                        // Signal that the application is running and is ready to accept traffic. This
                        // endpoint is used to continually determine whether the entire application is
                        // still in a healthy state.
                        // Kubernetes _will_ restart the application if this endpoint returns unhealthy.
                        .UseHealthChecks("/health-checks/live",
                            new HealthCheckOptions
                            {
                                AllowCachingResponses = false,
                                Predicate =
                                    _ => false // We don't use any healthchecks for liveliness, just that the app responds
                     });
            NavTab.ConfigureAll(modules); // TODO: UseNavTabs() or something
            Cache.Configure(settings);
        }
    }
}
