using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace StackExchange.Opserver.Helpers
{
    public static class Theme
    {
        public static List<string> Options { get; } = new List<string> { "light", "dark" };

        public const string Default = "light";
        private const string CookieName = "Op-Theme";

        // Cookies need an expiration date! A sliding time period seems downright silly so...
        // I chose at random from https://en.wikipedia.org/wiki/List_of_dates_predicted_for_apocalyptic_events
        // "Members predict that the world will end in 2026, when an asteroid would collide with Earth..."
        private static readonly DateTime CookieExpirationDate = new DateTime(2026, 1, 1);

        public static string Current => Opserver.Current.Request.Cookies[CookieName] ?? Default;

        public static void Set(string theme, HttpResponse response)
        {
            if (Options.Contains(theme))
            {
                response.Cookies.Append(CookieName, theme, new CookieOptions() { Expires = CookieExpirationDate });
            }
        }
    }
}
