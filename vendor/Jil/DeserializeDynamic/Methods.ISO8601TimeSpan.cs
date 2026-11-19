using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Jil.Common;

namespace Jil.DeserializeDynamic
{
    static partial class Methods
    {
        static readonly ulong MinTicks = (ulong)(-TimeSpan.MinValue.Ticks);
        static readonly ulong MaxTicks = (ulong)TimeSpan.MaxValue.Ticks;

        public static bool ReadISO8601TimeSpan(string str, out TimeSpan ts)
        {
            const ulong TicksPerDay = 864000000000;

            const ulong TicksPerWeek = TicksPerDay * 7;
            const ulong TicksPerMonth = TicksPerDay * 30;
            const ulong TicksPerYear = TicksPerDay * 365;

            // Format goes like so:
            // - (-)P(([n]Y)([n]M)([n]D))(T([n]H)([n]M)([n]S))
            // - P[n]W

            if (str.Length == 0)
            {
                ts = default(TimeSpan);
                return false;
            }

            var ix = 0;
            var isNegative = false;

            var c = str[ix];
            if (c == '-')
            {
                isNegative = true;
                ix++;
            }

            if (ix >= str.Length)
            {
                ts = default(TimeSpan);
                return false;
            }

            c = str[ix];
            if (c != 'P')
            {
                ts = default(TimeSpan);
                return false;
            }

            ix++;   // skip 'P'

            long year, month, week, day;
            bool hasTimePart;
            if (!ISO8601TimeSpan_ReadDatePart(str, ref ix, out year, out month, out week, out day, out hasTimePart))
            {
                ts = default(TimeSpan);
                return false;
            }

            if (week != -1 && (year != -1 || month != -1 || day != -1))
            {
                ts = default(TimeSpan);
                return false;
            }

            if (week != -1 && hasTimePart)
            {
                ts = default(TimeSpan);
                return false;
            }

            if (year == -1) year = 0;
            if (month == -1) month = 0;
            if (week == -1) week = 0;
            if (day == -1) day = 0;

            ulong timeTicks;

            if (hasTimePart)
            {
                ix++;   // skip 'T'
                if (!ISO8601TimeSpan_ReadTimePart(str, ref ix, out timeTicks))
                {
                    ts = default(TimeSpan);
                    return false;
                }
            }
            else
            {
                timeTicks = 0;
            }

            ulong ticks = 0;
            if (year != 0)
            {
                ticks += ((ulong)year) * TicksPerYear;
            }

            if (month != 0)
            {
                // .NET (via XmlConvert) converts months to years
                // This isn't inkeeping with the spec, but of the bad choices... I choose this one
                var yearsFromMonths = ((ulong)month) / 12;
                var monthsAfterYears = ((ulong)month) % 12;
                ticks += (ulong)(yearsFromMonths * TicksPerYear + monthsAfterYears * TicksPerMonth);
            }

            if (week != 0)
            {
                // ISO8601 defines weeks as 7 days, so don't convert weeks to months or years (even if that may seem more sensible)
                ticks += ((ulong)week) * TicksPerWeek;
            }

            ticks += (ulong)(((ulong)day) * TicksPerDay + timeTicks);

            if (ticks >= MaxTicks && !isNegative)
            {
                ts = TimeSpan.MaxValue;
                return true;
            }

            if (ticks >= MinTicks && isNegative)
            {
                ts = TimeSpan.MinValue;
                return true;
            }

            ts = new TimeSpan((long)ticks);
            if (isNegative)
            {
                ts = ts.Negate();
            }

            return true;
        }

        static bool ISO8601TimeSpan_ReadDatePart(string str, ref int ix, out long year, out long month, out long week, out long day, out bool hasTimePart)
        {
            year = month = week = day = -1;
            hasTimePart = false;

            bool yearSeen, monthSeen, weekSeen, daySeen;
            yearSeen = monthSeen = weekSeen = daySeen = false;

            while (ix != str.Length)
            {
                if (str[ix] == 'T')
                {
                    hasTimePart = true;
                    return true;
                }

                int whole, fraction, fracLen;
                var part = ISO8601TimeSpan_ReadPart(str, ref ix, out whole, out fraction, out fracLen);

                if (fracLen != 0)
                {
                    return false;
                }

                if (part == 'Y')
                {
                    if (yearSeen)
                    {
                        return false;
                    }

                    if (monthSeen)
                    {
                        return false;
                    }

                    if (daySeen)
                    {
                        return false;
                    }

                    year = (long)whole;
                    yearSeen = true;
                    continue;
                }

                if (part == 'M')
                {
                    if (monthSeen)
                    {
                        return false;
                    }

                    if (daySeen)
                    {
                        return false;
                    }

                    month = (long)whole;
                    monthSeen = true;
                    continue;
                }

                if (part == 'W')
                {
                    if (weekSeen)
                    {
                        return false;
                    }

                    week = (long)whole;
                    weekSeen = true;
                    continue;
                }

                if (part == 'D')
                {
                    if (daySeen)
                    {
                        return false;
                    }

                    day = (long)whole;
                    daySeen = true;
                    continue;
                }

                return false;
            }

            return true;
        }

        static bool ISO8601TimeSpan_ReadTimePart(string str, ref int ix, out ulong ticks)
        {
            const ulong TicksPerHour = 36000000000;
            const ulong TicksPerMinute = 600000000;
            const ulong TicksPerSecond = 10000000;

            ticks = 0;

            bool hourSeen, minutesSeen, secondsSeen;
            hourSeen = minutesSeen = secondsSeen = false;

            var fracSeen = false;

            while (ix != str.Length)
            {
                if (fracSeen)
                {
                    return false;
                }

                int whole, fraction, fracLen;
                var part = ISO8601TimeSpan_ReadPart(str, ref ix, out whole, out fraction, out fracLen);

                if (fracLen != 0)
                {
                    fracSeen = true;
                }

                if (part == 'H')
                {
                    if (hourSeen)
                    {
                        return false;
                    }

                    if (minutesSeen)
                    {
                        return false;
                    }

                    if (secondsSeen)
                    {
                        return false;
                    }

                    ticks += (ulong)whole * TicksPerHour + ISO8601TimeSpan_FractionToTicks(9, fraction * 36, fracLen);
                    hourSeen = true;
                    continue;
                }

                if (part == 'M')
                {
                    if (minutesSeen)
                    {
                        return false;
                    }

                    if (secondsSeen)
                    {
                        return false;
                    }

                    ticks += (ulong)whole * TicksPerMinute + ISO8601TimeSpan_FractionToTicks(8, fraction * 6, fracLen);
                    minutesSeen = true;
                    continue;
                }

                if (part == 'S')
                {
                    if (secondsSeen)
                    {
                        return false;
                    }

                    ticks += (ulong)whole * TicksPerSecond + ISO8601TimeSpan_FractionToTicks(7, fraction, fracLen);
                    secondsSeen = true;
                    continue;
                }

                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong ISO8601TimeSpan_FractionToTicks(int maxLen, int fraction, int fracLen) 
        {
            if (fracLen == 0) 
            {
                return 0;
            }

            if (fracLen > maxLen) 
            {
                fraction /= (int)Utils.Pow10(fracLen - maxLen);
                fracLen = maxLen;
            }

            return (ulong)(fraction * Utils.Pow10(maxLen - fracLen));
        }

        static char ISO8601TimeSpan_ReadPart(string str, ref int ix, out int whole, out int fraction, out int fracLen)
        {
            var part = 0;
            while (true)
            {
                var c = str[ix];

                if (c == '.' || c == ',')
                {
                    whole = part;
                    break;
                }

                ix++;
                if (c < '0' || c > '9' || ix == str.Length)
                {
                    whole = part;
                    fraction = 0;
                    fracLen = 0;
                    return c;
                }

                part *= 10;
                part += (c - '0');
            }

            var ixOfPeriod = ix;

            ix++;   // skip the '.' or ','
            part = 0;
            while (true)
            {
                var c = str[ix];

                ix++;
                if (c < '0' || c > '9' || ix == str.Length)
                {
                    fraction = part;
                    fracLen = (ix - 1) - (ixOfPeriod + 1);
                    return c;
                }

                part *= 10;
                part += (c - '0');
            }
        }
    }
}
