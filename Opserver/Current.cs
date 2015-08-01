using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.WebPages;
using StackExchange.Exceptional;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.SettingsProviders;

namespace StackExchange.Opserver
{
    public static partial class Current
    {
        public static SettingsProvider Settings => SettingsProvider.Current;

        public static LocalCache LocalCache => CoreCurrent.LocalCache;

        /// <summary>
        /// Shortcut to HttpContext.Current.
        /// </summary>
        public static HttpContext Context => HttpContext.Current;

        /// <summary>
        /// Shortcut to HttpContext.Current.Request.
        /// </summary>
        public static HttpRequest Request => Context.Request;

        /// <summary>
        /// Is the current request ajax? Determined by checking the X-Requested-With header
        /// </summary>
        public static bool IsAjaxRequest => Request != null && Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        /// <summary>
        /// Whether to render chart images at double resolution or not
        /// </summary>
        /// <remarks>Long-term, this will need updating to observe the pixel ratio which will grow to higher than 2 as displays improve</remarks>
        public static bool IsHighDPI
        {
            get
            {
                // HACK, Hackity hack hack hack, but it works for now.
                var cookie = Request.Cookies["highDPI"];
                return cookie != null && cookie.Value == "true";
            }
        }

        /// <summary>
        /// Gets if the current request is for a mobile view
        /// </summary>
        public static bool IsMobile => new HttpContextWrapper(Context).GetOverriddenBrowser().IsMobileDevice;

        /// <summary>
        /// Gets the current user from the request
        /// </summary>
        public static User User => Context.User as User;

        public static bool IsSecureConnection =>
            Request.IsSecureConnection ||
            // This can be "http", "https", or the more fun "https, http, https, https" even.
            (Request.Headers["X-Forwarded-Proto"] != null &&
             Request.Headers["X-Forwarded-Proto"].StartsWith("https"));

        private static readonly Regex _lastIpAddress = new Regex(@"\b([0-9]{1,3}\.){3}[0-9]{1,3}$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        
        /// <summary>
        /// Gets the IP this request came from, gets the real IP when behind a proxy
        /// </summary>
        public static string RequestIP
        {
            get
            {
                var ip = Request.ServerVariables["REMOTE_ADDR"];
                var ipForwarded = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                var realIp = Request.ServerVariables["HTTP_X_REAL_IP"];
                if (realIp.HasValue() && Request.ServerVariables["HTTP_X_FORWARDED_PROTO"] == "https")
                    return realIp;

                // check if we were forwarded from a proxy
                if (ipForwarded.HasValue())
                {
                    ipForwarded = _lastIpAddress.Match(ipForwarded).Value;
                    if (ipForwarded.HasValue() && !IsPrivateIP(ipForwarded))
                        ip = ipForwarded;
                }
                return ip.HasValue() ? ip : "0.0.0.0";
            }
        }

        private static bool IsPrivateIP(string s)
        {
            //TODO: convert to IPNet check and include 172.16.0.0/12
            return (s.StartsWith("192.168.") || s.StartsWith("10.") || s.StartsWith("127.0.0."));
        }

        /// <summary>
        /// manually write an exception to our standard exception log
        /// </summary>
        public static void LogException(string message, Exception innerException, string key = null, int? reLogDelaySeconds = null)
        {
            var ex = new Exception(message, innerException);
            LogException(ex);
        }

        /// <summary>
        /// manually write an exception to our standard exception log
        /// </summary>
        public static void LogException(Exception ex, string key = null, int? reLogDelaySeconds = null)
        {
            if (!ShouldLog(key)) return;
            ErrorStore.LogException(ex, Context, appendFullStackTrace: true);
            RecordLogged(key);
        }

        /// <summary>
        /// record that an exception was logged in local cache for the specified length of time (default of 30 minutes)
        /// </summary>
        private static void RecordLogged(string key, int? reLogDelaySeconds = 30 * 60 * 60)
        {
            if (key.IsNullOrEmpty() || !reLogDelaySeconds.HasValue) return;
            LocalCache.Set("ExceptionLogRetry-" + key, true, reLogDelaySeconds.Value);
        }

        /// <summary>
        /// see if an exception with the given key should be logged, based on if it was logged recently
        /// </summary>
        private static bool ShouldLog(string key)
        {
            if (key.IsNullOrEmpty()) return true;
            return !LocalCache.Get<bool?>("ExceptionLogRetry-"+key).GetValueOrDefault();
        }
        
        public static void LogRequest() { Interlocked.Increment(ref requestCount); }
        private static int requestCount;
    }
}