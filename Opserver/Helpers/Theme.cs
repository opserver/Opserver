using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;

namespace StackExchange.Opserver.Helpers
{
    public class Theme
    {
        public static List<string> Options { get; } 

        public const string Default = "light";
        private const string CookieName = "Op-Theme";

        // Cookies need an expiration date! A sliding time period seems downright silly so...
        // I chose at random from https://en.wikipedia.org/wiki/List_of_dates_predicted_for_apocalyptic_events
        // "Members predict that the world will end in 2026, when an asteroid would collide with Earth..."
        private static readonly DateTime CookieExpirationDate = new DateTime(2026, 1, 1);
        
        public static string Current => Opserver.Current.Request.Cookies[CookieName]?.Value ?? Default;

        static Theme()
        {
            var folder = HostingEnvironment.MapPath("~/Content/themes/");
            Options = Directory.EnumerateDirectories(folder)
                               .Select(d => new DirectoryInfo(d).Name)
                               .Where(f => !f.StartsWith("_"))
                               .OrderBy(f => f != Default)
                               .ThenBy(f => f).ToList();
        }


        public static void Set(string theme)
        {
            if (Options.Contains(theme))
            {
                HttpContext.Current.Response.Cookies.Add(new HttpCookie(CookieName)
                {
                    Expires = CookieExpirationDate,
                    Value = theme
                });
            }
        }
    }
}