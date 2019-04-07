using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using StackExchange.Exceptional;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.SettingsProviders;

namespace StackExchange.Opserver
{
    public static partial class Current
    {
        private static IHttpContextAccessor _httpAccessor;
        public static void Init(IHttpContextAccessor accessor) => _httpAccessor = accessor;

        public static SettingsProvider Settings => SettingsProvider.Current;

        public static LocalCache LocalCache => CoreCurrent.LocalCache;

        /// <summary>
        /// Shortcut to HttpContext.Current.
        /// </summary>
        public static HttpContext Context => _httpAccessor.HttpContext;

        /// <summary>
        /// Shortcut to HttpContext.Current.Request.
        /// </summary>
        public static HttpRequest Request => Context.Request;

        /// <summary>
        /// Is the current request ajax? Determined by checking the X-Requested-With header
        /// </summary>
        public static bool IsAjaxRequest => Request != null && Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        /// <summary>
        /// Gets the current user from the request
        /// </summary>
        public static User User => Context.User as User;

        public static bool IsSecureConnection => Request.IsHttps;

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
            exception.Log(Context);
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
