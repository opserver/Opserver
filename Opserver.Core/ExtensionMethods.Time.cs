using System;

namespace StackExchange.Opserver
{
    public static partial class ExtensionMethods
    {
        // TODO: Convert these to TicksPerSecond constructor, faster.
        public static TimeSpan Seconds(this int seconds) => TimeSpan.FromSeconds(seconds);
        public static TimeSpan Minutes(this int minutes) => TimeSpan.FromMinutes(minutes);
        public static TimeSpan Hours(this int hours) => TimeSpan.FromHours(hours);
        public static TimeSpan Days(this int days) => TimeSpan.FromDays(days);
        public static DateTime ToUniversalTime(this DateTime value, TimeZoneInfo sourceTimeZone)
        {
            switch (value.Kind)
            {
                case DateTimeKind.Unspecified:
                    return TimeZoneInfo.ConvertTimeToUtc(value, sourceTimeZone);
                case DateTimeKind.Local:
                    return value.ToUniversalTime();
                case DateTimeKind.Utc:
                default:
                    return value;
            }
        }
        public static DateTime? ToUniversalTime(this DateTime? value, TimeZoneInfo sourceTimeZone) => value?.ToUniversalTime(sourceTimeZone);
    }
}
