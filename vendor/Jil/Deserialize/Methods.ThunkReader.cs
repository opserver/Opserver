using Jil.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Deserialize
{
    static partial class Methods
    {
        static readonly MethodInfo ConsumeWhiteSpaceThunkReader = typeof(Methods).GetMethod("_ConsumeWhiteSpaceThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _ConsumeWhiteSpaceThunkReader(ref ThunkReader reader)
        {
            int c;
            while ((c = reader.Peek()) != -1)
            {
                if (!IsWhiteSpace(c)) return;

                reader.Read();
            }
        }

        static readonly MethodInfo ReadUInt8ThunkReader = typeof(Methods).GetMethod("_ReadUInt8ThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte _ReadUInt8ThunkReader(ref ThunkReader reader)
        {
            // max:  512
            // min:    0
            // digits: 3

            uint ret = 0;

            // first digit *must* exist, we can't overread
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Unexpected end of reader", ref reader, true);
            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", ref reader, false);
            ret += (uint)c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (byte)ret;
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
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

            AssertNotFollowedByDigit(ref reader);

            checked
            {
                return (byte)ret;
            }
        }

        static readonly MethodInfo ReadInt8ThunkReader = typeof(Methods).GetMethod("_ReadInt8ThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static sbyte _ReadInt8ThunkReader(ref ThunkReader reader)
        {
            // max:  127
            // min: -127
            // digits: 3

            int ret = 0;
            var negative = false;

            // digit #1
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected digit or '-'", ref reader, true);

            if (c == '-')
            {
                negative = true;
                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected digit", ref reader, true);
            }

            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", ref reader, false);
            ret += c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (sbyte)(ret * (negative ? -1 : 1));
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
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

            AssertNotFollowedByDigit(ref reader);

            checked
            {
                return (sbyte)(ret * (negative ? -1 : 1));
            }
        }

        static readonly MethodInfo ReadInt16ThunkReader = typeof(Methods).GetMethod("_ReadInt16ThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static short _ReadInt16ThunkReader(ref ThunkReader reader)
        {
            // max:  32767
            // min: -32768
            // digits:   5

            int ret = 0;
            var negative = false;

            // digit #1
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected digit or '-'", ref reader, true);

            if (c == '-')
            {
                negative = true;
                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected digit", ref reader, true);
            }

            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", ref reader, false);
            ret += c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (short)(ret * (negative ? -1 : 1));
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
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

            AssertNotFollowedByDigit(ref reader);

            checked
            {
                return (short)(ret * (negative ? -1 : 1));
            }
        }

        static readonly MethodInfo ReadUInt16ThunkReader = typeof(Methods).GetMethod("_ReadUInt16ThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ushort _ReadUInt16ThunkReader(ref ThunkReader reader)
        {
            // max: 65535
            // min:     0
            // digits:  5

            uint ret = 0;

            // digit #1
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected digit", ref reader, true);

            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", ref reader, false);
            ret += (uint)c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (ushort)ret;
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
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

            AssertNotFollowedByDigit(ref reader);

            checked
            {
                return (ushort)ret;
            }
        }

        static readonly MethodInfo ReadInt32ThunkReader = typeof(Methods).GetMethod("_ReadInt32ThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int _ReadInt32ThunkReader(ref ThunkReader reader)
        {
            // max:  2147483647
            // min: -2147483648
            // digits:       10

            long ret = 0;
            var negative = false;

            // digit #1
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected digit or '-'", ref reader, true);

            if (c == '-')
            {
                negative = true;
                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected digit", ref reader, true);
            }

            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", ref reader, false);
            ret += c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (int)(ret * (negative ? -1 : 1));
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
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

            AssertNotFollowedByDigit(ref reader);

            checked
            {
                return (int)(ret * (negative ? -1 : 1));
            }
        }

        static readonly MethodInfo ReadUInt32ThunkReader = typeof(Methods).GetMethod("_ReadUInt32ThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint _ReadUInt32ThunkReader(ref ThunkReader reader)
        {
            // max: 4294967295
            // min:          0
            // digits:      10

            ulong ret = 0;

            // digit #1
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected digit", ref reader, true);

            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", ref reader, false);
            ret += (uint)c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return (uint)ret;
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
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

            AssertNotFollowedByDigit(ref reader);

            checked
            {
                return (uint)ret;
            }
        }

        static readonly MethodInfo ReadInt64ThunkReader = typeof(Methods).GetMethod("_ReadInt64ThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long _ReadInt64ThunkReader(ref ThunkReader reader)
        {
            // max:  9223372036854775807
            // min: -9223372036854775808
            // digits:                19

            ulong ret = 0;
            var negative = false;

            // digit #1
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected digit or '-'", ref reader, true);

            if (c == '-')
            {
                negative = true;
                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected digit", ref reader, true);
            }

            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", ref reader, false);
            ret += (uint)c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ((long)ret * (negative ? -1 : 1));
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
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

            AssertNotFollowedByDigit(ref reader);

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

        static readonly MethodInfo ReadUInt64ThunkReader = typeof(Methods).GetMethod("_ReadUInt64ThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong _ReadUInt64ThunkReader(ref ThunkReader reader)
        {
            // max: 18446744073709551615
            // min:                    0
            // digits:                20

            ulong ret = 0;

            // digit #1
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected digit", ref reader, true);

            var firstDigitZero = c == '0';
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", ref reader, false);
            ret += (uint)c;

            // digit #2
            c = reader.Peek();
            c = c - '0';
            if (c < 0 || c > 9) return ret;
            if (firstDigitZero) throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
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

            AssertNotFollowedByDigit(ref reader);

            // ulong special case, we can overflow in the last **addition* instead of the last cast
            checked
            {
                ret *= 10;
                ret += (uint)c;

                return ret;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AssertNotFollowedByDigit(ref ThunkReader reader)
        {
            var next = reader.Peek();

            if (next >= '0' && next <= '9')
            {
                throw new DeserializationException(new OverflowException("Number did not end when expected, may overflow"), ref reader, false);
            }
        }

        static readonly MethodInfo ReadDoubleFastThunkReader = typeof(Methods).GetMethod("_ReadDoubleFastThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double _ReadDoubleFastThunkReader(ref ThunkReader reader, ref char[] buffer)
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
                            throw new DeserializationException("Unexpected +", ref reader, false);
                        }
                        firstValidCharIdx = idx;
                    }
                    else
                    {
                        if (c == '-')
                        {
                            if (prev != -1 && !(prev == 'e' || prev == 'E'))
                            {
                                throw new DeserializationException("Unexpected -", ref reader, false);
                            }
                            firstValidCharIdx = idx;
                        }
                        else
                        {
                            if (c == 'e' || c == 'E')
                            {
                                if (eIdx >= 0 || firstDigitIdx < 0)
                                {
                                    throw new DeserializationException("Unexpected " + c, ref reader, false);
                                }
                                eIdx = idx;
                            }
                            else
                            {
                                if (c == '.')
                                {
                                    if (eIdx >= 0 || decimalPointIdx >= 0)
                                    {
                                        throw new DeserializationException("Unexpected .", ref reader, false);
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

            if (idx == 0) throw new DeserializationException("Expected a double value", ref reader, false);

            if (buffer[idx - 1] == '.') throw new DeserializationException("Number cannot end with .", ref reader, false);
            if (idx >= 2 && buffer[0] == '0')
            {
                var secondChar = buffer[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
                }
            }
            if (idx >= 3 && buffer[0] == '-' && buffer[1] == '0')
            {
                var secondChar = buffer[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
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
                var maxChars = 5 +
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

        static readonly MethodInfo ReadDoubleCharArrayThunkReader = typeof(Methods).GetMethod("_ReadDoubleCharArrayThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double _ReadDoubleCharArrayThunkReader(ref ThunkReader reader, ref char[] buffer)
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
                            throw new DeserializationException("Unexpected +", ref reader, false);
                        }

                        goto storeChar;
                    }

                    var isMinus = c == '-';
                    if (isMinus)
                    {
                        if (prev != -1 && !(prev == 'e' || prev == 'E'))
                        {
                            throw new DeserializationException("Unexpected -", ref reader, false);
                        }

                        goto storeChar;
                    }

                    var isE = c == 'e' || c == 'E';
                    if (isE)
                    {
                        if (afterE || !afterFirstDigit)
                        {
                            throw new DeserializationException("Unexpected " + c, ref reader, false);
                        }

                        afterE = true;
                        goto storeChar;
                    }

                    var isDot = c == '.';
                    if (isDot)
                    {
                        if (!afterFirstDigit || afterE || afterDot)
                        {
                            throw new DeserializationException("Unexpected .", ref reader, false);
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

            if (idx == 0) throw new DeserializationException("Expected a double value", ref reader, false);

            if (buffer[idx - 1] == '.') throw new DeserializationException("Number cannot end with .", ref reader, false);
            if (idx >= 2 && buffer[0] == '0')
            {
                var secondChar = buffer[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
                }
            }
            if (idx >= 3 && buffer[0] == '-' && buffer[1] == '0')
            {
                var secondChar = buffer[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
                }
            }


            var result = double.Parse(new string(buffer, 0, idx), CultureInfo.InvariantCulture);
            return result;
        }

        static readonly MethodInfo ReadDoubleThunkReader = typeof(Methods).GetMethod("_ReadDoubleThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double _ReadDoubleThunkReader(ref ThunkReader reader, ref StringBuilder commonSb)
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
                            throw new DeserializationException("Unexpected +", ref reader, false);
                        }

                        goto storeChar;
                    }

                    var isMinus = c == '-';
                    if (isMinus)
                    {
                        if (prev != -1 && !(prev == 'e' || prev == 'E'))
                        {
                            throw new DeserializationException("Unexpected -", ref reader, false);
                        }

                        goto storeChar;
                    }

                    var isE = c == 'e' || c == 'E';
                    if (isE)
                    {
                        if (afterE || !afterFirstDigit)
                        {
                            throw new DeserializationException("Unexpected " + c, ref reader, false);
                        }

                        afterE = true;
                        goto storeChar;
                    }

                    var isDot = c == '.';
                    if (isDot)
                    {
                        if (!afterFirstDigit || afterE || afterDot)
                        {
                            throw new DeserializationException("Unexpected .", ref reader, false);
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

            if (strLen == 0) throw new DeserializationException("Expected a double value", ref reader, false);

            if (asStr[strLen - 1] == '.') throw new DeserializationException("Number cannot end with .", ref reader, false);
            if (strLen >= 2 && asStr[0] == '0')
            {
                var secondChar = asStr[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
                }
            }
            if (strLen >= 3 && asStr[0] == '-' && asStr[1] == '0')
            {
                var secondChar = asStr[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
                }
            }

            var result = double.Parse(asStr, CultureInfo.InvariantCulture);
            commonSb.Clear();
            return result;
        }

        static readonly MethodInfo ReadSingleFastThunkReader = typeof(Methods).GetMethod("_ReadSingleFastThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float _ReadSingleFastThunkReader(ref ThunkReader reader, ref char[] buffer)
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
                            throw new DeserializationException("Unexpected +", ref reader, false);
                        }
                        firstValidCharIdx = idx;
                    }
                    else
                    {
                        if (c == '-')
                        {
                            if (prev != -1 && !(prev == 'e' || prev == 'E'))
                            {
                                throw new DeserializationException("Unexpected -", ref reader, false);
                            }
                            firstValidCharIdx = idx;
                        }
                        else
                        {
                            if (c == 'e' || c == 'E')
                            {
                                if (eIdx >= 0 || firstDigitIdx < 0)
                                {
                                    throw new DeserializationException("Unexpected " + c, ref reader, false);
                                }
                                eIdx = idx;
                            }
                            else
                            {
                                if (c == '.')
                                {
                                    if (eIdx >= 0 || decimalPointIdx >= 0)
                                    {
                                        throw new DeserializationException("Unexpected .", ref reader, false);
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

            if (idx == 0) throw new DeserializationException("Expected a float value", ref reader, false);

            if (buffer[idx - 1] == '.') throw new DeserializationException("Number cannot end with .", ref reader, false);
            if (idx >= 2 && buffer[0] == '0')
            {
                var secondChar = buffer[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
                }
            }
            if (idx >= 3 && buffer[0] == '-' && buffer[1] == '0')
            {
                var secondChar = buffer[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
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

        static readonly MethodInfo ReadSingleCharArrayThunkReader = typeof(Methods).GetMethod("_ReadSingleCharArrayThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float _ReadSingleCharArrayThunkReader(ref ThunkReader reader, ref char[] buffer)
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
                            throw new DeserializationException("Unexpected +", ref reader, false);
                        }

                        goto storeChar;
                    }

                    var isMinus = c == '-';
                    if (isMinus)
                    {
                        if (prev != -1 && !(prev == 'e' || prev == 'E'))
                        {
                            throw new DeserializationException("Unexpected -", ref reader, false);
                        }

                        goto storeChar;
                    }

                    var isE = c == 'e' || c == 'E';
                    if (isE)
                    {
                        if (afterE || !afterFirstDigit)
                        {
                            throw new DeserializationException("Unexpected " + c, ref reader, false);
                        }

                        afterE = true;
                        goto storeChar;
                    }

                    var isDot = c == '.';
                    if (isDot)
                    {
                        if (!afterFirstDigit || afterE || afterDot)
                        {
                            throw new DeserializationException("Unexpected .", ref reader, false);
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

            if (idx == 0) throw new DeserializationException("Expected a float value", ref reader, false);

            if (buffer[idx - 1] == '.') throw new DeserializationException("Number cannot end with .", ref reader, false);
            if (idx >= 2 && buffer[0] == '0')
            {
                var secondChar = buffer[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
                }
            }
            if (idx >= 3 && buffer[0] == '-' && buffer[1] == '0')
            {
                var secondChar = buffer[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
                }
            }

            var result = float.Parse(new string(buffer, 0, idx), CultureInfo.InvariantCulture);
            return result;
        }

        static readonly MethodInfo ReadSingleThunkReader = typeof(Methods).GetMethod("_ReadSingleThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float _ReadSingleThunkReader(ref ThunkReader reader, ref StringBuilder commonSb)
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
                            throw new DeserializationException("Unexpected +", ref reader, false);
                        }

                        goto storeChar;
                    }

                    var isMinus = c == '-';
                    if (isMinus)
                    {
                        if (prev != -1 && !(prev == 'e' || prev == 'E'))
                        {
                            throw new DeserializationException("Unexpected -", ref reader, false);
                        }

                        goto storeChar;
                    }

                    var isE = c == 'e' || c == 'E';
                    if (isE)
                    {
                        if (afterE || !afterFirstDigit)
                        {
                            throw new DeserializationException("Unexpected " + c, ref reader, false);
                        }

                        afterE = true;
                        goto storeChar;
                    }

                    var isDot = c == '.';
                    if (isDot)
                    {
                        if (!afterFirstDigit || afterE || afterDot)
                        {
                            throw new DeserializationException("Unexpected .", ref reader, false);
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

            if (strLen == 0) throw new DeserializationException("Expected a float value", ref reader, false);

            if (asStr[strLen - 1] == '.') throw new DeserializationException("Number cannot end with .", ref reader, false);
            if (strLen >= 2 && asStr[0] == '0')
            {
                var secondChar = asStr[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
                }
            }
            if (strLen >= 3 && asStr[0] == '-' && asStr[1] == '0')
            {
                var secondChar = asStr[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
                }
            }

            var result = float.Parse(commonSb.ToString(), CultureInfo.InvariantCulture);
            commonSb.Clear();
            return result;
        }

        static readonly MethodInfo ReadDecimalFastThunkReader = typeof(Methods).GetMethod("_ReadDecimalFastThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static decimal _ReadDecimalFastThunkReader(ref ThunkReader reader, ref char[] buffer)
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
                            throw new DeserializationException("Unexpected +", ref reader, false);
                        }
                        firstValidCharIdx = idx;
                    }
                    else
                    {
                        if (c == '-')
                        {
                            if (prev != -1 && !(prev == 'e' || prev == 'E'))
                            {
                                throw new DeserializationException("Unexpected -", ref reader, false);
                            }
                            firstValidCharIdx = idx;
                        }
                        else
                        {
                            if (c == 'e' || c == 'E')
                            {
                                if (eIdx >= 0 || firstDigitIdx < 0)
                                {
                                    throw new DeserializationException("Unexpected " + c, ref reader, false);
                                }
                                eIdx = idx;
                            }
                            else
                            {
                                if (c == '.')
                                {
                                    if (eIdx >= 0 || decimalPointIdx >= 0)
                                    {
                                        throw new DeserializationException("Unexpected .", ref reader, false);
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

            if (idx == 0) throw new DeserializationException("Expected a decimal value", ref reader, false);

            if (buffer[idx - 1] == '.') throw new DeserializationException("Number cannot end with .", ref reader, false);
            if (idx >= 2 && buffer[0] == '0')
            {
                var secondChar = buffer[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
                }
            }
            if (idx >= 3 && buffer[0] == '-' && buffer[1] == '0')
            {
                var secondChar = buffer[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
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
                        while (idx < endIdx)
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

        static readonly MethodInfo ReadDecimalCharArrayThunkReader = typeof(Methods).GetMethod("_ReadDecimalCharArrayThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static decimal _ReadDecimalCharArrayThunkReader(ref ThunkReader reader, ref char[] buffer)
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
                            throw new DeserializationException("Unexpected +", ref reader, false);
                        }

                        goto storeChar;
                    }

                    var isMinus = c == '-';
                    if (isMinus)
                    {
                        if (prev != -1 && !(prev == 'e' || prev == 'E'))
                        {
                            throw new DeserializationException("Unexpected -", ref reader, false);
                        }

                        goto storeChar;
                    }

                    var isE = c == 'e' || c == 'E';
                    if (isE)
                    {
                        if (afterE || !afterFirstDigit)
                        {
                            throw new DeserializationException("Unexpected " + c, ref reader, false);
                        }

                        afterE = true;
                        goto storeChar;
                    }

                    var isDot = c == '.';
                    if (isDot)
                    {
                        if (!afterFirstDigit || afterE || afterDot)
                        {
                            throw new DeserializationException("Unexpected .", ref reader, false);
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

            if (idx == 0) throw new DeserializationException("Expected a decimal value", ref reader, false);

            if (buffer[idx - 1] == '.') throw new DeserializationException("Number cannot end with .", ref reader, false);
            if (idx >= 2 && buffer[0] == '0')
            {
                var secondChar = buffer[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
                }
            }
            if (idx >= 3 && buffer[0] == '-' && buffer[1] == '0')
            {
                var secondChar = buffer[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
                }
            }

            var asStr = new string(buffer, 0, idx);
            var result = decimal.Parse(asStr, NumberStyles.Float, CultureInfo.InvariantCulture);
            return result;
        }

        static readonly MethodInfo ReadDecimalThunkReader = typeof(Methods).GetMethod("_ReadDecimalThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static decimal _ReadDecimalThunkReader(ref ThunkReader reader, ref StringBuilder commonSb)
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
                            throw new DeserializationException("Unexpected +", ref reader, false);
                        }

                        goto storeChar;
                    }

                    var isMinus = c == '-';
                    if (isMinus)
                    {
                        if (prev != -1 && !(prev == 'e' || prev == 'E'))
                        {
                            throw new DeserializationException("Unexpected -", ref reader, false);
                        }

                        goto storeChar;
                    }

                    var isE = c == 'e' || c == 'E';
                    if (isE)
                    {
                        if (afterE || !afterFirstDigit)
                        {
                            throw new DeserializationException("Unexpected " + c, ref reader, false);
                        }

                        afterE = true;
                        goto storeChar;
                    }

                    var isDot = c == '.';
                    if (isDot)
                    {
                        if (!afterFirstDigit || afterE || afterDot)
                        {
                            throw new DeserializationException("Unexpected .", ref reader, false);
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

            if (strLen == 0) throw new DeserializationException("Expected a decimal value", ref reader, false);

            if (asStr[strLen - 1] == '.') throw new DeserializationException("Number cannot end with .", ref reader, false);
            if (strLen >= 2 && asStr[0] == '0')
            {
                var secondChar = asStr[1];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
                }
            }
            if (strLen >= 3 && asStr[0] == '-' && asStr[1] == '0')
            {
                var secondChar = asStr[2];
                if (secondChar != '.' && secondChar != 'e' && secondChar != 'E')
                {
                    throw new DeserializationException("Number cannot have leading zeros", ref reader, false);
                }
            }

            var result = decimal.Parse(asStr, NumberStyles.Float, CultureInfo.InvariantCulture);
            commonSb.Clear();
            return result;
        }

        static readonly MethodInfo ReadEncodedCharThunkReader = typeof(Methods).GetMethod("_ReadEncodedCharThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static char _ReadEncodedCharThunkReader(ref ThunkReader reader)
        {
            var first = reader.Read();
            if (first == -1) throw new DeserializationException("Expected any character", ref reader, true);

            if (first != '\\') return (char)first;

            var second = reader.Read();
            if (second == -1) throw new DeserializationException("Expected any character", ref reader, true);

            switch (second)
            {
                case '"': return '"';
                case '\\': return '\\';
                case '/': return '/';
                case 'b': return '\b';
                case 'f': return '\f';
                case 'n': return '\n';
                case 'r': return '\r';
                case 't': return '\t';
            }

            if (second != 'u') throw new DeserializationException("Unrecognized escape sequence", ref reader, false);

            // now we're in an escape sequence, we expect 4 hex #s; always
            var ret = 0;

            //char1:
            {
                var c = reader.Read();

                c -= '0';
                if (c >= 0 && c <= 9)
                {
                    ret += c;
                    goto char2;
                }

                c -= ('A' - '0');
                if (c >= 0 && c <= 5)
                {
                    ret += 10 + c;
                    goto char2;
                }

                c -= ('f' - 'F');
                if (c >= 0 && c <= 5)
                {
                    ret += 10 + c;
                    goto char2;
                }

                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, c == -1);
            }

        char2:
            ret *= 16;
            {
                var c = reader.Read();

                c -= '0';
                if (c >= 0 && c <= 9)
                {
                    ret += c;
                    goto char3;
                }

                c -= ('A' - '0');
                if (c >= 0 && c <= 5)
                {
                    ret += 10 + c;
                    goto char3;
                }

                c -= ('f' - 'F');
                if (c >= 0 && c <= 5)
                {
                    ret += 10 + c;
                    goto char3;
                }

                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, c == -1);
            }

        char3:
            ret *= 16;
            {
                var c = reader.Read();

                c -= '0';
                if (c >= 0 && c <= 9)
                {
                    ret += c;
                    goto char4;
                }

                c -= ('A' - '0');
                if (c >= 0 && c <= 5)
                {
                    ret += 10 + c;
                    goto char4;
                }

                c -= ('f' - 'F');
                if (c >= 0 && c <= 5)
                {
                    ret += 10 + c;
                    goto char4;
                }

                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, c == -1);
            }

        char4:
            ret *= 16;
            {
                var c = reader.Read();

                c -= '0';
                if (c >= 0 && c <= 9)
                {
                    ret += c;
                    goto finished;
                }

                c -= ('A' - '0');
                if (c >= 0 && c <= 5)
                {
                    ret += 10 + c;
                    goto finished;
                }

                c -= ('f' - 'F');
                if (c >= 0 && c <= 5)
                {
                    ret += 10 + c;
                    goto finished;
                }

                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, c == -1);
            }

        finished:
            if (ret < char.MinValue || ret > char.MaxValue) throw new DeserializationException("Encoded character out of System.Char range, found: " + ret, ref reader, false);

            return (char)ret;
        }

        static readonly MethodInfo ReadEncodedStringWithCharArrayThunkReader = typeof(Methods).GetMethod("_ReadEncodedStringWithCharArrayThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        static string _ReadEncodedStringWithCharArrayThunkReader(ref ThunkReader reader, ref char[] buffer)
        {
            var idx = 0;
            InitDynamicBuffer(ref buffer);

            while (true)
            {
                while (idx < buffer.Length - 1)
                {
                    var first = reader.Read();
                    if (first == -1) throw new DeserializationException("Expected any character", ref reader, true);

                    if (first == '"')
                    {
                        goto complete;
                    }

                    if (first != '\\')
                    {
                        buffer[idx++] = (char)first;
                        continue;
                    }

                    var second = reader.Read();
                    if (second == -1) throw new DeserializationException("Expected any character", ref reader, true);

                    switch (second)
                    {
                        case '"': buffer[idx++] = '"'; continue;
                        case '\\': buffer[idx++] = '\\'; continue;
                        case '/': buffer[idx++] = '/'; continue;
                        case 'b': buffer[idx++] = '\b'; continue;
                        case 'f': buffer[idx++] = '\f'; continue;
                        case 'n': buffer[idx++] = '\n'; continue;
                        case 'r': buffer[idx++] = '\r'; continue;
                        case 't': buffer[idx++] = '\t'; continue;
                    }

                    if (second != 'u') throw new DeserializationException("Unrecognized escape sequence", ref reader, false);

                    // now we're in an escape sequence, we expect 4 hex #s; always
                    buffer[idx++] = ReadHexQuad2(ref reader);
                }

                GrowDynamicBuffer(ref buffer);
            }

            complete: 
            return new string(buffer, 0, idx);
        }

        static char ReadHexQuad2(ref ThunkReader reader)
        {
            int unescaped;
            int c;

            c = reader.Read();
            if (c >= '0' && c <= '9')
            {
                unescaped = (c - '0') << 12;
            }
            else if (c >= 'A' && c <= 'F')
            {
                unescaped = (10 + c - 'A') << 12;
            }
            else if (c >= 'a' && c <= 'f')
            {
                unescaped = (10 + c - 'a') << 12;
            }
            else
            {
                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, c == -1);
            }

            c = reader.Read();
            if (c >= '0' && c <= '9')
            {
                unescaped |= (c - '0') << 8;
            }
            else if (c >= 'A' && c <= 'F')
            {
                unescaped |= (10 + c - 'A') << 8;
            }
            else if (c >= 'a' && c <= 'f')
            {
                unescaped |= (10 + c - 'a') << 8;
            }
            else
            {
                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, c == -1);
            }

            c = reader.Read();
            if (c >= '0' && c <= '9')
            {
                unescaped |= (c - '0') << 4;
            }
            else if (c >= 'A' && c <= 'F')
            {
                unescaped |= (10 + c - 'A') << 4;
            }
            else if (c >= 'a' && c <= 'f')
            {
                unescaped |= (10 + c - 'a') << 4;
            }
            else
            {
                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, c == -1);
            }

            c = reader.Read();
            if (c >= '0' && c <= '9')
            {
                unescaped |= c - '0';
            }
            else if (c >= 'A' && c <= 'F')
            {
                unescaped |= 10 + c - 'A';
            }
            else if (c >= 'a' && c <= 'f')
            {
                unescaped |= 10 + c - 'a';
            }
            else
            {
                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, c == -1);
            }

            return (char)unescaped;
        }

        static readonly MethodInfo ReadEncodedStringWithBufferThunkReader = typeof(Methods).GetMethod("_ReadEncodedStringWithBufferThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string _ReadEncodedStringWithBufferThunkReader(ref ThunkReader reader, char[] buffer, ref StringBuilder commonSb)
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
                    if (first == -1) throw new DeserializationException("Expected any character", ref reader, true);

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
                    if (second == -1) throw new DeserializationException("Expected any character", ref reader, true);

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

                    if (second != 'u') throw new DeserializationException("Unrecognized escape sequence", ref reader, false);

                    commonSb.Append(buffer, 0, ix);

                    // now we're in an escape sequence, we expect 4 hex #s; always
                    ReadHexQuadToBuilder(ref reader, commonSb);
                    break;
                }
            }

            // fall through to using a StringBuilder

            while (true)
            {
                var first = reader.Read();
                if (first == -1) throw new DeserializationException("Expected any character", ref reader, true);

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
                if (second == -1) throw new DeserializationException("Expected any character", ref reader, true);

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

                if (second != 'u') throw new DeserializationException("Unrecognized escape sequence", ref reader, false);

                // now we're in an escape sequence, we expect 4 hex #s; always
                ReadHexQuadToBuilder(ref reader, commonSb);
            }

            var ret = commonSb.ToString();

            // leave this clean for the next use
            commonSb.Clear();

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ReadHexQuadToBuilder(ref ThunkReader reader, StringBuilder commonSb)
        {
            var encodedChar = 0;

            //char1:
            {
                var c = reader.Read();

                if (c == -1) throw new DeserializationException("Expected hex digit, stream ended instead", ref reader, true);

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

                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, false);
            }

        char2:
            encodedChar *= 16;
            {
                var c = reader.Read();

                if (c == -1) throw new DeserializationException("Expected hex digit, stream ended instead", ref reader, true);

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

                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, false);
            }

        char3:
            encodedChar *= 16;
            {
                var c = reader.Read();

                if (c == -1) throw new DeserializationException("Expected hex digit, stream ended instead", ref reader, true);

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

                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, false);
            }

        char4:
            encodedChar *= 16;
            {
                var c = reader.Read();

                if (c == -1) throw new DeserializationException("Expected hex digit, stream ended instead", ref reader, true);

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

                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, false);
            }

        finished:
            commonSb.Append(Utils.SafeConvertFromUtf32(encodedChar));
        }

        static readonly MethodInfo ReadEncodedStringThunkReader = typeof(Methods).GetMethod("_ReadEncodedStringThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string _ReadEncodedStringThunkReader(ref ThunkReader reader, ref StringBuilder commonSb)
        {
            commonSb = commonSb ?? new StringBuilder();

            while (true)
            {
                var first = reader.Read();
                if (first == -1) throw new DeserializationException("Expected any character", ref reader, true);

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
                if (second == -1) throw new DeserializationException("Expected any character", ref reader, true);

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

                if (second != 'u') throw new DeserializationException("Unrecognized escape sequence", ref reader, false);

                // now we're in an escape sequence, we expect 4 hex #s; always
                ReadHexQuadToBuilder(ref reader, commonSb);
            }

            var ret = commonSb.ToString();

            // leave this clean for the next use
            commonSb.Clear();

            return ret;
        }

        static readonly MethodInfo ReadGuidThunkReader = typeof(Methods).GetMethod("_ReadGuidThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Guid _ReadGuidThunkReader(ref ThunkReader reader)
        {
            // 1314FAD4-7505-439D-ABD2-DBD89242928C
            // 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ
            //
            // Guid is guaranteed to be a 36 character string

            // Bytes are in a different order than you might expect
            // For: 35 91 8b c9 - 19 6d - 40 ea  - 97 79  - 88 9d 79 b7 53 f0 
            // Get: C9 8B 91 35   6D 19   EA 40    97 79    88 9D 79 B7 53 F0 
            // Ix:   0  1  2  3    4  5    6  7     8  9    10 11 12 13 14 15
            //
            // And we have to account for dashes
            //
            // So the map is like so:
            // chars[0]  -> bytes[3]  -> buffer[ 0, 1]
            // chars[1]  -> bytes[2]  -> buffer[ 2, 3]
            // chars[2]  -> bytes[1]  -> buffer[ 4, 5]
            // chars[3]  -> bytes[0]  -> buffer[ 6, 7]
            // chars[4]  -> bytes[5]  -> buffer[ 9,10]
            // chars[5]  -> bytes[4]  -> buffer[11,12]
            // chars[6]  -> bytes[7]  -> buffer[14,15]
            // chars[7]  -> bytes[6]  -> buffer[16,17]
            // chars[8]  -> bytes[8]  -> buffer[19,20]
            // chars[9]  -> bytes[9]  -> buffer[21,22]
            // chars[10] -> bytes[10] -> buffer[24,25]
            // chars[11] -> bytes[11] -> buffer[26,27]
            // chars[12] -> bytes[12] -> buffer[28,29]
            // chars[13] -> bytes[13] -> buffer[30,31]
            // chars[14] -> bytes[14] -> buffer[32,33]
            // chars[15] -> bytes[15] -> buffer[34,35]

            var asStruct = new GuidStruct();
            asStruct.B03 = ReadGuidByte(ref reader);
            asStruct.B02 = ReadGuidByte(ref reader);
            asStruct.B01 = ReadGuidByte(ref reader);
            asStruct.B00 = ReadGuidByte(ref reader);

            var c = reader.Read();
            if (c != '-') throw new DeserializationException("Expected -", ref reader, c == -1);

            asStruct.B05 = ReadGuidByte(ref reader);
            asStruct.B04 = ReadGuidByte(ref reader);

            c = reader.Read();
            if (c != '-') throw new DeserializationException("Expected -", ref reader, c == -1);

            asStruct.B07 = ReadGuidByte(ref reader);
            asStruct.B06 = ReadGuidByte(ref reader);

            c = reader.Read();
            if (c != '-') throw new DeserializationException("Expected -", ref reader, c == -1);

            asStruct.B08 = ReadGuidByte(ref reader);
            asStruct.B09 = ReadGuidByte(ref reader);

            c = reader.Read();
            if (c != '-') throw new DeserializationException("Expected -", ref reader, c == -1);

            asStruct.B10 = ReadGuidByte(ref reader);
            asStruct.B11 = ReadGuidByte(ref reader);
            asStruct.B12 = ReadGuidByte(ref reader);
            asStruct.B13 = ReadGuidByte(ref reader);
            asStruct.B14 = ReadGuidByte(ref reader);
            asStruct.B15 = ReadGuidByte(ref reader);

            return asStruct.Value;
        }

        static byte ReadGuidByte(ref ThunkReader reader)
        {
            var a = reader.Read();
            if (a == -1) throw new DeserializationException("Expected any character", ref reader, true);
            if (!((a >= '0' && a <= '9') || (a >= 'A' && a <= 'F') || (a >= 'a' && a <= 'f'))) throw new DeserializationException("Expected a hex number", ref reader, false);
            var b = reader.Read();
            if (b == -1) throw new DeserializationException("Expected any character", ref reader, true);
            if (!((b >= '0' && b <= '9') || (b >= 'A' && b <= 'F') || (b >= 'a' && b <= 'f'))) throw new DeserializationException("Expected a hex number", ref reader, false);

            if (a <= '9')
            {
                a -= '0';
            }
            else
            {
                if (a <= 'F')
                {
                    a -= ('A' - 10);
                }
                else
                {
                    a -= ('a' - 10);
                }
            }

            if (b <= '9')
            {
                b -= '0';
            }
            else
            {
                if (b <= 'F')
                {
                    b -= ('A' - 10);
                }
                else
                {
                    b -= ('a' - 10);
                }
            }

            return (byte)(a * 16 + b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadHexQuadThunkReader(ref ThunkReader reader)
        {
            int unescaped = 0;

            //char1:
            {
                var c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected hex digit, instead reader ended", ref reader, true);

                c -= '0';
                if (c >= 0 && c <= 9)
                {
                    unescaped += c;
                    goto char2;
                }

                c -= ('A' - '0');
                if (c >= 0 && c <= 5)
                {
                    unescaped += 10 + c;
                    goto char2;
                }

                c -= ('f' - 'F');
                if (c >= 0 && c <= 5)
                {
                    unescaped += 10 + c;
                    goto char2;
                }

                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, false);
            }

        char2:
            unescaped *= 16;
            {
                var c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected hex digit, instead reader ended", ref reader, true);

                c -= '0';
                if (c >= 0 && c <= 9)
                {
                    unescaped += c;
                    goto char3;
                }

                c -= ('A' - '0');
                if (c >= 0 && c <= 5)
                {
                    unescaped += 10 + c;
                    goto char3;
                }

                c -= ('f' - 'F');
                if (c >= 0 && c <= 5)
                {
                    unescaped += 10 + c;
                    goto char3;
                }

                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, false);
            }

        char3:
            unescaped *= 16;
            {
                var c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected hex digit, instead reader ended", ref reader, true);

                c -= '0';
                if (c >= 0 && c <= 9)
                {
                    unescaped += c;
                    goto char4;
                }

                c -= ('A' - '0');
                if (c >= 0 && c <= 5)
                {
                    unescaped += 10 + c;
                    goto char4;
                }

                c -= ('f' - 'F');
                if (c >= 0 && c <= 5)
                {
                    unescaped += 10 + c;
                    goto char4;
                }

                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, false);
            }

        char4:
            unescaped *= 16;
            {
                var c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected hex digit, instead reader ended", ref reader, true);

                c -= '0';
                if (c >= 0 && c <= 9)
                {
                    unescaped += c;
                    goto finished;
                }

                c -= ('A' - '0');
                if (c >= 0 && c <= 5)
                {
                    unescaped += 10 + c;
                    goto finished;
                }

                c -= ('f' - 'F');
                if (c >= 0 && c <= 5)
                {
                    unescaped += 10 + c;
                    goto finished;
                }

                throw new DeserializationException("Expected hex digit, found: " + c, ref reader, false);
            }

        finished:
            return unescaped;
        }

        static readonly MethodInfo ParseEnumThunkReader = typeof(Methods).GetMethod("_ParseEnumThunkReader", BindingFlags.NonPublic | BindingFlags.Static);
        static TEnum _ParseEnumThunkReader<TEnum>(string asStr, ref ThunkReader reader)
            where TEnum : struct
        {
            TEnum ret;
            if (!TryParseEnum<TEnum>(asStr, out ret))
            {
                throw new DeserializationException("Unexpected value for " + typeof(TEnum).Name + ": " + asStr, ref reader, false);
            }

            return ret;
        }

        static readonly MethodInfo ReadFlagsEnumThunkReader = typeof(Methods).GetMethod("_ReadFlagsEnumThunkReader", BindingFlags.NonPublic | BindingFlags.Static);
        static TEnum _ReadFlagsEnumThunkReader<TEnum>(ref ThunkReader reader, ref StringBuilder commonSb)
            where TEnum : struct
        {
            commonSb = commonSb ?? new StringBuilder();

            var ret = default(TEnum);

            while (true)
            {
                var c = _ReadEncodedCharThunkReader(ref reader);

                // ignore this *particular* whitespace
                if (c != ' ')
                {
                    // comma delimited
                    if (c == ',' || c == '"')
                    {
                        var asStr = commonSb.ToString();
                        TEnum parsed;
                        if (!TryParseEnum<TEnum>(asStr, out parsed))
                        {
                            throw new DeserializationException("Expected " + typeof(TEnum).Name + ", found: " + asStr, ref reader, false);
                        }

                        ret = FlagsEnumCombiner<TEnum>.Combine(ret, parsed);

                        commonSb.Clear();

                        if (c == '"') break;

                        continue;
                    }
                    commonSb.Append(c);
                }
            }

            // reset before returning
            commonSb.Clear();

            return ret;
        }

        static readonly MethodInfo ReadSkipWhitespaceThunkReader = typeof(Methods).GetMethod("_ReadSkipWhitespaceThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int _ReadSkipWhitespaceThunkReader(ref ThunkReader reader)
        {
            int c;
            do { c = reader.Read(); }
            while (IsWhiteSpace(c));
            return c;
        }

        static readonly MethodInfo SkipThunkReader = typeof(Methods).GetMethod("_SkipThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _SkipThunkReader(ref ThunkReader reader)
        {
            SkipWithLeadCharThunkReader(ref reader, reader.Read());
        }

        static void SkipWithLeadCharThunkReader(ref ThunkReader reader, int leadChar)
        {
            // skip null
            if (leadChar == 'n')
            {
                var c = reader.Read();
                if (c != 'u') throw new DeserializationException("Expected u", ref reader, c == -1);

                c = reader.Read();
                if (c != 'l') throw new DeserializationException("Expected l", ref reader, c == -1);

                c = reader.Read();
                if (c != 'l') throw new DeserializationException("Expected l", ref reader, c == -1);
                return;
            }

            // skip a string
            if (leadChar == '"')
            {
                SkipEncodedStringWithLeadCharThunkReader(ref reader, leadChar);
                return;
            }

            // skip an object
            if (leadChar == '{')
            {
                SkipObjectThunkReader(ref reader, leadChar);
                return;
            }

            // skip a list
            if (leadChar == '[')
            {
                SkipListThunkReader(ref reader, leadChar);
                return;
            }

            // skip a number
            if ((leadChar >= '0' && leadChar <= '9') || leadChar == '-')
            {
                SkipNumberThunkReader(ref reader, leadChar);
                return;
            }

            // skip false
            if (leadChar == 'f')
            {
                var c = reader.Read();
                if (c != 'a') throw new DeserializationException("Expected a", ref reader, c == -1);

                c = reader.Read();
                if (c != 'l') throw new DeserializationException("Expected l", ref reader, c == -1);

                c = reader.Read();
                if (c != 's') throw new DeserializationException("Expected s", ref reader, c == -1);

                c = reader.Read();
                if (c != 'e') throw new DeserializationException("Expected e", ref reader, c == -1);
                return;
            }

            // skip true
            if (leadChar == 't')
            {
                var c = reader.Read();
                if (c != 'r') throw new DeserializationException("Expected r", ref reader, c == -1);

                c = reader.Read();
                if (c != 'u') throw new DeserializationException("Expected u", ref reader, c == -1);

                c = reader.Read();
                if (c != 'e') throw new DeserializationException("Expected e", ref reader, c == -1);
                return;
            }

            throw new DeserializationException("Expected digit, -, \", {, n, t, f, or [", ref reader, leadChar == -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SkipEncodedStringWithLeadCharThunkReader(ref ThunkReader reader, int leadChar)
        {
            if (leadChar != '"') throw new DeserializationException("Expected \"", ref reader, leadChar == -1);

            while (true)
            {
                var first = reader.Read();
                if (first == -1) throw new DeserializationException("Expected any character", ref reader, true);

                // we didn't have to use anything but the buffer, make a string and return it!
                if (first == '"')
                {
                    return;
                }

                if (first != '\\')
                {
                    continue;
                }

                var second = reader.Read();
                if (second == -1) throw new DeserializationException("Expected any character", ref reader, true);

                switch (second)
                {
                    case '"': continue;
                    case '\\': continue;
                    case '/': continue;
                    case 'b': continue;
                    case 'f': continue;
                    case 'n': continue;
                    case 'r': continue;
                    case 't': continue;
                }

                if (second != 'u') throw new DeserializationException("Unrecognized escape sequence", ref reader, false);

                // now we're in an escape sequence, we expect 4 hex #s; always
                var u = reader.Read();
                if (!((u >= '0' && u <= '9') || (u >= 'A' && u <= 'F') || (u >= 'a' && u <= 'f'))) throw new DeserializationException("Expected hex digit", ref reader, u == -1);
                u = reader.Read();
                if (!((u >= '0' && u <= '9') || (u >= 'A' && u <= 'F') || (u >= 'a' && u <= 'f'))) throw new DeserializationException("Expected hex digit", ref reader, u == -1);
                u = reader.Read();
                if (!((u >= '0' && u <= '9') || (u >= 'A' && u <= 'F') || (u >= 'a' && u <= 'f'))) throw new DeserializationException("Expected hex digit", ref reader, u == -1);
                u = reader.Read();
                if (!((u >= '0' && u <= '9') || (u >= 'A' && u <= 'F') || (u >= 'a' && u <= 'f'))) throw new DeserializationException("Expected hex digit", ref reader, u == -1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SkipObjectThunkReader(ref ThunkReader reader, int leadChar)
        {
            if (leadChar != '{') throw new DeserializationException("Expected {", ref reader, leadChar == -1);

            int c;

            c = _ReadSkipWhitespaceThunkReader(ref reader);
            if (c == '}')
            {
                return;
            }
            SkipEncodedStringWithLeadCharThunkReader(ref reader, c);
            c = _ReadSkipWhitespaceThunkReader(ref reader);
            if (c != ':') throw new DeserializationException("Expected :", ref reader, c == -1);
            c = _ReadSkipWhitespaceThunkReader(ref reader);
            SkipWithLeadCharThunkReader(ref reader, c);

            while (true)
            {
                c = _ReadSkipWhitespaceThunkReader(ref reader);
                if (c == '}') return;
                if (c != ',') throw new DeserializationException("Expected ,", ref reader, c == -1);

                c = _ReadSkipWhitespaceThunkReader(ref reader);
                SkipEncodedStringWithLeadCharThunkReader(ref reader, c);
                c = _ReadSkipWhitespaceThunkReader(ref reader);
                if (c != ':') throw new DeserializationException("Expected :", ref reader, c == -1);
                c = _ReadSkipWhitespaceThunkReader(ref reader);
                SkipWithLeadCharThunkReader(ref reader, c);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SkipListThunkReader(ref ThunkReader reader, int leadChar)
        {
            if (leadChar != '[') throw new DeserializationException("Expected [", ref reader, leadChar == -1);

            int c;

            c = _ReadSkipWhitespaceThunkReader(ref reader);
            if (c == ']')
            {
                return;
            }
            SkipWithLeadCharThunkReader(ref reader, c);

            while (true)
            {
                c = _ReadSkipWhitespaceThunkReader(ref reader);
                if (c == ']') return;
                if (c != ',') throw new DeserializationException("Expected ], or ,", ref reader, c == -1);
                c = _ReadSkipWhitespaceThunkReader(ref reader);
                SkipWithLeadCharThunkReader(ref reader, c);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SkipNumberThunkReader(ref ThunkReader reader, int leadChar)
        {
            // leadChar should be a start of the number

            var seenDecimal = false;
            var seenExponent = false;

            while (true)
            {
                var c = reader.Peek();
                if (c == -1) throw new DeserializationException("Unexpected end of reader", ref reader, true);

                if (c >= '0' && c <= '9')
                {
                    reader.Read();  // skip the digit
                    continue;
                }

                if (c == '.' && !seenDecimal)
                {
                    reader.Read();      // skip the decimal
                    seenDecimal = true;
                    continue;
                }

                if ((c == 'e' || c == 'E') && !seenExponent)
                {
                    reader.Read();      // skip the decimal
                    seenExponent = true;
                    seenDecimal = true;

                    var next = reader.Peek();
                    if (next == '-' || next == '+' || (next >= '0' && next <= '9'))
                    {
                        reader.Read();
                        continue;
                    }

                    throw new DeserializationException("Expected -, or a digit", ref reader, false);
                }

                return;
            }
        }

        static MethodInfo SkipEncodedStringThunkReader = typeof(Methods).GetMethod("_SkipEncodedStringThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _SkipEncodedStringThunkReader(ref ThunkReader reader)
        {
            SkipEncodedStringWithLeadCharThunkReader(ref reader, reader.Read());
        }

        // Note: This same method, but then for TextReader, is in Methods.ReadNumbers.cs; it's called "_DiscardMicrosoftTimeZoneOffset".
        static readonly MethodInfo DiscardMicrosoftTimeZoneOffsetThunkReader = typeof(Methods).GetMethod("_DiscardMicrosoftTimeZoneOffsetThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _DiscardMicrosoftTimeZoneOffsetThunkReader(ref ThunkReader reader)
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
            if (c == -1) throw new DeserializationException("Unexpected end of reader", ref reader, true);
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", ref reader, false);
            temp += c;

            // second digit hour
            temp *= 10;
            c = reader.Read();
            if (c == -1) throw new DeserializationException("Unexpected end of reader", ref reader, true);
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", ref reader, false);
            temp += c;

            if (temp > 23) throw new DeserializationException("Expected hour portion of timezone offset between 0 and 24", ref reader, false);

            temp = 0;
            // first digit minute
            temp *= 10;
            c = reader.Read();
            if (c == -1) throw new DeserializationException("Unexpected end of reader", ref reader, true);
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", ref reader, false);
            temp += c;

            // second digit minute
            temp *= 10;
            c = reader.Read();
            if (c == -1) throw new DeserializationException("Unexpected end of reader", ref reader, true);
            c = c - '0';
            if (c < 0 || c > 9) throw new DeserializationException("Expected digit", ref reader, false);
            temp += c;

            if (temp > 59) throw new DeserializationException("Expected minute portion of timezone offset between 0 and 59", ref reader, false);
        }

        static readonly MethodInfo ReadISO8601DateWithCharArrayThunkReader = typeof(Methods).GetMethod("_ReadISO8601DateWithCharArrayThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DateTime _ReadISO8601DateWithCharArrayThunkReader(ref ThunkReader reader, ref char[] buffer)
        {
            InitDynamicBuffer(ref buffer);
            return _ReadISO8601DateThunkReader(ref reader, buffer);
        }

        static readonly MethodInfo ReadISO8601DateThunkReader = typeof(Methods).GetMethod("_ReadISO8601DateThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DateTime _ReadISO8601DateThunkReader(ref ThunkReader reader, char[] buffer)
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
                if (c == -1) throw new DeserializationException("Unexpected end of stream while parsing ISO8601 date", ref reader, true);

                if (c == '"') break;

                // actually consume that character
                reader.Read();

                ix++;
                if (ix == CharBufferSize) throw new DeserializationException("ISO8601 date is too long, expected " + CharBufferSize + " characters or less", ref reader, false);
                buffer[ix] = (char)c;

                // RFC3339 allows lowercase t and spaces as alternatives to ISO8601's T
                if (c == 'T' || c == 't' || c == ' ')
                {
                    if (tPos.HasValue) throw new DeserializationException("Unexpected second T in ISO8601 date", ref reader, false);
                    tPos = ix - 1;
                }

                if (tPos.HasValue)
                {
                    // RFC3339 allows lowercase z as alternatives to ISO8601's Z
                    if (c == 'Z' || c == 'z' || c == '+' || c == '-')
                    {
                        if (zPlusOrMinus.HasValue) throw new DeserializationException("Unexpected second Z, +, or - in ISO8601 date", ref reader, false);
                        zPlusOrMinus = ix - 1;
                    }
                }
            }
            
            var date = ParseISO8601DateThunkReader(ref reader, buffer, 0, tPos ?? ix); // this is in *LOCAL TIME* because that's what the spec says
            if (!tPos.HasValue)
            {
                return date;
            }

            var time = ParseISO8601TimeThunkReader(ref reader, buffer, tPos.Value + 2, zPlusOrMinus ?? ix);
            if (!zPlusOrMinus.HasValue)
            {
                try
                {
                    return date + time;
                }
                catch (Exception e)
                {
                    throw new DeserializationException("ISO8601 date with time could not be represented as a DateTime", ref reader, e, false);
                }
            }

            bool unknownLocalOffset;
            // only +1 here because the separator is significant (oy vey)
            var timezoneOffset = ParseISO8601TimeZoneOffsetThunkReader(ref reader, buffer, zPlusOrMinus.Value + 1, ix, out unknownLocalOffset);

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
                throw new DeserializationException("ISO8601 date with time and timezone offset could not be represented as a DateTime", ref reader, e, false);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DateTime ParseISO8601DateThunkReader(ref ThunkReader reader, char[] buffer, int start, int stop)
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
            if (len < 4) throw new DeserializationException("ISO8601 date must begin with a 4 character year", ref reader, false);

            var year = 0;
            var month = 0;
            var day = 0;
            int c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
            year += (c - '0');
            year *= 10;
            start++;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
            year += (c - '0');
            year *= 10;
            start++;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
            year += (c - '0');
            year *= 10;
            start++;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
            year += (c - '0');

            if (year == 0) throw new DeserializationException("ISO8601 year 0000 cannot be converted to a DateTime", ref reader, false);

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
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                            week += (c - '0');
                            week *= 10;
                            start++;
                            c = buffer[start];
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                            week += (c - '0');
                            if (week == 0 || week > 53) throw new DeserializationException("Expected week to be between 01 and 53", ref reader, false);

                            return ConvertWeekDateToDateTime(year, week, 1);

                        case 10:
                            c = buffer[start];
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                            week += (c - '0');
                            week *= 10;
                            start++;
                            c = buffer[start];
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                            week += (c - '0');
                            if (week == 0 || week > 53) throw new DeserializationException("Expected week to be between 01 and 53", ref reader, false);
                            start++;

                            c = buffer[start];
                            if (c != '-') throw new DeserializationException("Expected -", ref reader, false);
                            start++;

                            c = buffer[start];
                            if (c < '1' || c > '7') throw new DeserializationException("Expected day to be a digit between 1 and 7", ref reader, false);
                            day = (c - '0');

                            return ConvertWeekDateToDateTime(year, week, day);

                        default:
                            throw new DeserializationException("Unexpected date string length", ref reader, false);
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
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                            week += (c - '0');
                            week *= 10;
                            start++;
                            c = buffer[start];
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                            week += (c - '0');
                            if (week == 0 || week > 53) throw new DeserializationException("Expected week to be between 01 and 53", ref reader, false);

                            return ConvertWeekDateToDateTime(year, week, 1);

                        case 8:
                            c = buffer[start];
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                            week += (c - '0');
                            week *= 10;
                            start++;
                            c = buffer[start];
                            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                            week += (c - '0');
                            if (week == 0 || week > 53) throw new DeserializationException("Expected week to be between 01 and 53", ref reader, false);
                            start++;

                            c = buffer[start];
                            if (c < '1' || c > '7') throw new DeserializationException("Expected day to be a digit between 1 and 7", ref reader, false);
                            day = (c - '0');

                            return ConvertWeekDateToDateTime(year, week, day);

                        default:
                            throw new DeserializationException("Unexpected date string length", ref reader, false);
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
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                        month += (c - '0');
                        month *= 10;
                        start++;
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                        month += (c - '0');
                        if (month == 0 || month > 12) throw new DeserializationException("Expected month to be between 01 and 12", ref reader, false);

                        // year is [1,9999] and month is [1,12] for sure, no need to handle errors
                        return new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local);

                    case 8:
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                        day += (c - '0');
                        day *= 10;
                        start++;
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                        day += (c - '0');
                        day *= 10;
                        start++;
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref  reader, false);
                        day += (c - '0');
                        if (day == 0 || day > 366) throw new DeserializationException("Expected ordinal day to be between 001 and 366", ref reader, false);

                        if (day == 366)
                        {
                            var isLeapYear = (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0));

                            if (!isLeapYear) throw new DeserializationException("Ordinal day can only be 366 in a leap year", ref reader, false);
                        }

                        // year is [1,9999] and day is [1,366], no need to handle errors
                        return (new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Local)).AddDays(day - 1);

                    case 10:
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                        month += (c - '0');
                        month *= 10;
                        start++;
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                        month += (c - '0');
                        if (month == 0 || month > 12) throw new DeserializationException("Expected month to be between 01 and 12", ref reader, false);
                        start++;

                        if (buffer[start] != '-') throw new DeserializationException("Expected -", ref reader, false);
                        start++;

                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                        day += (c - '0');
                        day *= 10;
                        start++;
                        c = buffer[start];
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                        day += (c - '0');
                        if (day == 0 || day > 31) throw new DeserializationException("Expected day to be between 01 and 31", ref reader, false);
                        start++;

                        try
                        {
                            return (new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local));
                        }
                        catch (ArgumentOutOfRangeException e)
                        {
                            throw new DeserializationException("ISO8601 date could not be mapped to DateTime", ref reader, e, false);
                        }

                    default:
                        throw new DeserializationException("Unexpected date string length", ref reader, false);
                }
            }

            // Could still be
            // YYYYDDD          length: 7
            // YYYYMMDD         length: 8

            switch (len)
            {
                case 7:
                    c = buffer[start];
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                    day += (c - '0');
                    day *= 10;
                    start++;
                    c = buffer[start];
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                    day += (c - '0');
                    day *= 10;
                    start++;
                    c = buffer[start];
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                    day += (c - '0');
                    if (day == 0 || day > 366) throw new DeserializationException("Expected ordinal day to be between 001 and 366", ref reader, false);
                    start++;

                    if (day == 366)
                    {
                        var isLeapYear = (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0));

                        if (!isLeapYear) throw new DeserializationException("Ordinal day can only be 366 in a leap year", ref reader, false);
                    }

                    // year is [1,9999] and day is [1,366], no need to handle errors
                    return (new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Local)).AddDays(day - 1);

                case 8:
                    c = buffer[start];
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                    month += (c - '0');
                    month *= 10;
                    start++;
                    c = buffer[start];
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                    month += (c - '0');
                    if (month == 0 || month > 12) throw new DeserializationException("Expected month to be between 01 and 12", ref reader, false);
                    start++;

                    c = buffer[start];
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                    day += (c - '0');
                    day *= 10;
                    start++;
                    c = buffer[start];
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                    day += (c - '0');
                    if (day == 0 || day > 31) throw new DeserializationException("Expected day to be between 01 and 31", ref reader, false);
                    start++;

                    try
                    {
                        return (new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local));
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        throw new DeserializationException("ISO8601 date could not be mapped to DateTime", ref reader, e, false);
                    }

                default:
                    throw new DeserializationException("Unexpected date string length", ref reader, false);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static TimeSpan ParseISO8601TimeThunkReader(ref ThunkReader reader, char[] buffer, int start, int stop)
        {
            const long HoursToTicks = 36000000000;
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
            if (len < 2) throw new DeserializationException("ISO8601 time must begin with a 2 character hour", ref reader, false);

            var hour = 0;
            int c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
            hour += (c - '0');
            hour *= 10;
            start++;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
            hour += (c - '0');
            if (hour > 24) throw new DeserializationException("Expected hour to be between 00 and 24", ref reader, false);

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
                    if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);

                    // Max precision of TimeSpan.FromTicks
                    if (fracLength < 9) 
                    {
                        frac *= 10;
                        frac += (c - '0');
                        fracLength++;                    
                    }

                    start++;
                }

                if (fracLength == 0) throw new DeserializationException("Expected fractional part of ISO8601 time", ref reader, false);

                long hoursAsTicks = hour * HoursToTicks;
                hoursAsTicks += frac * 36 * Utils.Pow10(9 - fracLength);

                return TimeSpan.FromTicks(hoursAsTicks);
            }

            if (c == ':')
            {
                if (hasSeparators.HasValue && !hasSeparators.Value) throw new DeserializationException("Unexpected separator", ref reader, false);

                hasSeparators = true;
                start++;
            }
            else
            {
                if (hasSeparators.HasValue && hasSeparators.Value) throw new DeserializationException("Expected :", ref reader, false);

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

                if (len < 4) throw new DeserializationException("Expected minute part of ISO8601 time", ref reader, false);

                var min = 0;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                min += (c - '0');
                min *= 10;
                start++;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                min += (c - '0');
                if (min > 59) throw new DeserializationException("Expected minute to be between 00 and 59", ref reader, false);

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
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);

                        // Max precision of TimeSpan.FromTicks
                        if (fracLength < 8) 
                        {
                            frac *= 10;
                            frac += (c - '0');
                            fracLength++;                        
                        }

                        start++;
                    }

                    if (fracLength == 0) throw new DeserializationException("Expected fractional part of ISO8601 time", ref reader, false);

                    long hoursAsTicks = hour * HoursToTicks;
                    long minsAsTicks = min * MinutesToTicks;
                    minsAsTicks += frac * 6 * Utils.Pow10(8 - fracLength);

                    return TimeSpan.FromTicks(hoursAsTicks + minsAsTicks);
                }

                if (c != ':') throw new DeserializationException("Expected :", ref reader, false);
                start++;

                var secs = 0;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                secs += (c - '0');
                secs *= 10;
                start++;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
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
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);

                        // Max precision of TimeSpan.FromTicks
                        if (fracLength < 7) 
                        {
                            frac *= 10;
                            frac += (c - '0');
                            fracLength++;
                        }

                        start++;
                    }

                    if (fracLength == 0) throw new DeserializationException("Expected fractional part of ISO8601 time", ref reader, false);

                    long hoursAsTicks = hour * HoursToTicks;
                    long minsAsTicks = min * MinutesToTicks;
                    long secsAsTicks = secs * SecondsToTicks;
                    secsAsTicks += frac * Utils.Pow10(7 - fracLength);

                    return TimeSpan.FromTicks(hoursAsTicks + minsAsTicks + secsAsTicks);
                }

                throw new DeserializationException("Expected ,, or .", ref reader, false);
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

                if (len < 4) throw new DeserializationException("Expected minute part of ISO8601 time", ref reader, false);

                var min = 0;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                min += (c - '0');
                min *= 10;
                start++;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                min += (c - '0');
                if (min > 59) throw new DeserializationException("Expected minute to be between 00 and 59", ref reader, false);

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
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);

                        // Max precision of TimeSpan.FromTicks
                        if (fracLength < 8) 
                        {
                            frac *= 10;
                            frac += (c - '0');
                            fracLength++;
                        }

                        start++;
                    }

                    if (fracLength == 0) throw new DeserializationException("Expected fractional part of ISO8601 time", ref reader, false);

                    long hoursAsTicks = hour * HoursToTicks;
                    long minsAsTicks = min * MinutesToTicks;
                    minsAsTicks += frac * 6 * Utils.Pow10(8 - fracLength);

                    return TimeSpan.FromTicks(hoursAsTicks + minsAsTicks);
                }

                if (c == ':') throw new DeserializationException("Unexpected separator in ISO8601 time", ref reader, false);

                var secs = 0;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
                secs += (c - '0');
                secs *= 10;
                start++;
                c = buffer[start];
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
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
                        if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);

                        // Max precision of TimeSpan.FromTicks
                        if (fracLength < 7) 
                        {
                            frac *= 10;
                            frac += (c - '0');
                            fracLength++;
                        }
                        
                        start++;
                    }

                    if (fracLength == 0) throw new DeserializationException("Expected fractional part of ISO8601 time", ref reader, false);

                    long hoursAsTicks = hour * HoursToTicks;
                    long minsAsTicks = min * MinutesToTicks;
                    long secsAsTicks = secs * SecondsToTicks;
                    secsAsTicks += frac * Utils.Pow10(7 - fracLength);

                    return TimeSpan.FromTicks(hoursAsTicks + minsAsTicks + secsAsTicks);
                }

                throw new DeserializationException("Expected ,, or .", ref reader, false);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static TimeSpan ParseISO8601TimeZoneOffsetThunkReader(ref ThunkReader reader, char[] buffer, int start, int stop, out bool unknownLocalOffset)
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

            if (len < 2) throw new DeserializationException("Expected hour part of ISO8601 timezone offset", ref reader, false);
            var hour = 0;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
            hour += (c - '0');
            hour *= 10;
            start++;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
            hour += (c - '0');
            if (hour > 24) throw new DeserializationException("Expected hour offset to be between 00 and 24", ref reader, false);

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
                if (hasSeparators.HasValue && !hasSeparators.Value) throw new DeserializationException("Unexpected separator in ISO8601 timezone offset", ref reader, false);

                hasSeparators = true;
                start++;
            }
            else
            {
                if (hasSeparators.HasValue && hasSeparators.Value) throw new DeserializationException("Expected :", ref reader, false);

                hasSeparators = false;
            }

            if (stop - start + 1 < 2) throw new DeserializationException("Not enough character for ISO8601 timezone offset", ref reader, false);

            var mins = 0;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
            mins += (c - '0');
            mins *= 10;
            start++;
            c = buffer[start];
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, false);
            mins += (c - '0');
            if (mins > 59) throw new DeserializationException("Expected minute offset to be between 00 and 59", ref reader, false);

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

        static readonly MethodInfo ReadRFC1123DateThunkReader = typeof(Methods).GetMethod("_ReadRFC1123DateThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DateTime _ReadRFC1123DateThunkReader(ref ThunkReader reader)
        {
            // ddd, dd MMM yyyy HH:mm:ss GMT'"

            var dayOfWeek = ReadRFC1123DayOfWeekThunkReader(ref reader);

            var c = reader.Read();
            if (c != ',') throw new DeserializationException("Expected ,", ref reader, c == -1);

            c = reader.Read();
            if (c != ' ') throw new DeserializationException("Expected [space]", ref reader, c == -1);

            var day = 0;
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            day += (c - '0');
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            day *= 10;
            day += (c - '0');

            c = reader.Read();
            if (c != ' ') throw new DeserializationException("Expected [space]", ref reader, c == -1);

            var month = ReadRFC1123MonthThunkReader(ref reader);

            c = reader.Read();
            if (c != ' ') throw new DeserializationException("Expected [space]", ref reader, c == -1);

            var year = 0;
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            year += (c - '0');
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            year *= 10;
            year += (c - '0');
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            year *= 10;
            year += (c - '0');
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            year *= 10;
            year += (c - '0');

            c = reader.Read();
            if (c != ' ') throw new DeserializationException("Expected [space]", ref reader, c == -1);

            var hour = 0;
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            hour += (c - '0');
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            hour *= 10;
            hour += (c - '0');

            c = reader.Read();
            if (c != ':') throw new DeserializationException("Expected :", ref reader, c == -1);

            var min = 0;
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            min += (c - '0');
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            min *= 10;
            min += (c - '0');

            c = reader.Read();
            if (c != ':') throw new DeserializationException("Expected :", ref reader, c == -1);

            var sec = 0;
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            sec += (c - '0');
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            sec *= 10;
            sec += (c - '0');

            c = reader.Read();
            if (c != ' ') throw new DeserializationException("Expected [space]", ref reader, c == -1);

            c = reader.Read();
            if (c != 'G') throw new DeserializationException("Expected G", ref reader, c == -1);
            c = reader.Read();
            if (c != 'M') throw new DeserializationException("Expected M", ref reader, c == -1);
            c = reader.Read();
            if (c != 'T') throw new DeserializationException("Expected T", ref reader, c == -1);

            var ret = new DateTime(year, month, day, hour, min, sec, DateTimeKind.Utc);

            if (ret.DayOfWeek != (DayOfWeek)dayOfWeek)
            {
                throw new DeserializationException("RFC1123 DateTime claimed to be [" + (DayOfWeek)dayOfWeek + "], but really was [" + ret.DayOfWeek + "]", ref reader, false);
            }

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte ReadRFC1123MonthThunkReader(ref ThunkReader reader)
        {
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected month, instead stream ended", ref reader, true);

            // Jan | Jun | Jul
            if (c == 'J')
            {
                c = reader.Read();
                if (c == 'a')
                {
                    c = reader.Read();
                    if (c != 'n') throw new DeserializationException("Expected n", ref reader, c == -1);

                    return 1;
                }

                if (c != 'u') throw new DeserializationException("Expected u", ref reader, c == -1);

                c = reader.Read();
                if (c == 'n') return 6;
                if (c == 'l') return 7;

                throw new DeserializationException("Expected n, or l", ref reader, c == -1);
            }

            // Feb
            if (c == 'F')
            {
                c = reader.Read();
                if (c != 'e') throw new DeserializationException("Expected e", ref reader, c == -1);
                c = reader.Read();
                if (c != 'b') throw new DeserializationException("Expected b", ref reader, c == -1);

                return 2;
            }

            // Mar | May
            if (c == 'M')
            {
                c = reader.Read();
                if (c != 'a') throw new DeserializationException("Expected a", ref reader, c == -1);

                c = reader.Read();
                if (c == 'r') return 3;
                if (c == 'y') return 5;

                throw new DeserializationException("Expected r, or y", ref reader, c == -1);
            }

            // Apr | Aug
            if (c == 'A')
            {
                c = reader.Read();
                if (c == 'p')
                {
                    c = reader.Read();
                    if (c != 'r') throw new DeserializationException("Expected r", ref reader, c == -1);

                    return 4;
                }

                if (c == 'u')
                {
                    c = reader.Read();
                    if (c != 'g') throw new DeserializationException("Expected g", ref reader, c == -1);

                    return 8;
                }

                throw new DeserializationException("Expected p, or u", ref reader, c == -1);
            }

            // Sep
            if (c == 'S')
            {
                c = reader.Read();
                if (c != 'e') throw new DeserializationException("Expected e", ref reader, c == -1);
                c = reader.Read();
                if (c != 'p') throw new DeserializationException("Expected p", ref reader, c == -1);

                return 9;
            }

            // Oct
            if (c == 'O')
            {
                c = reader.Read();
                if (c != 'c') throw new DeserializationException("Expected c", ref reader, c == -1);
                c = reader.Read();
                if (c != 't') throw new DeserializationException("Expected t", ref reader, c == -1);

                return 10;
            }

            // Nov
            if (c == 'N')
            {
                c = reader.Read();
                if (c != 'o') throw new DeserializationException("Expected o", ref reader, c == -1);
                c = reader.Read();
                if (c != 'v') throw new DeserializationException("Expected v", ref reader, c == -1);

                return 11;
            }

            // Dec
            if (c == 'D')
            {
                c = reader.Read();
                if (c != 'e') throw new DeserializationException("Expected e", ref reader, c == -1);
                c = reader.Read();
                if (c != 'c') throw new DeserializationException("Expected c", ref reader, c == -1);

                return 12;
            }

            throw new DeserializationException("Expected J, F, M, A, S, O, N, or D", ref reader, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DayOfWeek ReadRFC1123DayOfWeekThunkReader(ref ThunkReader reader)
        {
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected day of week, instead stream ended", ref reader, true);

            // Mon
            if (c == 'M')
            {
                c = reader.Read();
                if (c != 'o') throw new DeserializationException("Expected o", ref reader, c == -1);

                c = reader.Read();
                if (c != 'n') throw new DeserializationException("Expected n", ref reader, c == -1);

                return DayOfWeek.Monday;
            }

            // Tue | Thu
            if (c == 'T')
            {
                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected h or u; instead stream ended", ref reader, true);

                if (c == 'u')
                {
                    c = reader.Read();
                    if (c != 'e') throw new DeserializationException("Expected e", ref reader, c == -1);

                    return DayOfWeek.Tuesday;
                }

                if (c == 'h')
                {
                    c = reader.Read();
                    if (c != 'u') throw new DeserializationException("Expected u", ref reader, c == -1);

                    return DayOfWeek.Thursday;
                }

                throw new DeserializationException("Expected h or u", ref reader, false);
            }

            // Wed
            if (c == 'W')
            {
                c = reader.Read();
                if (c != 'e') throw new DeserializationException("Expected e", ref reader, c == -1);

                c = reader.Read();
                if (c != 'd') throw new DeserializationException("Expected d", ref reader, c == -1);

                return DayOfWeek.Wednesday;
            }

            // Fri
            if (c == 'F')
            {
                c = reader.Read();
                if (c != 'r') throw new DeserializationException("Expected r", ref reader, c == -1);

                c = reader.Read();
                if (c != 'i') throw new DeserializationException("Expected i", ref reader, c == -1);

                return DayOfWeek.Friday;
            }

            // Sat | Sun
            if (c == 'S')
            {
                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected a or u; instead stream ended", ref reader, true);

                if (c == 'a')
                {
                    c = reader.Read();
                    if (c != 't') throw new DeserializationException("Expected t", ref reader, c == -1);

                    return DayOfWeek.Saturday;
                }

                if (c == 'u')
                {
                    c = reader.Read();
                    if (c != 'n') throw new DeserializationException("Expected n", ref reader, c == -1);

                    return DayOfWeek.Sunday;
                }

                throw new DeserializationException("Expected a or u", ref reader, false);
            }

            throw new DeserializationException("Expected M, T, W, F, or S", ref reader, false);
        }

        static readonly MethodInfo ReadISO8601DateWithOffsetWithCharArrayThunkReader = typeof(Methods).GetMethod("_ReadISO8601DateWithOffsetWithCharArrayThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DateTimeOffset _ReadISO8601DateWithOffsetWithCharArrayThunkReader(ref ThunkReader reader, ref char[] buffer)
        {
            InitDynamicBuffer(ref buffer);
            return _ReadISO8601DateWithOffsetThunkReader(ref reader, buffer);
        }

        static readonly MethodInfo ReadISO8601DateWithOffsetThunkReader = typeof(Methods).GetMethod("_ReadISO8601DateWithOffsetThunkReader", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DateTimeOffset _ReadISO8601DateWithOffsetThunkReader(ref ThunkReader reader, char[] buffer)
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
                if (c == -1) throw new DeserializationException("Unexpected end of stream while parsing ISO8601 date", ref reader, true);

                if (c == '"') break;

                // actually consume that character
                reader.Read();

                ix++;
                if (ix == CharBufferSize) throw new DeserializationException("ISO8601 date is too long, expected " + CharBufferSize + " characters or less", ref reader, false);
                buffer[ix] = (char)c;

                // RFC3339 allows lowercase t and spaces as alternatives to ISO8601's T
                if (c == 'T' || c == 't' || c == ' ')
                {
                    if (tPos.HasValue) throw new DeserializationException("Unexpected second T in ISO8601 date", ref reader, false);
                    tPos = ix - 1;
                }

                if (tPos.HasValue)
                {
                    // RFC3339 allows lowercase z as alternatives to ISO8601's Z
                    if (c == 'Z' || c == 'z' || c == '+' || c == '-')
                    {
                        if (zPlusOrMinus.HasValue) throw new DeserializationException("Unexpected second Z, +, or - in ISO8601 date", ref reader, false);
                        zPlusOrMinus = ix - 1;
                    }
                }
            }
            
            var date = ParseISO8601DateThunkReader(ref reader, buffer, 0, tPos ?? ix); // this is in *LOCAL TIME* because that's what the spec says
            if (!tPos.HasValue)
            {
                return date;
            }

            var time = ParseISO8601TimeThunkReader(ref reader, buffer, tPos.Value + 2, zPlusOrMinus ?? ix);
            if (!zPlusOrMinus.HasValue)
            {
                try
                {
                    return date + time;
                }
                catch (Exception e)
                {
                    throw new DeserializationException("ISO8601 date with time could not be represented as a DateTime", ref reader, e, false);
                }
            }

            bool unknownLocalOffset;
            // only +1 here because the separator is significant (oy vey)
            var timezoneOffset = ParseISO8601TimeZoneOffsetThunkReader(ref reader, buffer, zPlusOrMinus.Value + 1, ix, out unknownLocalOffset);

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
                throw new DeserializationException("ISO8601 date with time and timezone offset could not be represented as a DateTime", ref reader, e, false);
            }
        }

        static readonly MethodInfo ReadMicrosoftTimeSpanThunkReader = typeof(Methods).GetMethod("_ReadMicrosoftTimeSpanThunkReader", BindingFlags.NonPublic | BindingFlags.Static);
        static TimeSpan _ReadMicrosoftTimeSpanThunkReader(ref ThunkReader reader, char[] buffer)
        {
            var strLen = ReadTimeSpanIntoThunkReader(ref reader, buffer);

            if (strLen == 0)
            {
                throw new DeserializationException("Unexpected empty string", ref reader, false);
            }

            int days, hours, minutes, seconds, fraction;
            days = hours = minutes = seconds = fraction = 0;

            bool isNegative, pastDays, pastHours, pastMinutes, pastSeconds;
            isNegative = pastDays = pastHours = pastMinutes = pastSeconds = false;

            var ixOfLastPeriod = -1;
            var part = 0;

            int i;

            if (buffer[0] == '-')
            {
                isNegative = true;
                i = 1;
            }
            else
            {
                i = 0;
            }

            for (; i < strLen; i++)
            {
                var c = buffer[i];
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

                    throw new DeserializationException("Unexpected character .", ref reader, false);
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

                    throw new DeserializationException("Unexpected character :", ref reader, false);
                }

                if (c < '0' || c > '9')
                {
                    throw new DeserializationException("Expected digit, found " + c, ref reader, false);
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
                throw new DeserializationException("Missing required portion of TimeSpan", ref reader, false);
            }

            var msInt = 0;
            if (fraction != 0)
            {
                var sizeOfFraction = strLen - (ixOfLastPeriod + 1);

                if (sizeOfFraction > 7)
                {
                    throw new DeserializationException("Fractional part of TimeSpan too large", ref reader, false);
                }

                var fracOfSecond = part / DivideFractionBy[sizeOfFraction - 1];
                var ms = fracOfSecond * 1000.0;
                msInt = (int)ms;
            }

            var ret = new TimeSpan(days, hours, minutes, seconds, msInt);
            if (isNegative)
            {
                ret = ret.Negate();
            }

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int ReadTimeSpanIntoThunkReader(ref ThunkReader reader, char[] buffer)
        {
            var i = reader.Peek();
            if (i != '"') throw new DeserializationException("Expected \", found " + (char)i, ref reader, i == -1);

            reader.Read();  // skip the opening '"'

            var ix = 0;
            while (true)
            {
                if (ix >= CharBufferSize) throw new DeserializationException("ISO8601 duration too long", ref reader, false);

                i = reader.Read();
                if (i == -1) throw new DeserializationException("Unexpected end of stream", ref reader, true);
                if (i == '"')
                {
                    break;
                }

                buffer[ix] = (char)i;
                ix++;
            }

            return ix;
        }

        static readonly MethodInfo ReadISO8601TimeSpanThunkReader = typeof(Methods).GetMethod("_ReadISO8601TimeSpanThunkReader", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static TimeSpan _ReadISO8601TimeSpanThunkReader(ref ThunkReader reader, char[] str)
        {
            const ulong TicksPerDay = 864000000000;
            const ulong TicksPerWeek = TicksPerDay * 7;
            const ulong TicksPerMonth = TicksPerDay * 30;
            const ulong TicksPerYear = TicksPerDay * 365;

            // Format goes like so:
            // - (-)P(([n]Y)([n]M)([n]D))(T([n]H)([n]M)([n]S))
            // - P[n]W

            var len = ReadTimeSpanIntoThunkReader(ref reader, str);

            if (len == 0)
            {
                throw new DeserializationException("Unexpected empty string", ref reader, false);
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
                throw new DeserializationException("Expected P, instead TimeSpan string ended", ref reader, false);
            }

            c = str[ix];
            if (c != 'P')
            {
                throw new DeserializationException("Expected P, found " + c, ref reader, false);
            }

            ix++;   // skip 'P'

            long year, month, week, day;
            var hasTimePart = ISO8601TimeSpan_ReadDatePartThunkReader(ref reader, str, len, ref ix, out year, out month, out week, out day);

            if (week != -1 && (year != -1 || month != -1 || day != -1))
            {
                throw new DeserializationException("Week part of TimeSpan defined along with one or more of year, month, or day", ref reader, false);
            }

            if (week != -1 && hasTimePart)
            {
                throw new DeserializationException("TimeSpans with a week defined cannot also have a time defined", ref reader, false);
            }

            if (year == -1) year = 0;
            if (month == -1) month = 0;
            if (week == -1) week = 0;
            if (day == -1) day = 0;

            ulong timeTicks;

            if (hasTimePart)
            {
                ix++;   // skip 'T'
                ISO8601TimeSpan_ReadTimePartThunkReader(ref reader, str, len, ref ix, out timeTicks);
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
        static void ISO8601TimeSpan_ReadTimePartThunkReader(ref ThunkReader reader, char[] str, int strLen, ref int ix, out ulong ticks)
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
                    throw new DeserializationException("Expected Time part of TimeSpan to end", ref reader, false);
                }

                int whole, fraction, fracLen;
                var part = ISO8601TimeSpan_ReadPartThunkReader(ref reader, str, strLen, ref ix, out whole, out fraction, out fracLen);

                if (fracLen != 0)
                {
                    fracSeen = true;
                }

                if (part == 'H')
                {
                    if (hourSeen)
                    {
                        throw new DeserializationException("Hour part of TimeSpan seen twice", ref reader, false);
                    }

                    if (minutesSeen)
                    {
                        throw new DeserializationException("Hour part of TimeSpan seen after minutes already parsed", ref reader, false);
                    }

                    if (secondsSeen)
                    {
                        throw new DeserializationException("Hour part of TimeSpan seen after seconds already parsed", ref reader, false);
                    }

                    ticks += (ulong)whole * TicksPerHour + ISO8601TimeSpan_FractionToTicks(9, fraction * 36, fracLen);
                    hourSeen = true;
                    continue;
                }

                if (part == 'M')
                {
                    if (minutesSeen)
                    {
                        throw new DeserializationException("Minute part of TimeSpan seen twice", ref reader, false);
                    }

                    if (secondsSeen)
                    {
                        throw new DeserializationException("Minute part of TimeSpan seen after seconds already parsed", ref reader, false);
                    }

                    ticks += (ulong)whole * TicksPerMinute + ISO8601TimeSpan_FractionToTicks(8, fraction * 6, fracLen);
                    minutesSeen = true;
                    continue;
                }

                if (part == 'S')
                {
                    if (secondsSeen)
                    {
                        throw new DeserializationException("Seconds part of TimeSpan seen twice", ref reader, false);
                    }

                    ticks += (ulong)whole * TicksPerSecond + ISO8601TimeSpan_FractionToTicks(7, fraction, fracLen);
                    secondsSeen = true;
                    continue;
                }

                throw new DeserializationException("Expected H, M, or S but found: " + part, ref reader, false);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static char ISO8601TimeSpan_ReadPartThunkReader(ref ThunkReader reader, char[] str, int strLen, ref int ix, out int whole, out int fraction, out int fracLen)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ISO8601TimeSpan_ReadDatePartThunkReader(ref ThunkReader reader, char[] str, int strLen, ref int ix, out long year, out long month, out long week, out long day)
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
                var part = ISO8601TimeSpan_ReadPartThunkReader(ref reader, str, strLen, ref ix, out whole, out fraction, out fracLen);

                if (fracLen != 0)
                {
                    throw new DeserializationException("Fractional values are not supported in the year, month, day, or week parts of an ISO8601 TimeSpan", ref reader, false);
                }

                if (part == 'Y')
                {
                    if (yearSeen)
                    {
                        throw new DeserializationException("Year part of TimeSpan seen twice", ref reader, false);
                    }

                    if (monthSeen)
                    {
                        throw new DeserializationException("Year part of TimeSpan seen after month already parsed", ref reader, false);
                    }

                    if (daySeen)
                    {
                        throw new DeserializationException("Year part of TimeSpan seen after day already parsed", ref reader, false);
                    }

                    year = (long)whole;
                    yearSeen = true;
                    continue;
                }

                if (part == 'M')
                {
                    if (monthSeen)
                    {
                        throw new DeserializationException("Month part of TimeSpan seen twice", ref reader, false);
                    }

                    if (daySeen)
                    {
                        throw new DeserializationException("Month part of TimeSpan seen after day already parsed", ref reader, false);
                    }

                    month = (long)whole;
                    monthSeen = true;
                    continue;
                }

                if (part == 'W')
                {
                    if (weekSeen)
                    {
                        throw new DeserializationException("Week part of TimeSpan seen twice", ref reader, false);
                    }

                    week = (long)whole;
                    weekSeen = true;
                    continue;
                }

                if (part == 'D')
                {
                    if (daySeen)
                    {
                        throw new DeserializationException("Day part of TimeSpan seen twice", ref reader, false);
                    }

                    day = (long)whole;
                    daySeen = true;
                    continue;
                }

                throw new DeserializationException("Expected Y, M, W, or D but found: " + part, ref reader, false);
            }

            return false;
        }

        static readonly MethodInfo ReadMicrosoftDateTimeOffsetThunkReader = typeof(Methods).GetMethod("_ReadMicrosoftDateTimeOffsetThunkReader", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DateTimeOffset _ReadMicrosoftDateTimeOffsetThunkReader(ref ThunkReader reader)
        {
            const long EpochTicks = 621355968000000000L;
            const long MillisecondsToTicks = 10000L;

            var millis = _ReadInt64ThunkReader(ref reader);

            var utcTicks = EpochTicks + (millis * MillisecondsToTicks);

            var c = reader.Peek();
            if (!(c == '+' || c == '-'))
            {
                // no timezone, means it's UTC
                return new DateTimeOffset(utcTicks, TimeSpan.Zero);
            }

            reader.Read();
            var isNegative = c == '-';

            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            var hour = (c - '0');

            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            hour *= 10;
            hour += (c - '0');

            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            var min = (c - '0');

            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);
            min *= 10;
            min += (c - '0');

            var offset = new TimeSpan(hour, min, 0);

            if (isNegative)
            {
                offset = offset.Negate();
            }

            utcTicks += offset.Ticks;

            return new DateTimeOffset(utcTicks, offset);
        }

        public static readonly MethodInfo ThrowNoDefinedValueInEnumThunkReader = typeof(Methods).GetMethod("_ThrowNoDefinedValueInEnumThunkReader", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _ThrowNoDefinedValueInEnumThunkReader(string message, ref ThunkReader reader)
        {
            throw new DeserializationException(message, ref reader, false);
        }

        static readonly MethodInfo ReadBoolThunkReader = typeof(Methods).GetMethod("_ReadBoolThunkReader", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool _ReadBoolThunkReader(ref ThunkReader reader)
        {
            int firstChar = reader.Read();
            if (firstChar == -1)
            {
                throw new DeserializationException("Expected: 'true or false', but the reader ended", ref reader, true);
            }

            if (firstChar == 't')
            {
                int c;

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected character: 'r', but the reader ended", ref reader, true);
                if (c != 'r') throw new DeserializationException("Expected character: 'r'", ref reader, false);

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected character: 'u', but the reader ended", ref reader, true);
                if (c != 'u') throw new DeserializationException("Expected character: 'u'", ref reader, false);

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected character: 'e', but the reader ended", ref reader, true);
                if (c != 'e') throw new DeserializationException("Expected character: 'e'", ref reader, false);

                return true;
            }

            if (firstChar == 'f')
            {
                int c;

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected character: 'a', but the reader ended", ref reader, true);
                if (c != 'a') throw new DeserializationException("Expected character: 'a'", ref reader, false);

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected character: 'l', but the reader ended", ref reader, true);
                if (c != 'l') throw new DeserializationException("Expected character: 'l'", ref reader, false);

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected character: 's', but the reader ended", ref reader, true);
                if (c != 's') throw new DeserializationException("Expected character: 's'", ref reader, false);

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected character: 'e', but the reader ended", ref reader, true);
                if (c != 'e') throw new DeserializationException("Expected character: 'e'", ref reader, false);

                return false;
            }

            throw new DeserializationException("Expected: 'true or false'", ref reader, false);
        }

        static readonly MethodInfo IsNullThunkReader = typeof(Methods).GetMethod("_IsNullThunkReader", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool _IsNullThunkReader(ref ThunkReader reader)
        {
            // peek, so if it's not 'n' the next consumer can read the whole value
            var first = reader.Peek();
            if (first == -1)
            {
                throw new DeserializationException("Unexpected end of stream while expecting null or value", ref reader, true);
            }

            if (first != 'n') return false;

            reader.Read();  // skip 'n'

            int c;

            c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected character: 'u', but the reader ended", ref reader, true);
            if (c != 'u') throw new DeserializationException("Expected character: 'u'", ref reader, false);

            c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected character: 'l', but the reader ended", ref reader, true);
            if (c != 'l') throw new DeserializationException("Expected character: 'l'", ref reader, false);

            c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected character: 'l', but the reader ended", ref reader, true);
            if (c != 'l') throw new DeserializationException("Expected character: 'l'", ref reader, false);

            return true;
        }
    }
}
