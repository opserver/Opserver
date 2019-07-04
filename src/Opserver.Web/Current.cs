using System;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Http;
using StackExchange.Exceptional;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver
{
    public static partial class Current
    {
        private static readonly AsyncLocal<CurrentContext> _context = new AsyncLocal<CurrentContext>();
        public static CurrentContext Context => _context.Value;
        public static void SetContext(CurrentContext context) => _context.Value = context;

        public class CurrentContext
        {
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
                        if (SecuritySettings.Current?.ApiKey.HasValue() == true
                            && string.Equals(SecuritySettings.Current?.ApiKey, Request?.Query["key"]))
                        {
                            roles |= Roles.ApiRequest;
                        }

                        _user = new User(HttpContext.User, roles);
                    }
                    return _user;
                }
            }

            public CurrentContext(HttpContext httpContext)
            {
                HttpContext = httpContext;
            }
        }

        public static LocalCache LocalCache => CoreCurrent.LocalCache;

        /// <summary>
        /// Shortcut to HttpContext.Current.Request.
        /// </summary>
        public static HttpRequest Request => Context.HttpContext.Request;

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
        public static void LogException(string message, Exception innerException)
        {
            var ex = new Exception(message, innerException);
            LogException(ex);
        }

        /// <summary>
        /// Manually write an exception to our standard exception log.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to log.</param>
        /// <param name="key">(Optional) The throttle cache key.</param>
        public static void LogException(Exception exception, string key = null)
        {
            if (!ShouldLog(key)) return;
            exception.Log(Context.HttpContext);
            RecordLogged(key);
        }

        /// <summary>
        /// Record that an exception was logged in local cache for the specified length of time.
        /// </summary>
        /// <param name="key">The throttle cache key.</param>
        /// <param name="relogDelay">The duration of time to wait before logging again (default: 30 minutes).</param>
        private static void RecordLogged(string key, TimeSpan? relogDelay = null)
        {
            relogDelay = relogDelay ?? 30.Minutes();
            if (key.IsNullOrEmpty() || !relogDelay.HasValue) return;
            LocalCache.Set("ExceptionLogRetry-" + key, true, relogDelay.Value);
        }

        /// <summary>
        /// See if an exception with the given key should be logged, based on if it was logged recently.
        /// </summary>
        /// <param name="key">The throttle cache key.</param>
        private static bool ShouldLog(string key)
        {
            if (key.IsNullOrEmpty()) return true;
            return !LocalCache.Get<bool?>("ExceptionLogRetry-"+key).GetValueOrDefault();
        }
    }
}
