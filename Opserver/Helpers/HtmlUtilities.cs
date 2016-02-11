using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace StackExchange.Opserver.Helpers
{
    public static partial class HtmlUtilities
    {
        // filters control characters but allows only properly-formed surrogate sequences
        private static readonly Regex SanitizeUrlRegex = new Regex(@"[^-a-z0-9+&@#/%?=~_|!:,.;\(\)]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        /// <summary>
        /// This string is already correctly encoded html and can be sent to the client "as is" without additional encoding.
        /// </summary>
        public static IHtmlString AsHtml(this string html) => MvcHtmlString.Create(html);
        
        /// <summary>
        /// returns "safe" URL, stripping anything outside normal charsets for URL
        /// </summary>
        public static string SanitizeUrl(string url) => url.IsNullOrEmpty() ? url : SanitizeUrlRegex.Replace(url, "");
        
        /// <summary>
        /// fast (and maybe a bit inaccurate) check to see if the querystring contains the specified key
        /// </summary>
        public static bool QueryStringContains(string url, string key) => url.Contains(key + "=");

        /// <summary>
        /// removes the specified key, and any value, from the querystring. 
        /// for www.example.com/bar.foo?x=1&amp;y=2&amp;z=3 if you pass "y" you'll get back 
        /// www.example.com/bar.foo?x=1&amp;z=3
        /// </summary>
        public static string QueryStringRemove(string url, string key) => url.IsNullOrEmpty() ? "" : Regex.Replace(url, @"[?&]" + key + "=[^&]*", "");

        /// <summary>
        /// returns the value, if any, of the specified key in the querystring
        /// </summary>
        public static string QueryStringValue(string url, string key) => url.IsNullOrEmpty() ? "" : Regex.Match(url, key + "=.*").ToString().Replace(key + "=", "");
    }
}
