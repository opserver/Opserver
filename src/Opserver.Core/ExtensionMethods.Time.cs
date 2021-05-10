using System;

namespace Opserver
{
    public static partial class ExtensionMethods
    {
        // TODO: Convert these to TicksPerSecond constructor, faster.
        public static TimeSpan Seconds(this int seconds) => TimeSpan.FromSeconds(seconds);
        public static TimeSpan Minutes(this int minutes) => TimeSpan.FromMinutes(minutes);
        public static TimeSpan Hours(this int hours) => TimeSpan.FromHours(hours);
        public static TimeSpan Days(this int days) => TimeSpan.FromDays(days);
        // https://stackoverflow.com/a/20046261/871146
        public static DateTime RoundDown(this DateTime dt, TimeSpan d)
        {
            var delta = dt.Ticks % d.Ticks;
            return new DateTime(dt.Ticks - delta, dt.Kind);
        }
    }
}
