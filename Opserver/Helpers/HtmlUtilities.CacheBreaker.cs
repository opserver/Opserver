using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;

namespace StackExchange.Opserver.Helpers
{
    public partial class HtmlUtilities
    {
        public static IHtmlString CacheBreaker(this UrlHelper url, string path) => MvcHtmlString.Create(GetCacheBreakerUrl(path));

        /// <summary>
        /// Given the URL to a static file, returns the URL together with a cache breaker, i.e. ?v=123abc... appended.
        /// The cache breaker will always be based on the local version of the file, even if the URL points to sstatic.net,
        /// and only calculated once.
        /// </summary>
        public static string GetCacheBreakerUrl(string path) => CacheBreakerUrls.GetOrAdd(path, CalculateCacheBreakerUrl);

        internal static IEnumerable<KeyValuePair<string, string>> GetAllCacheBreakerUrls() => CacheBreakerUrls.ToList();

        private static readonly ConcurrentDictionary<string, string> CacheBreakers = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, string> CacheBreakerUrls = new ConcurrentDictionary<string, string>();

        private static string CalculateCacheBreakerUrl(string path)
        {
            string file;

            if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
            {
                var webPath = new Uri(path, UriKind.Absolute);
                file = "/content/" + webPath.AbsolutePath;
            }
            else
                file = path;
            
            var breaker = GetCacheBreakerForLocalFile(file);
            
            if (path.StartsWith("~/"))
                path = HostingEnvironment.ApplicationVirtualPath == "/"
                    ? path.Substring(1)
                    : HostingEnvironment.ApplicationVirtualPath + "/" + path.Substring(2);

            if (breaker == null)
                return path;

            return path + "?v=" + breaker;
        }

        /// <summary>
        /// Returns the cache breaker for the given file; a 12-digit hex string that's guaranteed
        /// to be stable if the file contents don't change.
        /// </summary>
        /// <param name="path">The path to the file, relative to the application directory (this will usually start with "/content")</param>
        internal static string GetCacheBreakerForLocalFile(string path)
        {
            return CacheBreakers.GetOrAdd(path, CalculateBreaker);
        }

        private static string CalculateBreaker(string path)
        {
            var fullpath =  path.StartsWith("~/")
                ? HostingEnvironment.MapPath(path)
                : AppDomain.CurrentDomain.BaseDirectory + path;
            if (!File.Exists(fullpath))
                return null;

            var sha = new SHA1Managed();
            var hash = ToHex(sha.ComputeHash(File.ReadAllBytes(fullpath)));
            return hash.Substring(0, 12);
        }

        private static string ToHex(byte[] buffer)
        {
            var bits = buffer.Select(b => b.ToString("x2")).ToArray();
            return string.Join("", bits);
        }
    }
}