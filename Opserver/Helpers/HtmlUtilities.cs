using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace StackExchange.Opserver.Helpers
{
    public static partial class HtmlUtilities
    {
        // filters control characters but allows only properly-formed surrogate sequences
        private static readonly Regex InvalidXMLCharsRegex = new Regex(@"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]", RegexOptions.Compiled);
        private static readonly Regex AutolinksRegex = new Regex(@"(\b(?:https?|ftp)://[A-Za-z0-9][-A-Za-z0-9+&@#/%?=~_|$!:,.;\[\]\(\)]*[-A-Za-z0-9+&@#/%=~_|$\[\]])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SanitizeUrlRegex = new Regex(@"[^-a-z0-9+&@#/%?=~_|!:,.;\(\)]", RegexOptions.IgnoreCase | RegexOptions.Compiled);


        /// <summary>
        /// This string is already correctly encoded html and can be sent to the client "as is" without additional encoding.
        /// </summary>
        public static IHtmlString AsHtml(this string html) => MvcHtmlString.Create(html);

        /// <summary>
        /// removes any unusual unicode characters that can't be encoded into XML
        /// </summary>
        public static string RemoveInvalidXMLChars(string text) => text.IsNullOrEmpty() ? "" : InvalidXMLCharsRegex.Replace(text, "");
        
        /// <summary>
        /// returns true if the provided text contains a semi-valid URL
        /// </summary>
        public static bool IsUrl(string text) => AutolinksRegex.IsMatch(text);

        /// <summary>
        /// returns Html Encoded string, suitable for use in
        /// &lt;html&gt;stuff-goes-here&lt;/html&gt;
        /// </summary>
        public static string Encode(string s) => HttpUtility.HtmlEncode(s);

        /// <summary>
        /// returns Url Encoded string, suitable for use in 
        /// http://example.com/stuff-goes-here
        /// </summary>
        public static string UrlEncode(string s) => HttpUtility.UrlEncode(s);

        /// <summary>
        /// returns QueryString encoded string, suitable for use in
        /// http://example.com/q=stuff-goes-here
        /// </summary>
        public static string QueryStringEncode(string s) => HttpUtility.UrlEncode(s)?.Replace("+", "%20") ?? "";

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
