using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace StackExchange.Opserver.Helpers
{
    public static partial class HtmlUtilities
    {
        /// <summary>
        /// This string is already correctly encoded html and can be sent to the client "as is" without additional encoding.
        /// </summary>
        public static IHtmlString AsHtml(this string html)
        {
            return MvcHtmlString.Create(html);
        }
        /// <summary>
        /// Encodes an email address for use in a mailto: link
        /// </summary>
        public static IHtmlString MailtoAddressEncode(this string emailAddress)
        {
            return emailAddress.IsNullOrEmpty()
                       ? MvcHtmlString.Empty
                       : emailAddress.UrlEncode().Replace("%40", "@").AsHtml();
        }

        public static bool HasValue(this IHtmlString html)
        {
            return !string.IsNullOrEmpty(html?.ToHtmlString());
        }
        [Obsolete("This .AsHtml() call is redundant", false)]
        public static IHtmlString AsHtml(this IHtmlString html)
        {
            return html ?? MvcHtmlString.Empty;
        }
        public static IHtmlString AsHtml(this StringBuilder html)
        {
            return html == null ? MvcHtmlString.Empty : html.ToString().AsHtml();
        }
        public static string ToStringOrNull(this IHtmlString html)
        {
            return html?.ToHtmlString();
        }
        public static string Decode(this IHtmlString html)
        {
            if (html == null) return null;
            var s = html.ToHtmlString();
            return string.IsNullOrEmpty(s) ? s : HttpUtility.HtmlDecode(s);
        }

        // filters control characters but allows only properly-formed surrogate sequences
        private static readonly Regex _invalidXMLChars = new Regex(@"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]",
            RegexOptions.Compiled);
        /// <summary>
        /// removes any unusual unicode characters that can't be encoded into XML
        /// </summary>
        public static string RemoveInvalidXMLChars(string text)
        {
            return text.IsNullOrEmpty() ? "" : _invalidXMLChars.Replace(text, "");
        }

        private static readonly Regex _autolinks = new Regex(@"(\b(?:https?|ftp)://[A-Za-z0-9][-A-Za-z0-9+&@#/%?=~_|$!:,.;\[\]\(\)]*[-A-Za-z0-9+&@#/%=~_|$\[\]])",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// returns true if the provided text contains a semi-valid URL
        /// </summary>
        public static bool IsUrl(string text)
        {
            return _autolinks.IsMatch(text);
        }

        private static readonly Regex _urlprotocol = new Regex(@"^(https?|ftp)://(www\.)?|(/$)", RegexOptions.Compiled);

        /// <summary>
        /// removes the protocol (and trailing slash, if present) from the URL; given 
        /// "http://www.example.com/" returns "example.com"
        /// </summary>
        public static string RemoveUrlProtocol(string url)
        {
            return _urlprotocol.Replace(url, "");
        }

        /// <summary>
        /// returns Html Encoded string, suitable for use in
        /// &lt;html&gt;stuff-goes-here&lt;/html&gt;
        /// </summary>
        public static string Encode(string s)
        {
            return HttpUtility.HtmlEncode(s);
        }

        /// <summary>
        /// returns Url Encoded string, suitable for use in 
        /// http://example.com/stuff-goes-here
        /// </summary>
        public static string UrlEncode(string s)
        {
            return HttpUtility.UrlEncode(s);
        }

        /// <summary>
        /// returns QueryString encoded string, suitable for use in
        /// http://example.com/q=stuff-goes-here
        /// </summary>
        public static string QueryStringEncode(string s)
        {
            return HttpUtility.UrlEncode(s).Replace("+", "%20");
        }
        
        /// <summary>
        /// removes any &gt; or &lt; characters from the input
        /// </summary>
        public static string RemoveTagChars(string s)
        {
            return s.IsNullOrEmpty() ? s : s.Replace("<", "").Replace(">", "");
        }

        private static readonly Regex _sanitizeUrl = new Regex(@"[^-a-z0-9+&@#/%?=~_|!:,.;\(\)]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// returns "safe" URL, stripping anything outside normal charsets for URL
        /// </summary>
        public static string SanitizeUrl(string url)
        {
            return url.IsNullOrEmpty() ? url : _sanitizeUrl.Replace(url, "");
        }

        private static readonly Regex _sanitizeUrlAllowSpaces = new Regex(@"[^-a-z0-9+&@#/%?=~_|!:,.;\(\) ]",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// returns "safe" URL, stripping anything outside normal charsets for URL
        /// </summary>
        public static string SanitizeUrlAllowSpaces(string url)
        {
            return url.IsNullOrEmpty() ? url : _sanitizeUrlAllowSpaces.Replace(url, "");
        }

        /// <summary>
        /// fast (and maybe a bit inaccurate) check to see if the querystring contains the specified key
        /// </summary>
        public static bool QueryStringContains(string url, string key)
        {
            return url.Contains(key + "=");
        }

        /// <summary>
        /// removes the specified key, and any value, from the querystring. 
        /// for www.example.com/bar.foo?x=1&amp;y=2&amp;z=3 if you pass "y" you'll get back 
        /// www.example.com/bar.foo?x=1&amp;z=3
        /// </summary>
        public static string QueryStringRemove(string url, string key)
        {
            return url.IsNullOrEmpty() ? "" : Regex.Replace(url, @"[?&]" + key + "=[^&]*", "");
        }

        /// <summary>
        /// returns the value, if any, of the specified key in the querystring
        /// </summary>
        public static string QueryStringValue(string url, string key)
        {
            return url.IsNullOrEmpty() ? "" : Regex.Match(url, key + "=.*").ToString().Replace(key + "=", "");
        }
    }
}
