using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Jil.Common;

namespace Jil.Deserialize
{
    static partial class Methods
    {
        static readonly ulong MinTicks = (ulong)(-TimeSpan.MinValue.Ticks);
        static readonly ulong MaxTicks = (ulong)TimeSpan.MaxValue.Ticks;
        
        static readonly MethodInfo ReadISO8601TimeSpan = typeof(Methods).GetMethod("_ReadISO8601TimeSpan", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static TimeSpan _ReadISO8601TimeSpan(TextReader reader, char[] str)
        {
            const ulong TicksPerDay = 864000000000;

            const ulong TicksPerWeek = TicksPerDay * 7;
            const ulong TicksPerMonth = TicksPerDay * 30;
            const ulong TicksPerYear = TicksPerDay * 365;

            // Format goes like so:
            // - (-)P(([n]Y)([n]M)([n]D))(T([n]H)([n]M)([n]S))
            // - P[n]W

            var len = ReadTimeSpanInto(reader, str);

            if (len == 0)
            {
                throw new DeserializationException("Unexpected empty string", reader, false);
            }

            var ix = 0;
            var isNegative = false;

            var c = str[ix];
            if (c == '-')
            {
                isNegative = true;
                ix++;
            }

            if (ix >= len)
            {
                throw new DeserializationException("Expected P, instead TimeSpan string ended", reader, false);
            }

            c = str[ix];
            if (c != 'P')
            {
                throw new DeserializationException("Expected P, found " + c, reader, false);
            }

            ix++;   // skip 'P'

            long year, month, week, day;
            var hasTimePart = ISO8601TimeSpan_ReadDatePart(reader, str, len, ref ix, out year, out month, out week, out day);

            if (week != -1 && (year != -1 || month != -1 || day != -1))
            {
                throw new DeserializationException("Week part of TimeSpan defined along with one or more of year, month, or day", reader, false);
            }

            if (week != -1 && hasTimePart)
            {
                throw new DeserializationException("TimeSpans with a week defined cannot also have a time defined", reader, false);
            }

            if (year == -1) year = 0;
            if (month == -1) month = 0;
            if (week == -1) week = 0;
            if (day == -1) day = 0;

            ulong timeTicks;

            if (hasTimePart)
            {
                ix++;   // skip 'T'
                ISO8601TimeSpan_ReadTimePart(reader, str, len, ref ix, out timeTicks);
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
                return TimeSpan.MaxValue;
            }

            if (ticks >= MinTicks && isNegative)
            {
                return TimeSpan.MinValue;
            }

            var ret = new TimeSpan((long)ticks);
            if (isNegative)
            {
                ret = ret.Negate();
            }

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int ReadTimeSpanInto(TextReader reader, char[] buffer)
        {
            var i = reader.Peek();
            if (i != '"') throw new DeserializationException("Expected \", found " + (char)i, reader, i == -1);
            
            reader.Read();  // skip the opening '"'

            var ix = 0;
            while (true)
            {
                if (ix >= CharBufferSize) throw new DeserializationException("ISO8601 duration too long", reader, false);

                i = reader.Read();
                if (i == -1) throw new DeserializationException("Unexpected end of stream", reader, true);
                if (i == '"')
                {
                    break;
                }

                buffer[ix] = (char)i;
                ix++;
            }

            return ix;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ISO8601TimeSpan_ReadDatePart(TextReader reader, char[] str, int strLen, ref int ix, out long year, out long month, out long week, out long day)
        {
            year = month = week = day = -1;

            bool yearSeen, monthSeen, weekSeen, daySeen;
            yearSeen = monthSeen = weekSeen = daySeen = false;

            while (ix != strLen)
            {
                if (str[ix] == 'T')
                {
                    return true;
                }

                int whole, fraction, fracLen;
                var part = ISO8601TimeSpan_ReadPart(reader, str, strLen, ref ix, out whole, out fraction, out fracLen);

                if (fracLen != 0)
                {
                    throw new DeserializationException("Fractional values are not supported in the year, month, day, or week parts of an ISO8601 TimeSpan", reader, false);
                }

                if (part == 'Y')
                {
                    if (yearSeen)
                    {
                        throw new DeserializationException("Year part of TimeSpan seen twice", reader, false);
                    }

                    if (monthSeen)
                    {
                        throw new DeserializationException("Year part of TimeSpan seen after month already parsed", reader, false);
                    }

                    if (daySeen)
                    {
                        throw new DeserializationException("Year part of TimeSpan seen after day already parsed", reader, false);
                    }

                    year = (long)whole;
                    yearSeen = true;
                    continue;
                }

                if (part == 'M')
                {
                    if (monthSeen)
                    {
                        throw new DeserializationException("Month part of TimeSpan seen twice", reader, false);
                    }

                    if (daySeen)
                    {
                        throw new DeserializationException("Month part of TimeSpan seen after day already parsed", reader, false);
                    }

                    month = (long)whole;
                    monthSeen = true;
                    continue;
                }

                if (part == 'W')
                {
                    if (weekSeen)
                    {
                        throw new DeserializationException("Week part of TimeSpan seen twice", reader, false);
                    }

                    week = (long)whole;
                    weekSeen = true;
                    continue;
                }

                if (part == 'D')
                {
                    if (daySeen)
                    {
                        throw new DeserializationException("Day part of TimeSpan seen twice", reader, false);
                    }

                    day = (long)whole;
                    daySeen = true;
                    continue;
                }

                throw new DeserializationException("Expected Y, M, W, or D but found: " + part, reader, false);
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ISO8601TimeSpan_ReadTimePart(TextReader reader, char[] str, int strLen, ref int ix, out ulong ticks)
        {
            const ulong TicksPerHour = 36000000000;
            const ulong TicksPerMinute = 600000000;
            const ulong TicksPerSecond = 10000000;

            ticks = 0;

            bool hourSeen, minutesSeen, secondsSeen;
            hourSeen = minutesSeen = secondsSeen = false;

            var fracSeen = false;

            while (ix != strLen)
            {
                if (fracSeen)
                {
                    throw new DeserializationException("Expected Time part of TimeSpan to end", reader, false);
                }

                int whole, fraction, fracLen;
                var part = ISO8601TimeSpan_ReadPart(reader, str, strLen, ref ix, out whole, out fraction, out fracLen);

                if (fracLen != 0)
                {
                    fracSeen = true;
                }

                if (part == 'H')
                {
                    if (hourSeen)
                    {
                        throw new DeserializationException("Hour part of TimeSpan seen twice", reader, false);
                    }

                    if (minutesSeen)
                    {
                        throw new DeserializationException("Hour part of TimeSpan seen after minutes already parsed", reader, false);
                    }

                    if (secondsSeen)
                    {
                        throw new DeserializationException("Hour part of TimeSpan seen after seconds already parsed", reader, false);
                    }

                    ticks += (ulong)whole * TicksPerHour + ISO8601TimeSpan_FractionToTicks(9, fraction * 36, fracLen);
                    hourSeen = true;
                    continue;
                }

                if (part == 'M')
                {
                    if (minutesSeen)
                    {
                        throw new DeserializationException("Minute part of TimeSpan seen twice", reader, false);
                    }

                    if (secondsSeen)
                    {
                        throw new DeserializationException("Minute part of TimeSpan seen after seconds already parsed", reader, false);
                    }

                    ticks += (ulong)whole * TicksPerMinute + ISO8601TimeSpan_FractionToTicks(8, fraction * 6, fracLen);
                    minutesSeen = true;
                    continue;
                }

                if (part == 'S')
                {
                    if (secondsSeen)
                    {
                        throw new DeserializationException("Seconds part of TimeSpan seen twice", reader, false);
                    }

                    ticks += (ulong)whole * TicksPerSecond + ISO8601TimeSpan_FractionToTicks(7, fraction, fracLen);
                    secondsSeen = true;
                    continue;
                }

                throw new DeserializationException("Expected H, M, or S but found: " + part, reader, false);
            }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static char ISO8601TimeSpan_ReadPart(TextReader reader, char[] str, int strLen, ref int ix, out int whole, out int fraction, out int fracLen)
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
                if (c < '0' || c > '9' || ix == strLen)
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
                if (c < '0' || c > '9' || ix == strLen)
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
