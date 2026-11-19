using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Deserialize
{
    // Let's talk about parsing numbers.
    //
    // Most of this file handles parsing integer numbers, it cheats for speed in a few ways.
    //   - unroll the loops because the type of a number gives us it's maximum size
    //   - omit sign checks when dealing with unsigned types
    //   - build up w/o allocation using * and +, since we know everything's base-10 w/o separators
    //   - only do overflow checking after reading the *last* digit
    //      * base 10 again, you can't overflow 256 with 2 or fewer characters
    //      * we specify the math we easily can in Int32, which is probably the fastest silicon on the chip anyway
    //   - oversize accumulator (int for byte, long for int, etc.) so we don't need to special case SignedType.MinValue
    //      * *except* for long, where we accumulate into a ulong and do the special checked; because there's no larger type
    static partial class Methods
    {
        // Note: This same method, but then for ThunkReader, is in Methods.ThunkReader.cs; it's called "_DiscardMicrosoftTimeZoneOffsetThunkReader".
        static readonly MethodInfo DiscardMicrosoftTimeZoneOffset = typeof(Methods).GetMethod("_DiscardMicrosoftTimeZoneOffset", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _DiscardMicrosoftTimeZoneOffset(TextReader reader)
        {
            // this is a special case when reading timezone information for MicrosoftStyle DateTimes
            //   so far as I can tell this is pointless data, the millisecond offset is still UTC relative
            //   so just use that... should validate that this correct though
            // max +9999
            // min -9999
            // digits: 4

            var c = reader.Peek();
            if (c != '-' && c != '+') return;

            reader.Read();

            var temp = 0;

            // first digit hour
            c = reader.Read();
            if (c == -1) throw new DeserializationException("Unexpected end of reader", reader, true);
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", reader, false);
            temp += c;

            // second digit hour
            temp *= 10;
            c = reader.Read();
            if (c == -1) throw new DeserializationException("Unexpected end of reader", reader, true);
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", reader, false);
            temp += c;

            if (temp > 23) throw new DeserializationException("Expected hour portion of timezone offset between 0 and 24", reader, false);

            temp = 0;
            // first digit minute
            temp *= 10;
            c = reader.Read();
            if (c == -1) throw new DeserializationException("Unexpected end of reader", reader, true);
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", reader, false);
            temp += c;

            // second digit minute
            temp *= 10;
            c = reader.Read();
            if (c == -1) throw new DeserializationException("Unexpected end of reader", reader, true);
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", reader, false);
            temp += c;

            if (temp > 59) throw new DeserializationException("Expected minute portion of timezone offset between 0 and 59", reader, false);
        }

        static readonly MethodInfo ReadUInt8 = typeof(Methods).GetMethod("_ReadUInt8", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte _ReadUInt8(TextReader reader)
        {
            // max:  512
            // min:    0
            // digits: 3

            uint ret = 0;

            // first digit *must* exist, we can't overread
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Unexpected end of reader", reader, true);
            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", reader, false);
            ret += (uint)c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (byte)ret;
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", reader, false);
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #3
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (byte)ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            AssertNotFollowedByDigit(reader);

            checked
            {
                return (byte)ret;
            }
        }

        static readonly MethodInfo ReadInt8 = typeof(Methods).GetMethod("_ReadInt8", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static sbyte _ReadInt8(TextReader reader)
        {
            // max:  127
            // min: -127
            // digits: 3

            int ret = 0;
            var negative = false;

            // digit #1
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected digit or '-'", reader, true);

            if (c == '-')
            {
                negative = true;
                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected digit", reader, true);
            }

            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", reader, false);
            ret += c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (sbyte)(ret * (negative ? -1 : 1));
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", reader, false);
            reader.Read();
            ret *= 10;
            ret += c;

            // digit #3
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (sbyte)(ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += c;

            AssertNotFollowedByDigit(reader);

            checked
            {
                return (sbyte)(ret * (negative ? -1 : 1));
            }
        }

        static readonly MethodInfo ReadInt16 = typeof(Methods).GetMethod("_ReadInt16", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static short _ReadInt16(TextReader reader)
        {
            // max:  32767
            // min: -32768
            // digits:   5

            int ret = 0;
            var negative = false;

            // digit #1
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected digit or '-'", reader, true);

            if (c == '-')
            {
                negative = true;
                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected digit", reader, true);
            }

            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", reader, false);
            ret += c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (short)(ret * (negative ? -1 : 1));
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", reader, false);
            reader.Read();
            ret *= 10;
            ret += c;

            // digit #3
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (sbyte)(ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += c;

            // digit #4
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (short)(ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += c;

            // digit #5
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (short)(ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += c;

            AssertNotFollowedByDigit(reader);

            checked
            {
                return (short)(ret * (negative ? -1 : 1));
            }
        }

        static readonly MethodInfo ReadUInt16 = typeof(Methods).GetMethod("_ReadUInt16", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ushort _ReadUInt16(TextReader reader)
        {
            // max: 65535
            // min:     0
            // digits:  5

            uint ret = 0;

            // digit #1
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected digit", reader, true);

            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", reader, false);
            ret += (uint)c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (ushort)ret;
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", reader, false);
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #3
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (ushort)ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #4
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (ushort)ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #5
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (ushort)ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            AssertNotFollowedByDigit(reader);

            checked
            {
                return (ushort)ret;
            }
        }

        static readonly MethodInfo ReadInt32 = typeof(Methods).GetMethod("_ReadInt32", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int _ReadInt32(TextReader reader)
        {
            // max:  2147483647
            // min: -2147483648
            // digits:       10

            long ret = 0;
            var negative = false;

            // digit #1
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected digit or '-'", reader, true);

            if (c == '-')
            {
                negative = true;
                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected digit", reader, true);
            }

            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", reader, false);
            ret += c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (int)(ret * (negative ? -1 : 1));
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", reader, false);
            reader.Read();
            ret *= 10;
            ret += c;

            // digit #3
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (int)(ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += c;

            // digit #4
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (int)(ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += c;

            // digit #5
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (int)(ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += c;

            // digit #6
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (int)(ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += c;

            // digit #7
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (int)(ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += c;

            // digit #8
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (int)(ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += c;

            // digit #9
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (int)(ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += c;

            // digit #10
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (int)(ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += c;

            AssertNotFollowedByDigit(reader);

            checked
            {
                return (int)(ret * (negative ? -1 : 1));
            }
        }

        static readonly MethodInfo ReadUInt32 = typeof(Methods).GetMethod("_ReadUInt32", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint _ReadUInt32(TextReader reader)
        {
            // max: 4294967295
            // min:          0
            // digits:      10

            ulong ret = 0;

            // digit #1
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected digit", reader, true);

            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", reader, false);
            ret += (uint)c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (uint)ret;
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", reader, false);
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #3
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (uint)ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #4
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (uint)ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #5
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (uint)ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #6
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (uint)ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #7
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (uint)ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #8
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (uint)ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #9
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (uint)ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #10
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (uint)ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            AssertNotFollowedByDigit(reader);

            checked
            {
                return (uint)ret;
            }
        }

        static readonly MethodInfo ReadInt64 = typeof(Methods).GetMethod("_ReadInt64", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long _ReadInt64(TextReader reader)
        {
            // max:  9223372036854775807
            // min: -9223372036854775808
            // digits:                19

            ulong ret = 0;
            var negative = false;

            // digit #1
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected digit or '-'", reader, true);

            if (c == '-')
            {
                negative = true;
                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected digit", reader, true);
            }

            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", reader, false);
            ret += (uint)c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", reader, false);
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #3
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #4
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #5
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #6
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #7
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #8
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #9
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #10
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #11
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #12
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #13
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #14
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #15
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #16
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #17
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #18
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #19
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            AssertNotFollowedByDigit(reader);

            // one edge case, the minimum long value has to be checked for
            //   because otherwise we will overflow long and throw
            if (negative && ret == 9223372036854775808UL)
            {
                return long.MinValue;
            }

            checked
            {
                return ((long)ret * (negative ? -1 : 1));
            }
        }

        static readonly MethodInfo ReadUInt64 = typeof(Methods).GetMethod("_ReadUInt64", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong _ReadUInt64(TextReader reader)
        {
            // max: 18446744073709551615
            // min:                    0
            // digits:                20

            ulong ret = 0;

            // digit #1
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected digit", reader, true);

            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", reader, false);
            ret += (uint)c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", reader, false);
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #3
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #4
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #5
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #6
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #7
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #8
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #9
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #10
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #11
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #12
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #13
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #14
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #15
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #16
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #17
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #18
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #19
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();
            ret *= 10;
            ret += (uint)c;

            // digit #20
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            reader.Read();

            AssertNotFollowedByDigit(reader);

            // ulong special case, we can overflow in the last **addition* instead of the last cast
            checked
            {
                ret *= 10;
                ret += (uint)c;

                return ret;
            }
        }

        static readonly MethodInfo ReadDouble = typeof(Methods).GetMethod("_ReadDouble", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double _ReadDouble(TextReader reader, ref StringBuilder commonSb)
        {
            commonSb = commonSb ?? new StringBuilder();

            int c;

            var prev = -1;
            var afterFirstDigit = false;
            var afterE = false;
            var afterDot = false;

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
                        if (prev != -1 && !(prev == 'e' || prev == 'E'))
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

            var asStr = commonSb.ToString();
            var strLen = asStr.Length;

            if (strLen == 0) throw new DeserializationException("Expected a double value", reader, false);

            if (asStr[strLen - 1] == '.') throw new DeserializationException("Number cannot end with .", reader, false);
            if (strLen >= 2 && asStr[0] == '0')
            {
                var secondChar = asStr[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }
            if (strLen >= 3 && asStr[0] == '-' && asStr[1] == '0')
            {
                var secondChar = asStr[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }

            var result = double.Parse(asStr, CultureInfo.InvariantCulture);
            commonSb.Clear();
            return result;
        }

        static readonly MethodInfo ReadDoubleCharArray = typeof(Methods).GetMethod("_ReadDoubleCharArray", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double _ReadDoubleCharArray(TextReader reader, ref char[] buffer)
        {
            var idx = 0;
            InitDynamicBuffer(ref buffer);

            int c;

            var prev = -1;
            var afterFirstDigit = false;
            var afterE = false;
            var afterDot = false;

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
                        if (prev != -1 && !(prev == 'e' || prev == 'E'))
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
                buffer[idx++] = (char)c;
                if (idx >= buffer.Length)
                {
                    GrowDynamicBuffer(ref buffer);
                }
                reader.Read();
                prev = c;
            }

            if (idx == 0) throw new DeserializationException("Expected a double value", reader, false);

            if (idx == 0) throw new DeserializationException("Expected a double value", reader, false);

            if (buffer[idx - 1] == '.') throw new DeserializationException("Number cannot end with .", reader, false);
            if (idx >= 2 && buffer[0] == '0')
            {
                var secondChar = buffer[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }
            if (idx >= 3 && buffer[0] == '-' && buffer[1] == '0')
            {
                var secondChar = buffer[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }


            var result = double.Parse(new string(buffer, 0, idx), CultureInfo.InvariantCulture);
            return result;
        }

        private static readonly double[] DoubleDividers = new[] {
            1.0,
            10.0,
            100.0,
            1000.0,
            10000.0,
            100000.0,
            1000000.0,
            10000000.0,
            100000000.0,
            1000000000.0,
            10000000000.0,
        };

        static readonly MethodInfo ReadDoubleFast = typeof(Methods).GetMethod("_ReadDoubleFast", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double _ReadDoubleFast(TextReader reader, ref char[] buffer)
        {
            var idx = 0;
            InitDynamicBuffer(ref buffer);

            int c;

            var prev = -1;

            var firstDigitIdx = -1;
            var firstValidCharIdx = -1;
            var decimalPointIdx = -1;
            var eIdx = -1;

            while ((c = reader.Peek()) != -1)
            {
                if (c >= '0' && c <= '9')
                {
                    if (firstDigitIdx < 0)
                    {
                        firstDigitIdx = idx;
                        if (firstValidCharIdx < 0)
                        {
                            firstValidCharIdx = idx;
                        }
                    }
                }
                else
                {
                    if (c == '+')
                    {
                        if (!(prev == 'e' || prev == 'E'))
                        {
                            throw new DeserializationException("Unexpected +", reader, false);
                        }
                        firstValidCharIdx = idx;
                    }
                    else
                    {
                        if (c == '-')
                        {
                            if (prev != -1 && !(prev == 'e' || prev == 'E'))
                            {
                                throw new DeserializationException("Unexpected -", reader, false);
                            }
                            firstValidCharIdx = idx;
                        }
                        else
                        {
                            if (c == 'e' || c == 'E')
                            {
                                if (eIdx >= 0 || firstDigitIdx < 0)
                                {
                                    throw new DeserializationException("Unexpected " + c, reader, false);
                                }
                                eIdx = idx;
                            }
                            else
                            {
                                if (c == '.')
                                {
                                    if (eIdx >= 0 || decimalPointIdx >= 0)
                                    {
                                        throw new DeserializationException("Unexpected .", reader, false);
                                    }
                                    decimalPointIdx = idx;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                buffer[idx] = (char)c;
                idx++;

                if (idx >= buffer.Length)
                {
                    GrowDynamicBuffer(ref buffer);
                }

                reader.Read();
                prev = c;
            }

            if (firstDigitIdx == -1) throw new DeserializationException("Expected a double value", reader, false);

            if (firstDigitIdx == -1) throw new DeserializationException("Expected a double value", reader, false);

            if (buffer[idx - 1] == '.') throw new DeserializationException("Number cannot end with .", reader, false);
            if (idx >= 2 && buffer[0] == '0')
            {
                var secondChar = buffer[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }
            if (idx >= 3 && buffer[0] == '-' && buffer[1] == '0')
            {
                var secondChar = buffer[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }

            if (eIdx < 0)
            {
                var endIdx = idx;
                while (decimalPointIdx >= 0 && endIdx > 1 && buffer[endIdx - 1] == '0')
                {
                    endIdx--;
                }

                var startIdx =
                    decimalPointIdx < 0 ? 
                        firstDigitIdx :
                        Math.Min(decimalPointIdx, firstDigitIdx);

                while (startIdx < endIdx && buffer[startIdx] == '0')
                {
                    startIdx++;
                }

                var hasIntegerComponent = buffer[startIdx] != '.';
                var includesDecimalPoint = decimalPointIdx >= 0;
                var lastCharIs5 = endIdx > 1 && buffer[endIdx - 1] == '5';
                var maxChars =  5 + 
                    (hasIntegerComponent ? 1 : 0) + 
                    (includesDecimalPoint ? 1 : 0) + 
                    (lastCharIs5 ? 1 : 0);

                if (endIdx - startIdx <= maxChars)
                {
                    if (decimalPointIdx == endIdx - 1)
                    {
                        decimalPointIdx = -1;
                        endIdx--;
                    }

                    var n = 0;
                    for (idx = startIdx; idx < endIdx; ++idx)
                    {
                        if (idx != decimalPointIdx)
                            n = n * 10 + buffer[idx] - '0';
                    }

                    if (buffer[firstValidCharIdx] == '-')
                    {
                        n = -n;
                    }

                    var result = (double)n;
                    if (decimalPointIdx >= 0)
                    {
                        result /= DoubleDividers[endIdx - decimalPointIdx - 1];
                    }

                    return result;
                }
            }

            return double.Parse(new string(buffer, 0, idx), CultureInfo.InvariantCulture);
        }

        static readonly MethodInfo ReadSingle = typeof(Methods).GetMethod("_ReadSingle", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float _ReadSingle(TextReader reader, ref StringBuilder commonSb)
        {
            commonSb = commonSb ?? new StringBuilder();

            int c;

            var prev = -1;
            var afterFirstDigit = false;
            var afterE = false;
            var afterDot = false;

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
                        if (prev != -1 && !(prev == 'e' || prev == 'E'))
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

            var asStr = commonSb.ToString();
            var strLen = asStr.Length;

            if (strLen == 0) throw new DeserializationException("Expected a float value", reader, false);

            if (asStr[strLen - 1] == '.') throw new DeserializationException("Number cannot end with .", reader, false);
            if (strLen >= 2 && asStr[0] == '0')
            {
                var secondChar = asStr[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }
            if (strLen >= 3 && asStr[0] == '-' && asStr[1] == '0')
            {
                var secondChar = asStr[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }

            var result = float.Parse(commonSb.ToString(), CultureInfo.InvariantCulture);
            commonSb.Clear();
            return result;
        }

        static readonly MethodInfo ReadSingleCharArray = typeof(Methods).GetMethod("_ReadSingleCharArray", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float _ReadSingleCharArray(TextReader reader, ref char[] buffer)
        {
            var idx = 0;
            InitDynamicBuffer(ref buffer);

            int c;

            var prev = -1;
            var afterFirstDigit = false;
            var afterE = false;
            var afterDot = false;

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
                        if (prev != -1 && !(prev == 'e' || prev == 'E'))
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
                buffer[idx++] = (char)c;
                if (idx >= buffer.Length)
                {
                    GrowDynamicBuffer(ref buffer);
                }
                reader.Read();
                prev = c;
            }

            if (idx == 0) throw new DeserializationException("Expected a float value", reader, false);

            if (idx == 0) throw new DeserializationException("Expected a float value", reader, false);

            if (buffer[idx - 1] == '.') throw new DeserializationException("Number cannot end with .", reader, false);
            if (idx >= 2 && buffer[0] == '0')
            {
                var secondChar = buffer[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }
            if (idx >= 3 && buffer[0] == '-' && buffer[1] == '0')
            {
                var secondChar = buffer[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }

            var result = float.Parse(new string(buffer, 0, idx), CultureInfo.InvariantCulture);
            return result;
        }

        private static readonly float[] SingleDividers = new[] {
            1.0f,
            10.0f,
            100.0f,
            1000.0f,
            10000.0f,
            100000.0f,
            1000000.0f,
            10000000.0f,
            100000000.0f,
            1000000000.0f,
            10000000000.0f,
        };

        static readonly MethodInfo ReadSingleFast = typeof(Methods).GetMethod("_ReadSingleFast", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float _ReadSingleFast(TextReader reader, ref char[] buffer)
        {
            var idx = 0;
            InitDynamicBuffer(ref buffer);

            int c;

            var prev = -1;

            var firstDigitIdx = -1;
            var firstValidCharIdx = -1;
            var decimalPointIdx = -1;
            var eIdx = -1;

            while ((c = reader.Peek()) != -1)
            {
                if (c >= '0' && c <= '9')
                {
                    if (firstDigitIdx < 0)
                    {
                        firstDigitIdx = idx;
                        if (firstValidCharIdx < 0)
                        {
                            firstValidCharIdx = idx;
                        }
                    }
                }
                else
                {
                    if (c == '+')
                    {
                        if (!(prev == 'e' || prev == 'E'))
                        {
                            throw new DeserializationException("Unexpected +", reader, false);
                        }
                        firstValidCharIdx = idx;
                    }
                    else
                    {
                        if (c == '-')
                        {
                            if (prev != -1 && !(prev == 'e' || prev == 'E'))
                            {
                                throw new DeserializationException("Unexpected -", reader, false);
                            }
                            firstValidCharIdx = idx;
                        }
                        else
                        {
                            if (c == 'e' || c == 'E')
                            {
                                if (eIdx >= 0 || firstDigitIdx < 0)
                                {
                                    throw new DeserializationException("Unexpected " + c, reader, false);
                                }
                                eIdx = idx;
                            }
                            else
                            {
                                if (c == '.')
                                {
                                    if (eIdx >= 0 || decimalPointIdx >= 0)
                                    {
                                        throw new DeserializationException("Unexpected .", reader, false);
                                    }
                                    decimalPointIdx = idx;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                buffer[idx] = (char)c;
                idx++;

                if (idx >= buffer.Length)
                {
                    GrowDynamicBuffer(ref buffer);
                }

                reader.Read();
                prev = c;
            }

            if(firstDigitIdx == -1) throw new DeserializationException("Expected a float value", reader, false);

            if(firstDigitIdx == -1) throw new DeserializationException("Expected a float value", reader, false);

            if (buffer[idx - 1] == '.') throw new DeserializationException("Number cannot end with .", reader, false);
            if (idx >= 2 && buffer[0] == '0')
            {
                var secondChar = buffer[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }
            if (idx >= 3 && buffer[0] == '-' && buffer[1] == '0')
            {
                var secondChar = buffer[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }

            if (eIdx < 0)
            {
                var endIdx = idx;
                while (decimalPointIdx >= 0 && endIdx > 1 && buffer[endIdx - 1] == '0')
                {
                    endIdx--;
                }

                var startIdx =
                    decimalPointIdx < 0 ? 
                        firstDigitIdx : 
                        Math.Min(decimalPointIdx, firstDigitIdx);

                while (startIdx < endIdx && buffer[startIdx] == '0')
                {
                    startIdx++;
                }

                var hasIntegerComponent = buffer[startIdx] != '.';
                var includesDecimalPoint = decimalPointIdx >= 0;
                var maxChars = 
                    6 + 
                    (hasIntegerComponent ? 1 : 0) + 
                    (includesDecimalPoint ? 1 : 0);

                if (endIdx - startIdx <= maxChars)
                {
                    if (decimalPointIdx == endIdx - 1)
                    {
                        decimalPointIdx = -1;
                        endIdx--;
                    }

                    var n = 0;
                    for (idx = startIdx; idx < endIdx; idx++)
                    {
                        if (idx != decimalPointIdx)
                        {
                            n = n * 10 + buffer[idx] - '0';
                        }
                    }

                    if (buffer[firstValidCharIdx] == '-')
                    {
                        n = -n;
                    }

                    var result = (float)n;
                    if (decimalPointIdx >= 0)
                    {
                        result /= SingleDividers[endIdx - decimalPointIdx - 1];
                    }

                    return result;
                }
            }

            return float.Parse(new string(buffer, 0, idx), CultureInfo.InvariantCulture);
        }

        static readonly MethodInfo ReadDecimal = typeof(Methods).GetMethod("_ReadDecimal", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static decimal _ReadDecimal(TextReader reader, ref StringBuilder commonSb)
        {
            commonSb = commonSb ?? new StringBuilder();

            int c;

            var prev = -1;
            var afterFirstDigit = false;
            var afterE = false;
            var afterDot = false;

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
                        if (prev != -1 && !(prev == 'e' || prev == 'E'))
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

            var asStr = commonSb.ToString();
            var strLen = asStr.Length;

            if (strLen == 0) throw new DeserializationException("Expected a decimal value", reader, false);

            if (asStr[strLen - 1] == '.') throw new DeserializationException("Number cannot end with .", reader, false);
            if (strLen >= 2 && asStr[0] == '0')
            {
                var secondChar = asStr[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }
            if (strLen >= 3 && asStr[0] == '-' && asStr[1] == '0')
            {
                var secondChar = asStr[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }

            var result = decimal.Parse(asStr, NumberStyles.Float, CultureInfo.InvariantCulture);
            commonSb.Clear();
            return result;
        }

        static readonly MethodInfo ReadDecimalCharArray = typeof(Methods).GetMethod("_ReadDecimalCharArray", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static decimal _ReadDecimalCharArray(TextReader reader, ref char[] buffer)
        {
            var idx = 0;
            InitDynamicBuffer(ref buffer);

            int c;

            var prev = -1;
            var afterFirstDigit = false;
            var afterE = false;
            var afterDot = false;

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
                        if (prev != -1 && !(prev == 'e' || prev == 'E'))
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
                buffer[idx++] = (char)c;
                if (idx >= buffer.Length)
                {
                    GrowDynamicBuffer(ref buffer);
                }
                reader.Read();
                prev = c;
            }

            if (idx == 0) throw new DeserializationException("Expected a decimal value", reader, false);

            if (idx == 0) throw new DeserializationException("Expected a decimal value", reader, false);

            if (buffer[idx - 1] == '.') throw new DeserializationException("Number cannot end with .", reader, false);
            if (idx >= 2 && buffer[0] == '0')
            {
                var secondChar = buffer[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }
            if (idx >= 3 && buffer[0] == '-' && buffer[1] == '0')
            {
                var secondChar = buffer[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }

            var asStr = new string(buffer, 0, idx);
            var result = decimal.Parse(asStr, NumberStyles.Float, CultureInfo.InvariantCulture);
            return result;
        }

        private static readonly decimal[] DecimalMultipliers = new[] {
                1m, 
                0.1m,
                0.01m, 
                0.001m, 
                0.0001m, 
                0.00001m, 
                0.000001m, 
                0.0000001m, 
                0.00000001m,
                0.000000001m, 
                0.0000000001m, 
                0.00000000001m, 
                0.000000000001m, 
                0.0000000000001m, 
                0.00000000000001m, 
                0.000000000000001m, 
                0.0000000000000001m, 
                0.00000000000000001m
            };

        static readonly MethodInfo ReadDecimalFast = typeof(Methods).GetMethod("_ReadDecimalFast", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static decimal _ReadDecimalFast(TextReader reader, ref char[] buffer)
        {
            var idx = 0;
            InitDynamicBuffer(ref buffer);

            int c;

            var prev = -1;

            var firstDigitIdx = -1;
            var firstValidCharIdx = -1;
            var decimalPointIdx = -1;
            var eIdx = -1;

            while ((c = reader.Peek()) != -1)
            {
                if (c >= '0' && c <= '9')
                {
                    if (firstDigitIdx < 0)
                    {
                        firstDigitIdx = idx;
                        if (firstValidCharIdx < 0)
                        {
                            firstValidCharIdx = idx;
                        }
                    }
                }
                else
                {
                    if (c == '+')
                    {
                        if (!(prev == 'e' || prev == 'E'))
                        {
                            throw new DeserializationException("Unexpected +", reader, false);
                        }
                        firstValidCharIdx = idx;
                    }
                    else
                    {
                        if (c == '-')
                        {
                            if (prev != -1 && !(prev == 'e' || prev == 'E'))
                            {
                                throw new DeserializationException("Unexpected -", reader, false);
                            }
                            firstValidCharIdx = idx;
                        }
                        else
                        {
                            if (c == 'e' || c == 'E')
                            {
                                if (eIdx >= 0 || firstDigitIdx < 0)
                                {
                                    throw new DeserializationException("Unexpected " + c, reader, false);
                                }
                                eIdx = idx;
                            }
                            else
                            {
                                if (c == '.')
                                {
                                    if (eIdx >= 0 || decimalPointIdx >= 0)
                                    {
                                        throw new DeserializationException("Unexpected .", reader, false);
                                    }
                                    decimalPointIdx = idx;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                buffer[idx] = (char)c;
                idx++;

                if (idx >= buffer.Length)
                {
                    GrowDynamicBuffer(ref buffer);
                }

                reader.Read();
                prev = c;
            }

            if (firstDigitIdx == -1) throw new DeserializationException("Expected a decimal value", reader, false);

            if (firstDigitIdx == -1) throw new DeserializationException("Expected a decimal value", reader, false);

            if (buffer[idx - 1] == '.') throw new DeserializationException("Number cannot end with .", reader, false);
            if (idx >= 2 && buffer[0] == '0')
            {
                var secondChar = buffer[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }
            if (idx >= 3 && buffer[0] == '-' && buffer[1] == '0')
            {
                var secondChar = buffer[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", reader, false);
                }
            }

            if (eIdx < 0)
            {
                var endIdx = idx;

                var maxChars = decimalPointIdx < 0 ? 18 : 19;
                if (endIdx - firstDigitIdx <= maxChars)
                {
                    if (decimalPointIdx == endIdx - 1)
                    {
                        decimalPointIdx = -1;
                        endIdx--;
                    }

                    var negative = buffer[firstValidCharIdx] == '-';

                    decimal result;
                    var n1 = 0; // we use int rather than long so as to work well on 32-bit runtime

                    for (idx = firstDigitIdx; idx < endIdx && n1 < 100000000; ++idx)
                    {
                        if (idx != decimalPointIdx)
                        {
                            n1 = n1 * 10 + buffer[idx] - '0';
                        }
                    }

                    if (negative)
                    {
                        n1 = -n1;
                    }

                    if (idx == endIdx)
                    {
                        result = n1;
                    }
                    else
                    {
                        var n2 = 0;
                        var multiplier = 1;
                        while(idx < endIdx)
                        {
                            if (idx != decimalPointIdx)
                            {
                                multiplier *= 10;
                                n2 = n2 * 10 + buffer[idx] - '0';
                            }

                            idx++;
                        }

                        if (negative)
                        {
                            n2 = -n2;
                        }

                        result = (long)n1 * multiplier + (long)n2;
                    }

                    if (decimalPointIdx > 0)
                    {
                        result *= DecimalMultipliers[endIdx - decimalPointIdx - 1];
                    }

                    return result;
                }
            }

            return decimal.Parse(new string(buffer, 0, idx), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AssertNotFollowedByDigit(TextReader reader)
        {
            var next = reader.Peek();

            if (next >= '0' && next <= '9')
            {
                throw new DeserializationException(new OverflowException("Number did not end when expected, may overflow"), reader, false);
            }
        }
    }
}
