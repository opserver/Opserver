using System;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Http;
using StackExchange.Exceptional;
using Opserver.Helpers;
using Opserver.Models;
using Opserver.Security;
using System.Collections.Generic;

namespace Opserver
{
    public static class Current
    {
        private static readonly AsyncLocal<CurrentContext> _context = new AsyncLocal<CurrentContext>();
        public static CurrentContext Context => _context.Value;
        public static void SetContext(CurrentContext context) => _context.Value = context;

        public class CurrentContext
        {
            private readonly IEnumerable<StatusModule> _modules;

            /// <summary>
            /// The security provider for this context.
            /// </summary>
            public SecurityProvider Security { get; }

            /// <summary>
            /// Shortcut to HttpContext.Current.
            /// </summary>
            public HttpContext HttpContext { get; }

            /// <summary>
            /// The current top level tab we're on.
            /// </summary>
            public NavTab NavTab { get; set; }

            private string _theme;
            /// <summary>
            /// The current theme were on.
            /// </summary>
            public string Theme => _theme ?? (_theme = Helpers.Theme.Get(HttpContext.Request));

            private User _user;
            /// <summary>
            /// Gets the current user from the request.
            /// </summary>
            public User User
            {
                get
                {
                    if (_user == null)
                    {
                        // Calc request-based roles
                        var roles = Roles.None;
                        if (IPAddress.IsLoopback(HttpContext.Connection.RemoteIpAddress))
                        {
                            roles |= Roles.LocalRequest;
                        }
                        if (Security.IsInternalIP(RequestIP))
                        {
                            roles |= Roles.InternalRequest;
                        }
                        if (Security.IsValidApiKey(Request?.Query["key"]))
                        {
                            roles |= Roles.ApiRequest;
                        }

                        _user = new User(Security, HttpContext.User, roles, _modules);
                    }
                    return _user;
                }
            }

            public CurrentContext(SecurityProvider security, HttpContext httpContext, IEnumerable<StatusModule> modules)
            {
                Security = security;
                HttpContext = httpContext;
                _modules = modules;
            }
        }

        /// <summary>
        /// Shortcut to HttpContext.Current.Request.
        /// </summary>
        public static HttpRequest Request => Context.HttpContext.Request;

        /// <summary>
        /// The security provider for this context.
        /// </summary>
        public static SecurityProvider Security => Context.Security;

        /// <summary>
        /// Gets the current user from the request.
        /// </summary>
        public static User User => Context.User;

        /// <summary>
        /// Gets the theme for the current request.
        /// </summary>
        public static string Theme => Context.Theme;

        /// <summary>
        /// Gets or set the top tab for this request.
        /// </summary>
        public static NavTab NavTab
        {
            get => Context.NavTab;
            set => Context.NavTab = value;
        }

        /// <summary>
        /// Gets the IP this request came from, gets the real IP when behind a proxy
        /// </summary>
        public static string RequestIP => Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

        /// <summary>
        /// Manually write an exception to our standard exception log.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="innerException">The inner exception to log.</param>
        public static void LogException(string message, Exception innerException) => LogException(new Exception(message, innerException));

        /// <summary>
        /// Manually write an exception to our standard exception log.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to log.</param>
        public static void LogException(Exception exception) => exception.Log(Context.HttpContext);
    }
}
