using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jil.Common;

namespace Jil.DeserializeDynamic
{
    static partial class Methods
    {
        public static bool ReadISO8601DateTime(string str, out DateTime dt)
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
            // * arbitrarily many (technically an "agreed upon" number, I'm agreeing on 7)

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
            // 9999-12-31T01:23:45.678901+01:23
            // 0123456789ABCDEFGHIJKLMNOPQRS
            //
            // Maximum date size is 33 characters

            dt = default(DateTime);
            if (str.Length > 33)
            {
                return false;
            }

            var ix = 0;
            int? tPos = null;
            int? zPlusOrMinus = null;
            for(ix = 0; ix < str.Length; ix++)
            {
                var c = str[ix];

                // RFC3339 allows lowercase t and spaces as alternatives to ISO8601's T
                if (c == 'T' || c == 't' || c == ' ')
                {
                    if (tPos.HasValue) return false;
                    tPos = ix - 1;
                }

                if (tPos.HasValue)
                {
                    // RFC3339 allows lowercase z as alternatives to ISO8601's Z
                    if (c == 'Z' || c == 'z' || c == '+' || c == '-')
                    {
                        if (zPlusOrMinus.HasValue) return false;
                        zPlusOrMinus = ix - 1;
                    }
                }
            }

            // step ix back one so it's still < str.Length
            ix--;

            bool? hasSeparators;

            DateTime date;// this is in *LOCAL TIME* because that's what the spec says
            if (!_ParseISO8601Date(str, 0, tPos ?? ix, out hasSeparators, out date))
            {
                return false;
            }

            if (!tPos.HasValue)
            {
                dt = date;
                return true;
            }

            TimeSpan time;
            if (!_ParseISO8601Time(str, tPos.Value + 2, zPlusOrMinus ?? ix, ref hasSeparators, out time))
            {
                return false;
            }

            if (!zPlusOrMinus.HasValue)
            {
                try
                {
                    dt = date + time;
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            bool unknownLocalOffset;
            // only +1 here because the separator is significant (oy vey)
            TimeSpan timezoneOffset;
            if (!_ParseISO8601TimeZoneOffset(str, zPlusOrMinus.Value + 1, ix, ref hasSeparators, out unknownLocalOffset, out timezoneOffset))
            {
                return false;
            }

            try
            {
                if (unknownLocalOffset)
                {
                    dt = DateTime.SpecifyKind(date, DateTimeKind.Unspecified) + time;
                    return true;
                }

                dt = DateTime.SpecifyKind(date, DateTimeKind.Utc) + time - timezoneOffset;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool ReadISO8601DateTimeOffset(string str, out DateTimeOffset dto)
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
            // * arbitrarily many (technically an "agreed upon" number, I'm agreeing on 7)

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
            // 9999-12-31T01:23:45.678901+01:23
            // 0123456789ABCDEFGHIJKLMNOPQRS
            //
            // Maximum date size is 33 characters

            dto = default(DateTimeOffset);
            if (str.Length > 33)
            {
                return false;
            }

            var ix = 0;
            int? tPos = null;
            int? zPlusOrMinus = null;
            for (ix = 0; ix < str.Length; ix++)
            {
                var c = str[ix];

                // RFC3339 allows lowercase t and spaces as alternatives to ISO8601's T
                if (c == 'T' || c == 't' || c == ' ')
                {
                    if (tPos.HasValue) return false;
                    tPos = ix - 1;
                }

                if (tPos.HasValue)
                {
                    // RFC3339 allows lowercase z as alternatives to ISO8601's Z
                    if (c == 'Z' || c == 'z' || c == '+' || c == '-')
                    {
                        if (zPlusOrMinus.HasValue) return false;
                        zPlusOrMinus = ix - 1;
                    }
                }
            }

            // step ix back one so it's still < str.Length
            ix--;

            bool? hasSeparators;

            DateTime date;// this is in *LOCAL TIME* because that's what the spec says
            if (!_ParseISO8601Date(str, 0, tPos ?? ix, out hasSeparators, out date))
            {
                return false;
            }

            if (!tPos.HasValue)
            {
                dto = date;
                return true;
            }

            TimeSpan time;
            if (!_ParseISO8601Time(str, tPos.Value + 2, zPlusOrMinus ?? ix, ref hasSeparators, out time))
            {
                return false;
            }

            if (!zPlusOrMinus.HasValue)
            {
                try
                {
                    dto = date + time;
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            bool unknownLocalOffset;
            // only +1 here because the separator is significant (oy vey)
            TimeSpan timezoneOffset;
            if (!_ParseISO8601TimeZoneOffset(str, zPlusOrMinus.Value + 1, ix, ref hasSeparators, out unknownLocalOffset, out timezoneOffset))
            {
                return false;
            }

            try
            {
                if (unknownLocalOffset)
                {
                    // even though the spec would say "it's unknown" we can't express that as a DateTimeOffset, so just assume UTC
                    dto = new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Unspecified) + time, TimeSpan.Zero);
                    return true;
                }

                dto = new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Unspecified) + time, timezoneOffset);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static bool _ParseISO8601TimeZoneOffset(string str, int start, int stop, ref bool? hasSeparators, out bool unknownLocalOffset, out TimeSpan ts)
        {
            ts = default(TimeSpan);
            unknownLocalOffset = default(bool);

            // Here are the possible formats for timezones
            // Z
            // +hh
            // +hh:mm
            // +hhmm
            // -hh
            // -hh:mm
            // -hhmm

            int c = str[start];
            // no need to validate, the caller has done that
            if (c == 'Z' || c == 'z')
            {
                unknownLocalOffset = false;
                ts = TimeSpan.Zero;
                return true;
            }
            var isNegative = c == '-';
            start++;

            var len = (stop - start) + 1;

            if (len < 2) return false;
            var hour = 0;
            c = str[start];
            if (c < '0' || c > '9') return false;
            hour += (c - '0');
            hour *= 10;
            start++;
            c = str[start];
            if (c < '0' || c > '9') return false;
            hour += (c - '0');
            if (hour > 24) return false;

            // just an HOUR offset
            if (start == stop)
            {
                unknownLocalOffset = false;

                if (isNegative)
                {
                    ts = new TimeSpan(-hour, 0, 0);
                    return true;
                }
                else
                {
                    ts = new TimeSpan(hour, 0, 0);
                    return true;
                }
            }

            start++;
            c = str[start];
            if (c == ':')
            {
                if (hasSeparators.HasValue && !hasSeparators.Value) return false;

                hasSeparators = true;
                start++;
            }
            else
            {
                if (hasSeparators.HasValue && hasSeparators.Value) return false;

                hasSeparators = false;
            }

            var mins = 0;
            c = str[start];
            if (c < '0' || c > '9') return false;
            mins += (c - '0');
            mins *= 10;
            start++;
            c = str[start];
            if (c < '0' || c > '9') return false;
            mins += (c - '0');
            if (mins > 59) return false;

            if (isNegative)
            {
                // per Section 4.3 of of RFC3339 (http://tools.ietf.org/html/rfc3339)
                // a timezone of "-00:00" is used to indicate an "Unknown Local Offset"
                unknownLocalOffset = hour == 0 && mins == 0;

                ts = new TimeSpan(-hour, -mins, 0);
                return true;
            }
            else
            {
                unknownLocalOffset = false;
                ts = new TimeSpan(hour, mins, 0);
                return true;
            }
        }

        static bool _ParseISO8601Date(string str, int start, int stop, out bool? hasSeparators, out DateTime dt)
        {
            dt = default(DateTime);
            hasSeparators = default(bool?);

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

            var len = (stop - start) + 1;
            if (len < 4) return false;

            var year = 0;
            var month = 0;
            var day = 0;
            int c = str[start];
            if (c < '0' || c > '9') return false;
            year += (c - '0');
            year *= 10;
            start++;
            c = str[start];
            if (c < '0' || c > '9') return false;
            year += (c - '0');
            year *= 10;
            start++;
            c = str[start];
            if (c < '0' || c > '9') return false;
            year += (c - '0');
            year *= 10;
            start++;
            c = str[start];
            if (c < '0' || c > '9') return false;
            year += (c - '0');

            if (year == 0) return false;

            // we've reached the end
            if (start == stop)
            {
                hasSeparators = null;
                // year is [1,9999] for sure, no need to handle errors
                dt = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Local);
                return true;
            }

            start++;
            hasSeparators = str[start] == '-';
            var isWeekDate = str[start] == 'W';
            if (hasSeparators.Value && start != stop)
            {
                isWeekDate = str[start + 1] == 'W';
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
                            c = str[start];
                            if (c < '0' || c > '9') return false;
                            week += (c - '0');
                            week *= 10;
                            start++;
                            c = str[start];
                            if (c < '0' || c > '9') return false;
                            week += (c - '0');
                            if (week == 0 || week > 53) return false;

                            return _ConvertWeekDateToDateTime(year, week, 1, out dt);

                        case 10:
                            c = str[start];
                            if (c < '0' || c > '9') return false;
                            week += (c - '0');
                            week *= 10;
                            start++;
                            c = str[start];
                            if (c < '0' || c > '9') return false;
                            week += (c - '0');
                            if (week == 0 || week > 53) return false;
                            start++;

                            c = str[start];
                            if (c != '-') return false;
                            start++;

                            c = str[start];
                            if (c < '1' || c > '7') return false;
                            day = (c - '0');

                            return _ConvertWeekDateToDateTime(year, week, day, out dt);

                        default:
                            return false;
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
                            c = str[start];
                            if (c < '0' || c > '9') return false;
                            week += (c - '0');
                            week *= 10;
                            start++;
                            c = str[start];
                            if (c < '0' || c > '9') return false;
                            week += (c - '0');
                            if (week == 0 || week > 53) return false;

                            return _ConvertWeekDateToDateTime(year, week, 1, out dt);

                        case 8:
                            c = str[start];
                            if (c < '0' || c > '9') return false;
                            week += (c - '0');
                            week *= 10;
                            start++;
                            c = str[start];
                            if (c < '0' || c > '9') return false;
                            week += (c - '0');
                            if (week == 0 || week > 53) return false;
                            start++;

                            c = str[start];
                            if (c < '1' || c > '7') return false;
                            day = (c - '0');

                            return _ConvertWeekDateToDateTime(year, week, day, out dt);

                        default:
                            return false;
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
                        c = str[start];
                        if (c < '0' || c > '9') return false;
                        month += (c - '0');
                        month *= 10;
                        start++;
                        c = str[start];
                        if (c < '0' || c > '9') return false;
                        month += (c - '0');
                        if (month == 0 || month > 12) return false;

                        // year is [1,9999] and month is [1,12] for sure, no need to handle errors
                        dt =new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local);
                        return true;

                    case 8:
                        c = str[start];
                        if (c < '0' || c > '9') return false;
                        day += (c - '0');
                        day *= 10;
                        start++;
                        c = str[start];
                        if (c < '0' || c > '9') return false;
                        day += (c - '0');
                        day *= 10;
                        start++;
                        c = str[start];
                        if (c < '0' || c > '9') return false;
                        day += (c - '0');
                        if (day == 0 || day > 366) return false;

                        if (day == 366)
                        {
                            var isLeapYear = (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0));

                            if (!isLeapYear) return false;
                        }

                        // year is [1,9999] and day is [1,366], no need to handle errors
                        dt = (new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Local)).AddDays(day - 1);
                        return true;

                    case 10:
                        c = str[start];
                        if (c < '0' || c > '9') return false;
                        month += (c - '0');
                        month *= 10;
                        start++;
                        c = str[start];
                        if (c < '0' || c > '9') return false;
                        month += (c - '0');
                        if (month == 0 || month > 12) return false;
                        start++;

                        if (str[start] != '-') return false;
                        start++;

                        c = str[start];
                        if (c < '0' || c > '9') return false;
                        day += (c - '0');
                        day *= 10;
                        start++;
                        c = str[start];
                        if (c < '0' || c > '9') return false;
                        day += (c - '0');
                        if (day == 0 || day > 31) return false;
                        start++;

                        try
                        {
                            dt= (new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local));
                            return true;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            return false;
                        }

                    default:
                        return false;
                }
            }

            // Could still be
            // YYYYDDD          length: 7
            // YYYYMMDD         length: 8

            switch (len)
            {
                case 7:
                    c = str[start];
                    if (c < '0' || c > '9') return false;
                    day += (c - '0');
                    day *= 10;
                    start++;
                    c = str[start];
                    if (c < '0' || c > '9') return false;
                    day += (c - '0');
                    day *= 10;
                    start++;
                    c = str[start];
                    if (c < '0' || c > '9') return false;
                    day += (c - '0');
                    if (day == 0 || day > 366) return false;
                    start++;

                    if (day == 366)
                    {
                        var isLeapYear = (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0));

                        if (!isLeapYear) return false;
                    }

                    // year is [1,9999] and day is [1,366], no need to handle errors
                    dt = (new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Local)).AddDays(day - 1);
                    return true;

                case 8:
                    c = str[start];
                    if (c < '0' || c > '9') return false;
                    month += (c - '0');
                    month *= 10;
                    start++;
                    c = str[start];
                    if (c < '0' || c > '9') return false;
                    month += (c - '0');
                    if (month == 0 || month > 12) return false;
                    start++;

                    c = str[start];
                    if (c < '0' || c > '9') return false;
                    day += (c - '0');
                    day *= 10;
                    start++;
                    c = str[start];
                    if (c < '0' || c > '9') return false;
                    day += (c - '0');
                    if (day == 0 || day > 31) return false;
                    start++;

                    try
                    {
                        dt = (new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local));
                        return true;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return false;
                    }

                default:
                    return false;
            }
        }

        static bool _ConvertWeekDateToDateTime(int year, int week, int day, out DateTime dt)
        {
            dt = default(DateTime);

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
                default: return false;
            }

            var offset = day - currentDay;
            ret += TimeSpan.FromDays(offset);

            dt = ret;
            return true;
        }

        static bool _ParseISO8601Time(string str, int start, int stop, ref bool? hasSeparators, out TimeSpan ts)
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

            ts = default(TimeSpan);

            var len = (stop - start) + 1;
            if (len < 2) return false;

            var hour = 0;
            int c = str[start];
            if (c < '0' || c > '9') return false;
            hour += (c - '0');
            hour *= 10;
            start++;
            c = str[start];
            if (c < '0' || c > '9') return false;
            hour += (c - '0');
            if (hour > 24) return false;

            // just an hour part
            if (start == stop)
            {
                ts = TimeSpan.FromHours(hour);
                return true;
            }

            start++;
            c = str[start];

            // hour with a fractional part
            if (c == ',' || c == '.')
            {
                start++;
                var frac = 0;
                var fracLength = 0;
                while (start <= stop)
                {
                    c = str[start];
                    if (c < '0' || c > '9') return false;

                    // Max precision of TimeSpan.FromTicks
                    if (fracLength < 9) 
                    {
                        frac *= 10;
                        frac += (c - '0');
                        fracLength++;
                    }

                    start++;
                }

                if (fracLength == 0) return false;

                long hoursAsTicks = hour * HoursToTicks;
                hoursAsTicks += frac * 36 * Utils.Pow10(9 - fracLength);

                ts = TimeSpan.FromTicks(hoursAsTicks);
                return true;
            }

            if (c == ':')
            {
                if (hasSeparators.HasValue && !hasSeparators.Value) return false;

                hasSeparators = true;
                start++;
            }
            else
            {
                if (hasSeparators.HasValue && hasSeparators.Value) return false;

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

                if (len < 4) return false;

                var min = 0;
                c = str[start];
                if (c < '0' || c > '9') return false;
                min += (c - '0');
                min *= 10;
                start++;
                c = str[start];
                if (c < '0' || c > '9') return false;
                min += (c - '0');
                if (min > 59) return false;

                // just HOUR and MINUTE part
                if (start == stop)
                {
                    ts = new TimeSpan(hour, min, 0);
                    return true;
                }

                start++;
                c = str[start];

                // HOUR, MINUTE, and FRACTION
                if (c == ',' || c == '.')
                {
                    start++;
                    var frac = 0;
                    var fracLength = 0;
                    while (start <= stop)
                    {
                        c = str[start];
                        if (c < '0' || c > '9') return false;

                        // Max precision of TimeSpan.FromTicks
                        if (fracLength < 8) 
                        {
                            frac *= 10;
                            frac += (c - '0');
                            fracLength++;
                        }

                        start++;
                    }

                    if (fracLength == 0) return false;

                    long hoursAsMilliseconds = hour * HoursToTicks;
                    long minsAsMilliseconds = min * MinutesToTicks;
                    minsAsMilliseconds += frac * 6 * Utils.Pow10(8 - fracLength);

                    ts = TimeSpan.FromTicks(hoursAsMilliseconds + minsAsMilliseconds);
                    return true;
                }

                if (c != ':') return false;
                start++;

                var secs = 0;
                c = str[start];
                if (c < '0' || c > '9') return false;
                secs += (c - '0');
                secs *= 10;
                start++;
                c = str[start];
                if (c < '0' || c > '9') return false;
                secs += (c - '0');

                // HOUR, MINUTE, and SECONDS
                if (start == stop)
                {
                    ts = new TimeSpan(hour, min, secs);
                    return true;
                }

                start++;
                c = str[start];
                if (c == ',' || c == '.')
                {
                    start++;
                    var frac = 0;
                    var fracLength = 0;
                    while (start <= stop)
                    {
                        c = str[start];
                        if (c < '0' || c > '9') return false;

                        // Max precision of TimeSpan.FromTicks
                        if (fracLength < 7) 
                        {
                            frac *= 10;
                            frac += (c - '0');
                            fracLength++;
                        }

                        start++;
                    }

                    if (fracLength == 0) return false;

                    long hoursAsMilliseconds = hour * HoursToTicks;
                    long minsAsMilliseconds = min * MinutesToTicks;
                    long secsAsMilliseconds = secs * SecondsToTicks;
                    secsAsMilliseconds += frac * Utils.Pow10(7 - fracLength);

                    ts = TimeSpan.FromTicks(hoursAsMilliseconds + minsAsMilliseconds + secsAsMilliseconds);
                    return true;
                }

                return false;
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

                if (len < 4) return false;

                var min = 0;
                c = str[start];
                if (c < '0' || c > '9') return false;
                min += (c - '0');
                min *= 10;
                start++;
                c = str[start];
                if (c < '0' || c > '9') return false;
                min += (c - '0');
                if (min > 59) return false;

                // just HOUR and MINUTE part
                if (start == stop)
                {
                    ts = new TimeSpan(hour, min, 0);
                    return true;
                }

                start++;
                c = str[start];

                // HOUR, MINUTE, and FRACTION
                if (c == ',' || c == '.')
                {
                    start++;
                    var frac = 0;
                    var fracLength = 0;
                    while (start <= stop)
                    {
                        c = str[start];
                        if (c < '0' || c > '9') return false;

                        // Max precision of TimeSpan.FromTicks
                        if (fracLength < 8) 
                        {
                            frac *= 10;
                            frac += (c - '0');
                            fracLength++;                        
                        }

                        start++;
                    }

                    if (fracLength == 0) return false;

                    long hoursAsMilliseconds = hour * HoursToTicks;
                    long minsAsMilliseconds = min * MinutesToTicks;
                    minsAsMilliseconds += frac * 6 * Utils.Pow10(8 - fracLength);

                    ts = TimeSpan.FromTicks(hoursAsMilliseconds + minsAsMilliseconds);
                    return true;
                }

                if (c == ':') return false;

                var secs = 0;
                c = str[start];
                if (c < '0' || c > '9') return false;
                secs += (c - '0');
                secs *= 10;
                start++;
                c = str[start];
                if (c < '0' || c > '9') return false;
                secs += (c - '0');

                // HOUR, MINUTE, and SECONDS
                if (start == stop)
                {
                    ts = new TimeSpan(hour, min, secs);
                    return true;
                }

                start++;
                c = str[start];
                if (c == ',' || c == '.')
                {
                    start++;
                    var frac = 0;
                    var fracLength = 0;
                    while (start <= stop)
                    {
                        c = str[start];
                        if (c < '0' || c > '9') return false;

                        // Max precision of TimeSpan.FromTicks
                        if (fracLength < 7) 
                        {
                            frac *= 10;
                            frac += (c - '0');
                            fracLength++;
                        }
   
                        start++;
                    }

                    if (fracLength == 0) return false;

                    long hoursAsMilliseconds = hour * HoursToTicks;
                    long minsAsMilliseconds = min * MinutesToTicks;
                    long secsAsMilliseconds = secs * SecondsToTicks;
                    secsAsMilliseconds += frac * Utils.Pow10(7 - fracLength);

                    ts = TimeSpan.FromTicks(hoursAsMilliseconds + minsAsMilliseconds + secsAsMilliseconds);
                    return true;
                }

                return false;
            }
        }

        public static bool ReadISO8601DateWithOffset(string str, out DateTimeOffset dto)
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
            // * arbitrarily many (technically an "agreed upon" number, I'm agreeing on 7)

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
            // 9999-12-31T01:23:45.678901+01:23
            // 0123456789ABCDEFGHIJKLMNOPQRS
            //
            // Maximum date size is 33 characters

            dto = default(DateTimeOffset);
            if (str.Length > 33)
            {
                return false;
            }

            var ix = 0;
            int? tPos = null;
            int? zPlusOrMinus = null;
            for (ix = 0; ix < str.Length; ix++)
            {
                var c = str[ix];

                // RFC3339 allows lowercase t and spaces as alternatives to ISO8601's T
                if (c == 'T' || c == 't' || c == ' ')
                {
                    if (tPos.HasValue) return false;
                    tPos = ix - 1;
                }

                if (tPos.HasValue)
                {
                    // RFC3339 allows lowercase z as alternatives to ISO8601's Z
                    if (c == 'Z' || c == 'z' || c == '+' || c == '-')
                    {
                        if (zPlusOrMinus.HasValue) return false;
                        zPlusOrMinus = ix - 1;
                    }
                }
            }

            // step ix back one so it's still < str.Length
            ix--;

            bool? hasSeparators;

            DateTime date;// this is in *LOCAL TIME* because that's what the spec says
            if (!_ParseISO8601Date(str, 0, tPos ?? ix, out hasSeparators, out date))
            {
                return false;
            }

            if (!tPos.HasValue)
            {
                try
                {
                    dto = new DateTimeOffset(date);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // there's no reliable way to detect an overflow...
                    //   but in the event of an overflow, try and go to the "nearest"
                    //   edge
                    var distToMin = date - DateTime.MinValue;
                    var distToMax = DateTime.MaxValue - date;

                    if (distToMin < distToMax)
                    {
                        dto = DateTimeOffset.MinValue;
                    }
                    else
                    {
                        dto = DateTimeOffset.MaxValue;
                    }
                }
                return true;
            }

            TimeSpan time;
            if (!_ParseISO8601Time(str, tPos.Value + 2, zPlusOrMinus ?? ix, ref hasSeparators, out time))
            {
                return false;
            }

            if (!zPlusOrMinus.HasValue)
            {
                try
                {
                    dto = DateTime.SpecifyKind(date + time, DateTimeKind.Unspecified);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            bool unknownLocalOffset;
            // only +1 here because the separator is significant (oy vey)
            TimeSpan timezoneOffset;
            if (!_ParseISO8601TimeZoneOffset(str, zPlusOrMinus.Value + 1, ix, ref hasSeparators, out unknownLocalOffset, out timezoneOffset))
            {
                return false;
            }

            try
            {
                if (unknownLocalOffset)
                {
                    dto = DateTime.SpecifyKind(date + time, DateTimeKind.Unspecified);
                    return true;
                }

                dto = new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Unspecified) + time, timezoneOffset);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
