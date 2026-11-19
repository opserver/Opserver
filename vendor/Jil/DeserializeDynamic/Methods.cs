using Jil.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Jil.DeserializeDynamic
{
    static partial class Methods
    {
        internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        internal static readonly DateTimeOffset UnixEpochOffset = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // .NET will expand a char[] allocation up to 32, don't bother trimming this unless you can get this down to 16
        //    you'll just be making string parsing slower for no benefit
        public const int CharBufferSize = 32;

        public static void ConsumeWhiteSpace(TextReader reader)
        {
            int c;
            while ((c = reader.Peek()) != -1)
            {
                if (!IsWhiteSpace(c)) return;

                reader.Read();
            }
        }

        static bool IsWhiteSpace(int c)
        {
            // per http://www.ietf.org/rfc/rfc4627.txt
            // insignificant whitespace in JSON is defined as 
            //  \u0020  - space
            //  \u0009  - tab
            //  \u000A  - new line
            //  \u000D  - carriage return

            return
                c == 0x20 ||
                c == 0x09 ||
                c == 0x0A ||
                c == 0x0D;
        }

        public static string ReadEncodedStringWithBuffer(TextReader reader, char[] buffer, ref StringBuilder commonSb)
        {
            commonSb = commonSb ?? new StringBuilder();

            {
                var ix = 0;

                while (ix <= CharBufferSize)
                {
                    if (ix == CharBufferSize)
                    {
                        commonSb.Append(buffer, 0, ix);
                        break;
                    }

                    var first = reader.Read();
                    if (first == -1) throw new DeserializationException("Expected any character", reader, true);

                    // we didn't have to use anything but the buffer, make a string and return it!
                    if (first == '"')
                    {
                        // avoid an allocation here
                        if (ix == 0) return "";

                        return new string(buffer, 0, ix);
                    }

                    if (first != '\\')
                    {
                        buffer[ix] = (char)first;
                        ix++;
                        continue;
                    }

                    var second = reader.Read();
                    if (second == -1) throw new DeserializationException("Expected any character", reader, true);

                    switch (second)
                    {
                        case '"': buffer[ix] = '"'; ix++; continue;
                        case '\\': buffer[ix] = '\\'; ix++; continue;
                        case '/': buffer[ix] = '/'; ix++; continue;
                        case 'b': buffer[ix] = '\b'; ix++; continue;
                        case 'f': buffer[ix] = '\f'; ix++; continue;
                        case 'n': buffer[ix] = '\n'; ix++; continue;
                        case 'r': buffer[ix] = '\r'; ix++; continue;
                        case 't': buffer[ix] = '\t'; ix++; continue;
                    }

                    if (second != 'u') throw new DeserializationException("Unrecognized escape sequence", reader, false);

                    commonSb.Append(buffer, 0, ix);

                    // now we're in an escape sequence, we expect 4 hex #s; always
                    ReadHexQuadToBuilder(reader, commonSb);
                    break;
                }
            }

            // fall through to using a StringBuilder

            while (true)
            {
                var first = reader.Read();
                if (first == -1) throw new DeserializationException("Expected any character", reader, true);

                if (first == '"')
                {
                    break;
                }

                if (first != '\\')
                {
                    commonSb.Append((char)first);
                    continue;
                }

                var second = reader.Read();
                if (second == -1) throw new DeserializationException("Expected any character", reader, true);

                switch (second)
                {
                    case '"': commonSb.Append('"'); continue;
                    case '\\': commonSb.Append('\\'); continue;
                    case '/': commonSb.Append('/'); continue;
                    case 'b': commonSb.Append('\b'); continue;
                    case 'f': commonSb.Append('\f'); continue;
                    case 'n': commonSb.Append('\n'); continue;
                    case 'r': commonSb.Append('\r'); continue;
                    case 't': commonSb.Append('\t'); continue;
                }

                if (second != 'u') throw new DeserializationException("Unrecognized escape sequence", reader, false);

                // now we're in an escape sequence, we expect 4 hex #s; always
                ReadHexQuadToBuilder(reader, commonSb);
            }

            var ret = commonSb.ToString();

            // leave this clean for the next use
            commonSb.Clear();

            return ret;
        }

        static void ReadHexQuadToBuilder(TextReader reader, StringBuilder commonSb)
        {
            var encodedChar = 0;

            //char1:
            {
                var c = reader.Read();
                if (c == -1) throw new DeserializationException("Unexpected end of reader", reader, true);

                c -= '0';
                if (c >= 0 && c <= 9)
                {
                    encodedChar += c;
                    goto char2;
                }

                c -= ('A' - '0');
                if (c >= 0 && c <= 5)
                {
                    encodedChar += 10 + c;
                    goto char2;
                }

                c -= ('f' - 'F');
                if (c >= 0 && c <= 5)
                {
                    encodedChar += 10 + c;
                    goto char2;
                }

                throw new DeserializationException("Expected hex digit, found: " + c, reader, false);
            }

            char2:
            encodedChar *= 16;
            {
                var c = reader.Read();
                if (c == -1) throw new DeserializationException("Unexpected end of reader", reader, true);

                c -= '0';
                if (c >= 0 && c <= 9)
                {
                    encodedChar += c;
                    goto char3;
                }

                c -= ('A' - '0');
                if (c >= 0 && c <= 5)
                {
                    encodedChar += 10 + c;
                    goto char3;
                }

                c -= ('f' - 'F');
                if (c >= 0 && c <= 5)
                {
                    encodedChar += 10 + c;
                    goto char3;
                }

                throw new DeserializationException("Expected hex digit, found: " + c, reader, false);
            }

            char3:
            encodedChar *= 16;
            {
                var c = reader.Read();
                if (c == -1) throw new DeserializationException("Unexpected end of reader", reader, true);

                c -= '0';
                if (c >= 0 && c <= 9)
                {
                    encodedChar += c;
                    goto char4;
                }

                c -= ('A' - '0');
                if (c >= 0 && c <= 5)
                {
                    encodedChar += 10 + c;
                    goto char4;
                }

                c -= ('f' - 'F');
                if (c >= 0 && c <= 5)
                {
                    encodedChar += 10 + c;
                    goto char4;
                }

                throw new DeserializationException("Expected hex digit, found: " + c, reader, false);
            }

            char4:
            encodedChar *= 16;
            {
                var c = reader.Read();
                if (c == -1) throw new DeserializationException("Unexpected end of reader", reader, true);

                c -= '0';
                if (c >= 0 && c <= 9)
                {
                    encodedChar += c;
                    goto finished;
                }

                c -= ('A' - '0');
                if (c >= 0 && c <= 5)
                {
                    encodedChar += 10 + c;
                    goto finished;
                }

                c -= ('f' - 'F');
                if (c >= 0 && c <= 5)
                {
                    encodedChar += 10 + c;
                    goto finished;
                }

                throw new DeserializationException("Expected hex digit, found: " + c, reader, false);
            }

            finished:
            commonSb.Append(Utils.SafeConvertFromUtf32(encodedChar));
        }

        public static double ReadDouble(char firstChar, TextReader reader, ref StringBuilder commonSb)
        {
            commonSb = commonSb ?? new StringBuilder();

            int c;

            int prev = firstChar;
            var afterFirstDigit = (firstChar >= '0' && firstChar <= '9');
            var afterE = false;
            var afterDot = false;

            commonSb.Append(firstChar);

            while ((c = reader.Peek()) != -1)
            {
                var isDigit = c >= '0' && c <= '9';
                if (!isDigit)
                {
                    var isPlus = c == '+';
                    if (isPlus)
                    {
                        if (!(prev == 'e' || prev == 'E'))
                        {
                            throw new DeserializationException("Unexpected +", reader, false);
                        }

                        goto storeChar;
                    }

                    var isMinus = c == '-';
                    if (isMinus)
                    {
                        if (!(prev == 'e' || prev == 'E'))
                        {
                            throw new DeserializationException("Unexpected -", reader, false);
                        }

                        goto storeChar;
                    }

                    var isE = c == 'e' || c == 'E';
                    if (isE)
                    {
                        if (afterE || !afterFirstDigit)
                        {
                            throw new DeserializationException("Unexpected " + c, reader, false);
                        }

                        afterE = true;
                        goto storeChar;
                    }

                    var isDot = c == '.';
                    if (isDot)
                    {
                        if (!afterFirstDigit || afterE || afterDot)
                        {
                            throw new DeserializationException("Unexpected .", reader, false);
                        }

                        afterDot = true;
                        goto storeChar;
                    }

                    break;
                }
                else
                {
                    afterFirstDigit = true;
                }

            storeChar:
                commonSb.Append((char)c);
                reader.Read();
                prev = c;
            }

            var result = double.Parse(commonSb.ToString(), CultureInfo.InvariantCulture);
            commonSb.Clear();
            return result;
        }

        public static ulong ReadULong(char firstChar, TextReader reader, out byte overflowByPowersOfTen)
        {
            // ulong.MaxValue
            // ==============
            // 18446744073709551615
            // 123456789ABCDEFGHIJK
            //
            // Length: 20 

            overflowByPowersOfTen = 0;
            ulong ret;
            int c;

            var firstDigitZero = firstChar == '0';

            // char1
            ret = (ulong)(firstChar - '0');

            for (var i = 2; i < 20; i++)
            {
                c = reader.Peek();
                if (c < '0' || c > '9') return ret;
                reader.Read();

                if(firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", reader, false);

                ret *= 10;
                ret += (uint)(c - '0');
            }

            // still more number to go?
            c = reader.Peek();
            if (c < '0' || c > '9') return ret;

            // now we need to see if we can pack this very last digit into the ulong
            //  this check is only necessary *now* because, due to base 10, you can't 
            //  overflow a ulong until the 20th digit
            var retCanBeMultiplied = (ulong.MaxValue / ret) >= 10;
            if(retCanBeMultiplied)
            {
                // remaining space will only be < 9 when we're really close
                //   to ulong.MaxValue (ie. we've read 1844674407370955161 
                //   and are now on the last digit, which could be [0, 5])
                var remainingSpace = (ulong.MaxValue - (ret * 10UL));
                var asAdd = (uint)(c - '0');
                if (asAdd <= remainingSpace)
                {
                    // we fit the criteria, advance!
                    reader.Read();
                    ret *= 10;
                    ret += asAdd;
                }
            }

            // now every character is just an extra order of magnitude for the exponent
            for(var i = 0; i < byte.MaxValue; i++)
            {
                c = reader.Peek();
                if (c < '0' || c > '9') return ret;
                reader.Read();
                overflowByPowersOfTen++;
            }

            throw new DeserializationException("Number too large to be parsed encountered", reader, false);
        }

        public static long ReadLong(char firstChar, TextReader reader)
        {
            var negate = false;
            long ret = 0;
            if (firstChar == '-')
            {
                negate = true;
            }
            else
            {
                ret = (firstChar - '0');
            }

            int c;
            while ((c = reader.Peek()) != -1)
            {
                c -= '0';
                if (c < 0 || c > 9) break;

                reader.Read();  // skip digit

                ret *= 10;
                ret += c;
            }

            if (negate) ret = -ret;

            return ret;
        }

        public static void ReadUIntOrULong(char firstChar, TextReader reader, out byte length, out uint? uintRet, out ulong? ulongRet)
        {
            // since the last character may overflow
            const int maxUIntLength = 10 - 1;
            const int maxULongLength = 20 - 1;

            length = 1;
            uintRet = (uint)(firstChar - '0');

            int c;
            while ((c = reader.Peek()) != -1)
            {
                c -= '0';
                if (c < 0 || c > 9)
                {
                    ulongRet = null;
                    return;
                }

                reader.Read();  // skip digit
                length++;

                if(length == maxUIntLength) break;

                uintRet *= 10;
                uintRet += (uint)c;
            }

            if (c == -1)
            {
                ulongRet = null;
                return;
            }

            ulongRet = uintRet;
            ulongRet *= 10;
            ulongRet += (ulong)c;
            uintRet = null;

            while ((c = reader.Peek()) != -1)
            {
                c -= '0';
                if (c < 0 || c > 9)
                {
                    return;
                }

                reader.Read();  // skip digit

                if (length == maxULongLength) break;
                
                length++;

                ulongRet *= 10;
                ulongRet += (ulong)c;
            }

            if (c == -1)
            {
                return;
            }

            try
            {
                checked
                {
                    var ulongRet2 = ulongRet;
                    ulongRet2 *= 10;
                    ulongRet2 += (ulong)c;

                    ulongRet = ulongRet2;
                    
                    length++;
                }
            }
            catch(OverflowException)
            {
                // whelp, looks like we're dropping that character
            }

            while ((c = reader.Peek()) != -1)
            {
                c -= '0';
                if (c < 0 || c > 9)
                {
                    return;
                }

                reader.Read();  // skip digit
            }
        }

        public static bool ReadMicrosoftStyleDateTime(string str, out DateTime dt)
        {
            // Format: /Date(#####+####)/

            dt = DateTime.MinValue;

            if (str.Length < 9) return false;

            if (str[0] != '/' || str[1] != 'D' || str[2] != 'a' || str[3] != 't' || str[4] != 'e' || str[5] != '(')
            {
                return false;
            }

            bool negative = false;

            var ix = 6;
            var c = str[ix];
            if (c == '-')
            {
                negative = true;
                ix++;
                c = str[ix];
            }

            if (c < '0' || c > '9') return false;

            long l = 0;
            for (var i = 0; ix < str.Length - 1 && i < 20; i++)
            {
                l *= 10;
                l += (c - '0');
                ix++;
                c = str[ix];
                if (c < '0' || c > '9') break;
            }

            if (negative) l = -l;

            if (ix == str.Length) return false;
            var hasTimeZone = str[ix] == '+' || str[ix] == '-';
            if (hasTimeZone)
            {
                ix++;
                for (var i = 0; i < 4; i++)
                {
                    if (ix == str.Length) return false;
                    c = str[ix];
                    if (c < '0' || c > '9') return false;
                    ix++;
                }
            }

            var remainingLen = str.Length - ix;
            if (remainingLen != 2) return false;
            if (str[ix] != ')') return false;
            ix++;
            if (str[ix] != '/') return false;

            dt = UnixEpoch + TimeSpan.FromMilliseconds(l);
            return true;
        }

        public static bool ReadMicrosoftStyleDateTimeOffset(string str, out DateTimeOffset dto)
        {
            const long EpochTicks = 621355968000000000L;
            const long MillisecondsToTicks = 10000L;

            // Format: /Date(#####+####)/

            dto = DateTimeOffset.MinValue;

            if (str.Length < 9) return false;

            if (str[0] != '/' || str[1] != 'D' || str[2] != 'a' || str[3] != 't' || str[4] != 'e' || str[5] != '(')
            {
                return false;
            }

            bool negative = false;

            var ix = 6;
            var c = str[ix];
            if (c == '-')
            {
                negative = true;
                ix++;
                c = str[ix];
            }

            if (c < '0' || c > '9') return false;

            long l = 0;
            for (var i = 0; ix < str.Length - 1 && i < 20; i++)
            {
                l *= 10;
                l += (c - '0');
                ix++;
                c = str[ix];
                if (c < '0' || c > '9') break;
            }

            if (negative) l = -l;

            if (ix == str.Length) return false;
            var hasTimeZone = str[ix] == '+' || str[ix] == '-';

            var tsHour = 0;
            var tsMin = 0;
            var tsIsNegative = false;
            if (hasTimeZone)
            {
                tsIsNegative = str[ix] == '-';

                ix++;

                if (ix == str.Length) return false;
                c = str[ix];
                ix++;
                if (c < '0' || c > '9') return false;
                tsHour = c - '0';
                
                
                if (ix == str.Length) return false;
                c = str[ix];
                ix++;
                if (c < '0' || c > '9') return false;
                tsHour *= 10;
                tsHour += (c - '0');

                if (ix == str.Length) return false;
                c = str[ix];
                ix++;
                if (c < '0' || c > '9') return false;
                tsMin= c - '0';

                
                if (ix == str.Length) return false;
                c = str[ix];
                ix++;
                if (c < '0' || c > '9') return false;
                tsMin *= 10;
                tsMin += (c - '0');
            }

            var remainingLen = str.Length - ix;
            if (remainingLen != 2) return false;
            if (str[ix] != ')') return false;
            ix++;
            if (str[ix] != '/') return false;

            var utcTicks = EpochTicks + l * MillisecondsToTicks;
            if(!hasTimeZone)
            {
                dto = new DateTimeOffset(utcTicks, TimeSpan.Zero);
                return true;
            }

            var offset = new TimeSpan(tsHour, tsMin, 0);
            if (tsIsNegative)
            {
                offset = offset.Negate();
            }

            utcTicks += offset.Ticks;

            dto = new DateTimeOffset(utcTicks, offset);
            return true;
        }

        static readonly double[] DivideFractionBy =
            new double[]
            { 
                10, 
                100, 
                1000, 
                10000, 
                100000,
                1000000,
                10000000,
                100000000
            };

        public static bool ReadMicrosoftStyleTimeSpan(string str, out TimeSpan ts)
        {
            if (str.Length == 0)
            {
                ts = default(TimeSpan);
                return false;
            }

            int days, hours, minutes, seconds, fraction;
            days = hours = minutes = seconds = fraction = 0;

            bool isNegative, pastDays, pastHours, pastMinutes, pastSeconds;
            isNegative = pastDays = pastHours = pastMinutes = pastSeconds = false;

            var ixOfLastPeriod = -1;
            var part = 0;

            int i;

            if (str[0] == '-')
            {
                isNegative = true;
                i = 1;
            }
            else
            {
                i = 0;
            }

            for (; i < str.Length; i++)
            {
                var c = str[i];
                if (c == '.')
                {
                    ixOfLastPeriod = i;

                    if (!pastDays)
                    {
                        days = part;
                        part = 0;
                        pastDays = true;
                        continue;
                    }

                    if (!pastSeconds)
                    {
                        seconds = part;
                        part = 0;
                        pastSeconds = true;
                        continue;
                    }

                    ts = default(TimeSpan);
                    return false;
                }

                if (c == ':')
                {
                    if (!pastHours)
                    {
                        hours = part;
                        part = 0;
                        pastHours = true;
                        continue;
                    }

                    if (!pastMinutes)
                    {
                        minutes = part;
                        part = 0;
                        pastMinutes = true;
                        continue;
                    }

                    ts = default(TimeSpan);
                    return false;
                }

                if (c < '0' || c > '9')
                {
                    ts = default(TimeSpan);
                    return false;
                }

                part *= 10;
                part += (c - '0');
            }

            if (!pastSeconds)
            {
                seconds = part;
                pastSeconds = true;
            }
            else
            {
                fraction = part;
            }

            if (!pastHours || !pastMinutes || !pastSeconds)
            {
                ts = default(TimeSpan);
                return false;
            }

            var msInt = 0;
            if (fraction != 0)
            {
                var sizeOfFraction = str.Length - (ixOfLastPeriod + 1);

                if (sizeOfFraction > 7)
                {
                    ts = default(TimeSpan);
                    return false;
                }

                var fracOfSecond = part / DivideFractionBy[sizeOfFraction - 1];
                var ms = fracOfSecond * 1000.0;
                msInt = (int)ms;
            }

            ts = new TimeSpan(days, hours, minutes, seconds, msInt);
            if (isNegative)
            {
                ts = ts.Negate();
            }

            return true;
        }

        public static bool ReadRFC1123DateTime(string str, out DateTime res)
        {
            // ddd, dd MMM yyyy HH:mm:ss GMT'"

            var ix = 0;
            DayOfWeek dayOfWeek;
            if (!ReadRFC1123DayOfWeek(str, ref ix, out dayOfWeek))
            {
                res = default(DateTime);
                return false;
            }

            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }

            var c = str[ix];
            if (c != ',')
            {
                res = default(DateTime);
                return false;
            }

            ix++;
            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }

            c = str[ix];
            if (c != ' ')
            {
                res = default(DateTime);
                return false;
            }

            ix++;
            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }

            var day = 0;
            c = str[ix];
            ix++;
            if (c < '0' || c > '9')
            {
                res = default(DateTime);
                return false;
            }
            day += (c - '0');

            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c < '0' || c > '9')
            {
                res = default(DateTime);
                return false;
            }
            day *= 10;
            day += (c - '0');

            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c != ' ')
            {
                res = default(DateTime);
                return false;
            }

            int month;
            if(!ReadRFC1123Month(str, ref ix, out month))
            {
                res = default(DateTime);
                return false;
            }

            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c != ' ')
            {
                res = default(DateTime);
                return false;
            }

            var year = 0;
            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c < '0' || c > '9')
            {
                res = default(DateTime);
                return false;
            }
            year += (c - '0');
            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c < '0' || c > '9')
            {
                res = default(DateTime);
                return false;
            }
            year *= 10;
            year += (c - '0');
            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c < '0' || c > '9')
            {
                res = default(DateTime);
                return false;
            }
            year *= 10;
            year += (c - '0');
            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c < '0' || c > '9')
            {
                res = default(DateTime);
                return false;
            }
            year *= 10;
            year += (c - '0');

            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c != ' ')
            {
                res = default(DateTime);
                return false;
            }

            var hour = 0;
            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c < '0' || c > '9')
            {
                res = default(DateTime);
                return false;
            }
            hour += (c - '0');

            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c < '0' || c > '9')
            {
                res = default(DateTime);
                return false;
            }
            hour *= 10;
            hour += (c - '0');

            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c != ':')
            {
                res = default(DateTime);
                return false;
            }

            var min = 0;
            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c < '0' || c > '9')
            {
                res = default(DateTime);
                return false;
            }
            min += (c - '0');
            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c < '0' || c > '9')
            {
                res = default(DateTime);
                return false;
            }
            min *= 10;
            min += (c - '0');

            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c != ':')
            {
                res = default(DateTime);
                return false;
            }

            var sec = 0;
            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c < '0' || c > '9')
            {
                res = default(DateTime);
                return false;
            }
            sec += (c - '0');
            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c < '0' || c > '9')
            {
                res = default(DateTime);
                return false;
            }
            sec *= 10;
            sec += (c - '0');

            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c != ' ')
            {
                res = default(DateTime);
                return false;
            }

            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c != 'G')
            {
                res = default(DateTime);
                return false;
            }
            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c != 'M')
            {
                res = default(DateTime);
                return false;
            }
            if (ix >= str.Length)
            {
                res = default(DateTime);
                return false;
            }
            c = str[ix];
            ix++;
            if (c != 'T')
            {
                res = default(DateTime);
                return false;
            }

            var ret = new DateTime(year, month, day, hour, min, sec, DateTimeKind.Utc);

            if (ret.DayOfWeek != (DayOfWeek)dayOfWeek)
            {
                res = default(DateTime);
                return false;
            }

            res = ret;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ReadRFC1123Month(string str, ref int ix, out int res)
        {
            if (ix >= str.Length)
            {
                res = 0;
                return false;
            }
            var c = str[ix];
            ix++;

            // Jan | Jun | Jul
            if (c == 'J')
            {
                if (ix >= str.Length)
                {
                    res = 0;
                    return false;
                }
                c = str[ix];
                ix++;
                if (c == 'a')
                {
                    if (ix >= str.Length)
                    {
                        res = 0;
                        return false;
                    }
                    c = str[ix];
                    ix++;
                    if (c != 'n')
                    {
                        res = 0;
                        return false;
                    }

                    res = 1;
                    return true;
                }

                if (c != 'u')
                {
                    res = 0;
                    return false;
                }

                if (ix >= str.Length)
                {
                    res = 0;
                    return false;
                }
                c = str[ix];
                ix++;
                
                if (c == 'n')
                {
                    res = 6;
                    return true;
                }

                if (c == 'l')
                {
                    res = 7;
                    return true;
                }

                res = 0;
                return false;
            }

            // Feb
            if (c == 'F')
            {
                if (ix >= str.Length)
                {
                    res = 0;
                    return false;
                }
                c = str[ix];
                ix++;
                if (c != 'e')
                {
                    res = 0;
                    return false;
                }
                if (ix >= str.Length)
                {
                    res = 0;
                    return false;
                }
                c = str[ix];
                ix++;
                if (c != 'b')
                {
                    res = 0;
                    return false;
                }

                res = 2;
                return true;
            }

            // Mar | May
            if (c == 'M')
            {
                if (ix >= str.Length)
                {
                    res = 0;
                    return false;
                }
                c = str[ix];
                ix++;
                if (c != 'a')
                {
                    res = 0;
                    return false;
                }

                if (ix >= str.Length)
                {
                    res = 0;
                    return false;
                }
                c = str[ix];
                ix++;
                
                if (c == 'r')
                {
                    res = 3;
                    return true;
                }

                if (c == 'y')
                {
                    res = 5;
                    return true;
                }

                res = 0;
                return false;
            }

            // Apr | Aug
            if (c == 'A')
            {
                if (ix >= str.Length)
                {
                    res = 0;
                    return false;
                }
                c = str[ix];
                ix++;
                if (c == 'p')
                {
                    if (ix >= str.Length)
                    {
                        res = 0;
                        return false;
                    }
                    c = str[ix];
                    ix++;
                    if (c != 'r')
                    {
                        res = 0;
                        return false;
                    }

                    res = 4;
                    return true;
                }

                if (c == 'u')
                {
                    if (ix >= str.Length)
                    {
                        res = 0;
                        return false;
                    }
                    c = str[ix];
                    ix++;
                    if (c != 'g')
                    {
                        res = 0;
                        return false;
                    }

                    res = 8;
                    return true;
                }

                res = 0;
                return false;
            }

            // Sep
            if (c == 'S')
            {
                if (ix >= str.Length)
                {
                    res = 0;
                    return false;
                }
                c = str[ix];
                ix++;
                if (c != 'e')
                {
                    res = 0;
                    return false;
                }

                if (ix >= str.Length)
                {
                    res = 0;
                    return false;
                }
                c = str[ix];
                ix++;
                if (c != 'p')
                {
                    res = 0;
                    return false;
                }

                res = 9;
                return true;
            }

            // Oct
            if (c == 'O')
            {
                if (ix >= str.Length)
                {
                    res = 0;
                    return false;
                }
                c = str[ix];
                ix++;

                if (c != 'c')
                {
                    res = 0;
                    return false;
                }

                if (ix >= str.Length)
                {
                    res = 0;
                    return false;
                }
                c = str[ix];
                ix++;
                if (c != 't')
                {
                    res = 0;
                    return false;
                }

                res = 10;
                return true;
            }

            // Nov
            if (c == 'N')
            {
                if (ix >= str.Length)
                {
                    res = 0;
                    return false;
                }
                c = str[ix];
                ix++;

                if (c != 'o')
                {
                    res = 0;
                    return false;
                }

                if (ix >= str.Length)
                {
                    res = 0;
                    return false;
                }
                c = str[ix];
                ix++;

                if (c != 'v')
                {
                    res = 0;
                    return false;
                }

                res = 11;
                return true;
            }

            // Dec
            if (c == 'D')
            {
                if (ix >= str.Length)
                {
                    res = 0;
                    return false;
                }
                c = str[ix];
                ix++;

                if (c != 'e')
                {
                    res = 0;
                    return false;
                }

                if (ix >= str.Length)
                {
                    res = 0;
                    return false;
                }
                c = str[ix];
                ix++;

                if (c != 'c')
                {
                    res = 0;
                    return false;
                }

                res = 12;
                return true;
            }

            res = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ReadRFC1123DayOfWeek(string str, ref int ix, out DayOfWeek res)
        {
            if(ix >= str.Length)
            {
                res= default(DayOfWeek);
                return false;
            }

            var c = str[ix];
            ix++;

            // Mon
            if (c == 'M')
            {
                if (ix >= str.Length)
                {
                    res = default(DayOfWeek);
                    return false;
                }
                c = str[ix];
                ix++;
                if (c != 'o')
                {
                    res = default(DayOfWeek);
                    return false;
                }

                if (ix >= str.Length)
                {
                    res = default(DayOfWeek);
                    return false;
                }
                c = str[ix];
                ix++;
                if (c != 'n')
                {
                    res = default(DayOfWeek);
                    return false;
                }

                res=   DayOfWeek.Monday;
                return true;
            }

            // Tue | Thu
            if (c == 'T')
            {
                if (ix >= str.Length)
                {
                    res = default(DayOfWeek);
                    return false;
                }
                c = str[ix];
                ix++;

                if (c == 'u')
                {
                    if (ix >= str.Length)
                    {
                        res = default(DayOfWeek);
                        return false;
                    }
                    c = str[ix];
                    ix++;
                    if (c != 'e')
                    {
                        res = default(DayOfWeek);
                        return false;
                    }

                    res =  DayOfWeek.Tuesday;
                    return true;
                }

                if (c == 'h')
                {
                    if (ix >= str.Length)
                    {
                        res = default(DayOfWeek);
                        return false;
                    }
                    c = str[ix];
                    ix++;
                    if (c != 'u')
                    {
                        res = default(DayOfWeek);
                        return false;
                    }

                    res= DayOfWeek.Thursday;
                    return true;
                }

                res = default(DayOfWeek);
                return false;
            }

            // Wed
            if (c == 'W')
            {
                if (ix >= str.Length)
                {
                    res = default(DayOfWeek);
                    return false;
                }
                c = str[ix];
                ix++;
                if (c != 'e')
                {
                    res = default(DayOfWeek);
                    return false;
                }

                if (ix >= str.Length)
                {
                    res = default(DayOfWeek);
                    return false;
                }
                c = str[ix];
                ix++;
                if (c != 'd')
                {
                    res = default(DayOfWeek);
                    return false;
                }

                res = DayOfWeek.Wednesday;
                return true;
            }

            // Fri
            if (c == 'F')
            {
                if (ix >= str.Length)
                {
                    res = default(DayOfWeek);
                    return false;
                }
                c = str[ix];
                ix++;
                if (c != 'r')
                {
                    res = default(DayOfWeek);
                    return false;
                }

                if (ix >= str.Length)
                {
                    res = default(DayOfWeek);
                    return false;
                }
                c = str[ix];
                ix++;
                if (c != 'i')
                {
                    res = default(DayOfWeek);
                    return false;
                }

                res = DayOfWeek.Friday;
                return true;
            }

            // Sat | Sun
            if (c == 'S')
            {
                if (ix >= str.Length)
                {
                    res = default(DayOfWeek);
                    return false;
                }
                c = str[ix];
                ix++;

                if (c == 'a')
                {
                    if (ix >= str.Length)
                    {
                        res = default(DayOfWeek);
                        return false;
                    }
                    c = str[ix];
                    ix++;
                    if (c != 't')
                    {
                        res = default(DayOfWeek);
                        return false;
                    }

                    res = DayOfWeek.Saturday;
                    return true;
                }

                if (c == 'u')
                {
                    if (ix >= str.Length)
                    {
                        res = default(DayOfWeek);
                        return false;
                    }
                    c = str[ix];
                    ix++;
                    if (c != 'n')
                    {
                        res = default(DayOfWeek);
                        return false;
                    }

                    res = DayOfWeek.Sunday;
                    return true;
                }

                res = default(DayOfWeek);
                return false;
            }

            res = default(DayOfWeek);
            return false;
        }
    }
}
