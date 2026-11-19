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
        static readonly MethodInfo ReadISO8601DateWithCharArray = typeof(Methods).GetMethod("_ReadISO8601DateWithCharArray", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DateTime _ReadISO8601DateWithCharArray(TextReader reader, ref char[] buffer)
        {
            InitDynamicBuffer(ref buffer);
            return _ReadISO8601Date(reader, buffer);
        }

        static readonly MethodInfo ReadISO8601Date = typeof(Methods).GetMethod("_ReadISO8601Date", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DateTime _ReadISO8601Date(TextReader reader, char[] buffer)
        {
            // ISO8601 / RFC3339 (the internet "profile"* of ISO8601) is a plague
            //   See: http://en.wikipedia.org/wiki/ISO_8601 &
            //        http://tools.ietf.org/html/rfc3339
            //        *is bullshit

            // Here are the possible formats for dates
            // YYYY-MM-DD
            // YYYY-MM
            // YYYY-DDD (ordinal date)
            // YYYY-Www (week date, the W is a literal)
            // YYYY-Www-D
            // YYYYMMDD
            // YYYYWww
            // YYYYWwwD
            // YYYYDDD

            // Here are the possible formats for times
            // hh
            // hh:mm
            // hhmm
            // hh:mm:ss
            // hhmmss
            // hh,fff*
            // hh:mm,fff*
            // hhmm,fff*
            // hh:mm:ss,fff*
            // hhmmss,fff*
            // hh.fff*
            // hh:mm.fff*
            // hhmm.fff*
            // hh:mm:ss.fff*
            // hhmmss.fff*
            // * arbitrarily many (technically an "agreed upon" number, I'm agreeing on 7 because that's out to a Tick)

            // Here are the possible formats for timezones
            // Z
            // +hh
            // +hh:mm
            // +hhmm
            // -hh
            // -hh:mm
            // -hhmm

            // they are concatenated to form a full instant, with T as a separator between date & time
            // i.e. <date>T<time><timezone>
            // the longest possible string:
            // 9999-12-31T01:23:45.6789012+34:56
            //
            // Maximum date size is 33 characters

            var ix = -1;
            int? tPos = null;
            int? zPlusOrMinus = null;
            while (true)
            {
                var c = reader.Peek();
                if (c == -1) throw new DeserializationException("Unexpected end of stream while parsing ISO8601 date", reader, true);

                if (c == '"') break;

                // actually consume that character
                reader.Read();

                ix++;
                if (ix == CharBufferSize) throw new DeserializationException("ISO8601 date is too long, expected " + CharBufferSize + " characters or less", reader, false);
                buffer[ix] = (char)c;

                // RFC3339 allows lowercase t and spaces as alternatives to ISO8601's T
                if (c == 'T' || c == 't' || c == ' ')
                {
                    if (tPos.HasValue) throw new DeserializationException("Unexpected second T in ISO8601 date", reader, false);
                    tPos = ix - 1;
                }

                if (tPos.HasValue)
                {
                    // RFC3339 allows lowercase z as alternatives to ISO8601's Z
                    if (c == 'Z' || c == 'z' || c == '+' || c == '-')
                    {
                        if (zPlusOrMinus.HasValue) throw new DeserializationException("Unexpected second Z, +, or - in ISO8601 date", reader, false);
                        zPlusOrMinus = ix - 1;
                    }
                }
            }
            
            var date = ParseISO8601Date(reader, buffer, 0, tPos ?? ix); // this is in *LOCAL TIME* because that's what the spec says
            if (!tPos.HasValue)
            {
                return date;
            }

            var time = ParseISO8601Time(reader, buffer, tPos.Value + 2, zPlusOrMinus ?? ix);
            if (!zPlusOrMinus.HasValue)
            {
                try
                {
                    return date + time;
                }
                catch (Exception e)
                {
                    throw new DeserializationException("ISO8601 date with time could not be represented as a DateTime", reader, e, false);
                }
            }

            bool unknownLocalOffset;
            // only +1 here because the separator is significant (oy vey)
            var timezoneOffset = ParseISO8601TimeZoneOffset(reader, buffer, zPlusOrMinus.Value + 1, ix, out unknownLocalOffset);

            try
            {
                if (unknownLocalOffset)
                {
                    return DateTime.SpecifyKind(date, DateTimeKind.Unspecified) + time;
                }

                return DateTime.SpecifyKind(date, DateTimeKind.Utc) + time - timezoneOffset;
            }
            catch (Exception e)
            {
                throw new DeserializationException("ISO8601 date with time and timezone offset could not be represented as a DateTime", reader, e, false);
            }
        }

        static readonly MethodInfo ReadISO8601DateWithOffsetWithCharArray = typeof(Methods).GetMethod("_ReadISO8601DateWithOffsetWithCharArray", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DateTimeOffset _ReadISO8601DateWithOffsetWithCharArray(TextReader reader, ref char[] buffer)
        {
            InitDynamicBuffer(ref buffer);
            return _ReadISO8601DateWithOffset(reader, buffer);
        }

        static readonly MethodInfo ReadISO8601DateWithOffset = typeof(Methods).GetMethod("_ReadISO8601DateWithOffset", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DateTimeOffset _ReadISO8601DateWithOffset(TextReader reader, char[] buffer)
        {
            // ISO8601 / RFC3339 (the internet "profile"* of ISO8601) is a plague
            //   See: http://en.wikipedia.org/wiki/ISO_8601 &
            //        http://tools.ietf.org/html/rfc3339
            //        *is bullshit

            // Here are the possible formats for dates
            // YYYY-MM-DD
            // YYYY-MM
            // YYYY-DDD (ordinal date)
            // YYYY-Www (week date, the W is a literal)
            // YYYY-Www-D
            // YYYYMMDD
            // YYYYWww
            // YYYYWwwD
            // YYYYDDD

            // Here are the possible formats for times
            // hh
            // hh:mm
            // hhmm
            // hh:mm:ss
            // hhmmss
            // hh,fff*
            // hh:mm,fff*
            // hhmm,fff*
            // hh:mm:ss,fff*
            // hhmmss,fff*
            // hh.fff*
            // hh:mm.fff*
            // hhmm.fff*
            // hh:mm:ss.fff*
            // hhmmss.fff*
            // * arbitrarily many (technically an "agreed upon" number, I'm agreeing on 7 because that's out to a Tick)

            // Here are the possible formats for timezones
            // Z
            // +hh
            // +hh:mm
            // +hhmm
            // -hh
            // -hh:mm
            // -hhmm

            // they are concatenated to form a full instant, with T as a separator between date & time
            // i.e. <date>T<time><timezone>
            // the longest possible string:
            // 9999-12-31T01:23:45.6789012+34:56
            //
            // Maximum date size is 33 characters

            var ix = -1;
            int? tPos = null;
            int? zPlusOrMinus = null;
            while (true)
            {
                var c = reader.Peek();
                if (c == -1) throw new DeserializationException("Unexpected end of stream while parsing ISO8601 date", reader, true);

                if (c == '"') break;

                // actually consume that character
                reader.Read();

                ix++;
                if (ix == CharBufferSize) throw new DeserializationException("ISO8601 date is too long, expected " + CharBufferSize + " characters or less", reader, false);
                buffer[ix] = (char)c;

                // RFC3339 allows lowercase t and spaces as alternatives to ISO8601's T
                if (c == 'T' || c == 't' || c == ' ')
                {
                    if (tPos.HasValue) throw new DeserializationException("Unexpected second T in ISO8601 date", reader, false);
                    tPos = ix - 1;
                }

                if (tPos.HasValue)
                {
                    // RFC3339 allows lowercase z as alternatives to ISO8601's Z
                    if (c == 'Z' || c == 'z' || c == '+' || c == '-')
                    {
                        if (zPlusOrMinus.HasValue) throw new DeserializationException("Unexpected second Z, +, or - in ISO8601 date", reader, false);
                        zPlusOrMinus = ix - 1;
                    }
                }
            }
            
            var date = ParseISO8601Date(reader, buffer, 0, tPos ?? ix); // this is in *LOCAL TIME* because that's what the spec says
            if (!tPos.HasValue)
            {
                return date;
            }

            var time = ParseISO8601Time(reader, buffer, tPos.Value + 2, zPlusOrMinus ?? ix);
            if (!zPlusOrMinus.HasValue)
            {
                try
                {
                    return date + time;
                }
                catch (Exception e)
                {
                    throw new DeserializationException("ISO8601 date with time could not be represented as a DateTime", reader, e, false);
                }
            }

            bool unknownLocalOffset;
            // only +1 here because the separator is significant (oy vey)
            var timezoneOffset = ParseISO8601TimeZoneOffset(reader, buffer, zPlusOrMinus.Value + 1, ix, out unknownLocalOffset);

            try
            {
                if (unknownLocalOffset)
                {
                    return DateTime.SpecifyKind(date, DateTimeKind.Unspecified) + time;
                }

                var utc = DateTime.SpecifyKind(DateTime.SpecifyKind(date, DateTimeKind.Utc) + time, DateTimeKind.Unspecified);

                return new DateTimeOffset(utc, timezoneOffset);
            }
            catch (Exception e)
            {
                throw new DeserializationException("ISO8601 date with time and timezone offset could not be represented as a DateTime", reader, e, false);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DateTime ParseISO8601Date(TextReader reader, char[] buffer, int start, int stop)
        {
            // Here are the possible formats for dates
            // YYYY-MM-DD
            // YYYY-MM
            // YYYY-DDD (ordinal date)
            // YYYY-Www (week date, the W is a literal)
            // YYYY-Www-D
            // YYYYMMDD
            // YYYYWww
            // YYYYWwwD
            // YYYYDDD

            bool? hasSeparators = null;

            var len = (stop - start) + 1;
            if (len < 4) throw new DeserializationException("ISO8601 date must begin with a 4 character year", reader, false);

            var year = 0;
            var month = 0;
            var day = 0;
            int c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
            year += (c - '0');
            year *= 10;
            start++;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
            year += (c - '0');
            year *= 10;
            start++;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
            year += (c - '0');
            year *= 10;
            start++;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
            year += (c - '0');

            if (year == 0) throw new DeserializationException("ISO8601 year 0000 cannot be converted to a DateTime", reader, false);

            // we've reached the end
            if (start == stop)
            {
                hasSeparators = null;
                // year is [1,9999] for sure, no need to handle errors
                return new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Local);
            }

            start++;
            hasSeparators = buffer[start] == '-';
            var isWeekDate = buffer[start] == 'W';
            if (hasSeparators.Value && start != stop)
            {
                isWeekDate = buffer[start + 1] == 'W';
                if (isWeekDate)
                {
                    start++;
                }
            }

            if (isWeekDate)
            {
                start++;    // skip the W

                var week = 0;

                if (hasSeparators.Value)
                {
                    // Could still be
                    // YYYY-Www         length:  8
                    // YYYY-Www-D       length: 10

                    switch (len)
                    {

                        case 8:
                            c = buffer[start];
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                            week += (c - '0');
                            week *= 10;
                            start++;
                            c = buffer[start];
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                            week += (c - '0');
                            if (week == 0 || week > 53) throw new DeserializationException("Expected week to be between 01 and 53", reader, false);

                            return ConvertWeekDateToDateTime(year, week, 1);

                        case 10:
                            c = buffer[start];
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                            week += (c - '0');
                            week *= 10;
                            start++;
                            c = buffer[start];
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                            week += (c - '0');
                            if (week == 0 || week > 53) throw new DeserializationException("Expected week to be between 01 and 53", reader, false);
                            start++;

                            c = buffer[start];
                            if (c != '-') throw new DeserializationException("Expected -", reader, false);
                            start++;

                            c = buffer[start];
                            if (c < '1' || c > '7') throw new DeserializationException("Expected day to be a digit between 1 and 7", reader, false);
                            day = (c - '0');

                            return ConvertWeekDateToDateTime(year, week, day);

                        default:
                            throw new DeserializationException("Unexpected date string length", reader, false);
                    }
                }
                else
                {
                    // Could still be
                    // YYYYWww          length: 7
                    // YYYYWwwD         length: 8
                    switch (len)
                    {

                        case 7:
                            c = buffer[start];
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                            week += (c - '0');
                            week *= 10;
                            start++;
                            c = buffer[start];
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                            week += (c - '0');
                            if (week == 0 || week > 53) throw new DeserializationException("Expected week to be between 01 and 53", reader, false);

                            return ConvertWeekDateToDateTime(year, week, 1);

                        case 8:
                            c = buffer[start];
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                            week += (c - '0');
                            week *= 10;
                            start++;
                            c = buffer[start];
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                            week += (c - '0');
                            if (week == 0 || week > 53) throw new DeserializationException("Expected week to be between 01 and 53", reader, false);
                            start++;

                            c = buffer[start];
                            if (c < '1' || c > '7') throw new DeserializationException("Expected day to be a digit between 1 and 7", reader, false);
                            day = (c - '0');

                            return ConvertWeekDateToDateTime(year, week, day);

                        default:
                            throw new DeserializationException("Unexpected date string length", reader, false);
                    }
                }
            }

            if (hasSeparators.Value)
            {
                start++;

                // Could still be:
                // YYYY-MM              length:  7
                // YYYY-DDD             length:  8
                // YYYY-MM-DD           length: 10

                switch (len)
                {
                    case 7:
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                        month += (c - '0');
                        month *= 10;
                        start++;
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                        month += (c - '0');
                        if (month == 0 || month > 12) throw new DeserializationException("Expected month to be between 01 and 12", reader, false);

                        // year is [1,9999] and month is [1,12] for sure, no need to handle errors
                        return new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local);

                    case 8:
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                        day += (c - '0');
                        day *= 10;
                        start++;
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                        day += (c - '0');
                        day *= 10;
                        start++;
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                        day += (c - '0');
                        if (day == 0 || day > 366) throw new DeserializationException("Expected ordinal day to be between 001 and 366", reader, false);

                        if (day == 366)
                        {
                            var isLeapYear = (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0));

                            if (!isLeapYear) throw new DeserializationException("Ordinal day can only be 366 in a leap year", reader, false);
                        }

                        // year is [1,9999] and day is [1,366], no need to handle errors
                        return (new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Local)).AddDays(day - 1);

                    case 10:
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                        month += (c - '0');
                        month *= 10;
                        start++;
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                        month += (c - '0');
                        if (month == 0 || month > 12) throw new DeserializationException("Expected month to be between 01 and 12", reader, false);
                        start++;

                        if (buffer[start] != '-') throw new DeserializationException("Expected -", reader, false);
                        start++;

                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                        day += (c - '0');
                        day *= 10;
                        start++;
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                        day += (c - '0');
                        if (day == 0 || day > 31) throw new DeserializationException("Expected day to be between 01 and 31", reader, false);
                        start++;

                        try
                        {
                            return (new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local));
                        }
                        catch (ArgumentOutOfRangeException e)
                        {
                            throw new DeserializationException("ISO8601 date could not be mapped to DateTime", reader, e, false);
                        }

                    default:
                        throw new DeserializationException("Unexpected date string length", reader, false);
                }
            }

            // Could still be
            // YYYYDDD          length: 7
            // YYYYMMDD         length: 8

            switch (len)
            {
                case 7:
                    c = buffer[start];
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                    day += (c - '0');
                    day *= 10;
                    start++;
                    c = buffer[start];
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                    day += (c - '0');
                    day *= 10;
                    start++;
                    c = buffer[start];
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                    day += (c - '0');
                    if (day == 0 || day > 366) throw new DeserializationException("Expected ordinal day to be between 001 and 366", reader, false);
                    start++;

                    if (day == 366)
                    {
                        var isLeapYear = (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0));

                        if (!isLeapYear) throw new DeserializationException("Ordinal day can only be 366 in a leap year", reader, false);
                    }

                    // year is [1,9999] and day is [1,366], no need to handle errors
                    return (new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Local)).AddDays(day - 1);

                case 8:
                    c = buffer[start];
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                    month += (c - '0');
                    month *= 10;
                    start++;
                    c = buffer[start];
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                    month += (c - '0');
                    if (month == 0 || month > 12) throw new DeserializationException("Expected month to be between 01 and 12", reader, false);
                    start++;

                    c = buffer[start];
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                    day += (c - '0');
                    day *= 10;
                    start++;
                    c = buffer[start];
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                    day += (c - '0');
                    if (day == 0 || day > 31) throw new DeserializationException("Expected day to be between 01 and 31", reader, false);
                    start++;

                    try
                    {
                        return (new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local));
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        throw new DeserializationException("ISO8601 date could not be mapped to DateTime", reader, e, false);
                    }

                default:
                    throw new DeserializationException("Unexpected date string length", reader, false);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static TimeSpan ParseISO8601Time(TextReader reader, char[] buffer, int start, int stop)
        {
            const long HoursToTicks   = 36000000000;
            const long MinutesToTicks = 600000000;
            const long SecondsToTicks = 10000000;

            // Here are the possible formats for times
            // hh
            // hh,fff
            // hh.fff
            //
            // hhmmss
            // hhmm
            // hhmm,fff
            // hhmm.fff
            // hhmmss.fff
            // hhmmss,fff
            // hh:mm
            // hh:mm:ss
            // hh:mm,fff
            // hh:mm:ss,fff
            // hh:mm.fff
            // hh:mm:ss.fff

            bool? hasSeparators = null;

            var len = (stop - start) + 1;
            if (len < 2) throw new DeserializationException("ISO8601 time must begin with a 2 character hour", reader, false);

            var hour = 0;
            int c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
            hour += (c - '0');
            hour *= 10;
            start++;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
            hour += (c - '0');
            if (hour > 24) throw new DeserializationException("Expected hour to be between 00 and 24", reader, false);

            // just an hour part
            if (start == stop)
            {
                return TimeSpan.FromHours(hour);
            }

            start++;
            c = buffer[start];

            // hour with a fractional part
            if (c == ',' || c == '.')
            {
                start++;
                var frac = 0;
                var fracLength = 0;
                while (start <= stop)
                {
                    c = buffer[start];
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);

                    // Max precision of TimeSpan.FromTicks
                    if (fracLength < 9) 
                    {
                        frac *= 10;
                        frac += (c - '0');
                        fracLength++;
                    }

                    start++;
                }

                if (fracLength == 0) throw new DeserializationException("Expected fractional part of ISO8601 time", reader, false);

                long hoursAsTicks = hour * HoursToTicks;
                hoursAsTicks += frac * 36 * Utils.Pow10(9 - fracLength);

                return TimeSpan.FromTicks(hoursAsTicks);
            }

            if (c == ':')
            {
                if (hasSeparators.HasValue && !hasSeparators.Value) throw new DeserializationException("Unexpected separator", reader, false);

                hasSeparators = true;
                start++;
            }
            else
            {
                if (hasSeparators.HasValue && hasSeparators.Value) throw new DeserializationException("Expected :", reader, false);

                hasSeparators = false;
            }

            if (hasSeparators.Value)
            {
                // Could still be
                // hh:mm
                // hh:mm:ss
                // hh:mm,fff
                // hh:mm:ss,fff
                // hh:mm.fff
                // hh:mm:ss.fff

                if (len < 4) throw new DeserializationException("Expected minute part of ISO8601 time", reader, false);

                var min = 0;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                min += (c - '0');
                min *= 10;
                start++;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                min += (c - '0');
                if (min > 59) throw new DeserializationException("Expected minute to be between 00 and 59", reader, false);

                // just HOUR and MINUTE part
                if (start == stop)
                {
                    return new TimeSpan(hour, min, 0);
                }

                start++;
                c = buffer[start];

                // HOUR, MINUTE, and FRACTION
                if (c == ',' || c == '.')
                {
                    start++;
                    var frac = 0;
                    var fracLength = 0;
                    while (start <= stop)
                    {
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);

                        // Max precision of TimeSpan.FromTicks
                        if (fracLength < 8) 
                        {
                            frac *= 10;
                            frac += (c - '0');
                            fracLength++;
                        }

                        start++;
                    }

                    if (fracLength == 0) throw new DeserializationException("Expected fractional part of ISO8601 time", reader, false);

                    long hoursAsTicks = hour * HoursToTicks;
                    long minsAsTicks = min * MinutesToTicks;
                    minsAsTicks += frac * 6 * Utils.Pow10(8 - fracLength);

                    return TimeSpan.FromTicks(hoursAsTicks + minsAsTicks);
                }

                if (c != ':') throw new DeserializationException("Expected :", reader, false);
                start++;

                var secs = 0;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                secs += (c - '0');
                secs *= 10;
                start++;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                secs += (c - '0');

                // HOUR, MINUTE, and SECONDS
                if (start == stop)
                {
                    return new TimeSpan(hour, min, secs);
                }

                start++;
                c = buffer[start];
                if (c == ',' || c == '.')
                {
                    start++;
                    var frac = 0;
                    var fracLength = 0;
                    while (start <= stop)
                    {
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);

                        // Max precision of TimeSpan.FromTicks
                        if (fracLength < 7) 
                        {
                            frac *= 10;
                            frac += (c - '0');
                            fracLength++;
                        }

                        start++;
                    }

                    if (fracLength == 0) throw new DeserializationException("Expected fractional part of ISO8601 time", reader, false);

                    long hoursAsTicks = hour * HoursToTicks;
                    long minsAsTicks = min * MinutesToTicks;
                    long secsAsTicks = secs * SecondsToTicks;
                    secsAsTicks += frac * Utils.Pow10(7 - fracLength);

                    return TimeSpan.FromTicks(hoursAsTicks + minsAsTicks + secsAsTicks);
                }

                throw new DeserializationException("Expected ,, or .", reader, false);
            }
            else
            {
                // Could still be
                // hhmmss
                // hhmm
                // hhmm,fff
                // hhmm.fff
                // hhmmss.fff
                // hhmmss,fff

                if (len < 4) throw new DeserializationException("Expected minute part of ISO8601 time", reader, false);

                var min = 0;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                min += (c - '0');
                min *= 10;
                start++;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                min += (c - '0');
                if (min > 59) throw new DeserializationException("Expected minute to be between 00 and 59", reader, false);

                // just HOUR and MINUTE part
                if (start == stop)
                {
                    return new TimeSpan(hour, min, 0);
                }

                start++;
                c = buffer[start];

                // HOUR, MINUTE, and FRACTION
                if (c == ',' || c == '.')
                {
                    start++;
                    var frac = 0;
                    var fracLength = 0;
                    while (start <= stop)
                    {
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);

                        // Max precision of TimeSpan.FromTicks
                        if (fracLength < 8) 
                        {
                            frac *= 10;
                            frac += (c - '0');
                            fracLength++;
                        }

                        start++;
                    }

                    if (fracLength == 0) throw new DeserializationException("Expected fractional part of ISO8601 time", reader, false);

                    long hoursAsTicks = hour * HoursToTicks;
                    long minsAsTicks = min * MinutesToTicks;
                    minsAsTicks += frac * 6 * Utils.Pow10(8 - fracLength);

                    return TimeSpan.FromTicks(hoursAsTicks + minsAsTicks);
                }

                if (c == ':') throw new DeserializationException("Unexpected separator in ISO8601 time", reader, false);

                var secs = 0;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                secs += (c - '0');
                secs *= 10;
                start++;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
                secs += (c - '0');

                // HOUR, MINUTE, and SECONDS
                if (start == stop)
                {
                    return new TimeSpan(hour, min, secs);
                }

                start++;
                c = buffer[start];
                if (c == ',' || c == '.')
                {
                    start++;
                    var frac = 0;
                    var fracLength = 0;
                    while (start <= stop)
                    {
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);

                        // Max precision of TimeSpan.FromTicks
                        if (fracLength < 7) 
                        {
                            frac *= 10;
                            frac += (c - '0');
                            fracLength++;
                        }

                        start++;
                    }

                    if (fracLength == 0) throw new DeserializationException("Expected fractional part of ISO8601 time", reader, false);

                    long hoursAsTicks = hour * HoursToTicks;
                    long minsAsTicks = min * MinutesToTicks;
                    long secsAsTicks = secs * SecondsToTicks;
                    secsAsTicks += frac * Utils.Pow10(7 - fracLength);

                    return TimeSpan.FromTicks(hoursAsTicks + minsAsTicks + secsAsTicks);
                }

                throw new DeserializationException("Expected ,, or .", reader, false);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static TimeSpan ParseISO8601TimeZoneOffset(TextReader reader, char[] buffer, int start, int stop, out bool unknownLocalOffset)
        {
            // Here are the possible formats for timezones
            // Z
            // +hh
            // +hh:mm
            // +hhmm
            // -hh
            // -hh:mm
            // -hhmm

            bool? hasSeparators = null;

            int c = buffer[start];
            // no need to validate, the caller has done that
            if (c == 'Z' || c == 'z')
            {
                unknownLocalOffset = false;
                return TimeSpan.Zero;
            }
            var isNegative = c == '-';
            start++;

            var len = (stop - start) + 1;

            if (len < 2) throw new DeserializationException("Expected hour part of ISO8601 timezone offset", reader, false);
            var hour = 0;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
            hour += (c - '0');
            hour *= 10;
            start++;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
            hour += (c - '0');
            if (hour > 24) throw new DeserializationException("Expected hour offset to be between 00 and 24", reader, false);

            // just an HOUR offset
            if (start == stop)
            {
                unknownLocalOffset = false;

                if (isNegative)
                {
                    return new TimeSpan(-hour, 0, 0);
                }
                else
                {
                    return new TimeSpan(hour, 0, 0);
                }
            }

            start++;
            c = buffer[start];
            if (c == ':')
            {
                if (hasSeparators.HasValue && !hasSeparators.Value) throw new DeserializationException("Unexpected separator in ISO8601 timezone offset", reader, false);

                hasSeparators = true;
                start++;
            }
            else
            {
                if (hasSeparators.HasValue && hasSeparators.Value) throw new DeserializationException("Expected :", reader, false);

                hasSeparators = false;
            }

            if (stop - start + 1 < 2) throw new DeserializationException("Not enough character for ISO8601 timezone offset", reader, false);

            var mins = 0;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
            mins += (c - '0');
            mins *= 10;
            start++;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, false);
            mins += (c - '0');
            if (mins > 59) throw new DeserializationException("Expected minute offset to be between 00 and 59", reader, false);

            if (isNegative)
            {
                // per Section 4.3 of of RFC3339 (http://tools.ietf.org/html/rfc3339)
                // a timezone of "-00:00" is used to indicate an "Unknown Local Offset"
                unknownLocalOffset = hour == 0 && mins == 0;

                return new TimeSpan(-hour, -mins, 0);
            }
            else
            {
                unknownLocalOffset = false;
                return new TimeSpan(hour, mins, 0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DateTime ConvertWeekDateToDateTime(int year, int week, int day)
        {
            // January 4th will always be in week 1
            var ret = new DateTime(year, 1, 4, 0, 0, 0, DateTimeKind.Utc);

            if (week != 1)
            {
                ret += TimeSpan.FromDays(7 * (week - 1));
            }

            int currentDay;
            switch (ret.DayOfWeek)
            {
                case DayOfWeek.Sunday: currentDay = 7; break;
                case DayOfWeek.Monday: currentDay = 1; break;
                case DayOfWeek.Tuesday: currentDay = 2; break;
                case DayOfWeek.Wednesday: currentDay = 3; break;
                case DayOfWeek.Thursday: currentDay = 4; break;
                case DayOfWeek.Friday: currentDay = 5; break;
                case DayOfWeek.Saturday: currentDay = 6; break;
                default: throw new Exception("Unexpected DayOfWeek");
            }

            var offset = day - currentDay;
            ret += TimeSpan.FromDays(offset);

            return ret;
        }
    }
}
