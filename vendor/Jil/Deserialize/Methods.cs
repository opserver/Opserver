using Jil.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Deserialize
{
    static partial class Methods
    {
        public const int DynamicCharBufferInitialSize = 128;
        public const int CharBufferSize = 33;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void InitDynamicBuffer(ref char[] dynBuffer)
        {
            dynBuffer = dynBuffer ?? new char[DynamicCharBufferInitialSize];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void GrowDynamicBuffer(ref char[] dynBuffer)
        {
            var newLen = dynBuffer.Length * 2;
            if (newLen < DynamicCharBufferInitialSize) newLen = DynamicCharBufferInitialSize;

            var biggerBuffer = new char[newLen];
            Array.Copy(dynBuffer, biggerBuffer, dynBuffer.Length);
            dynBuffer = biggerBuffer;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        struct GuidStruct
        {
            [FieldOffset(0)]
            public readonly Guid Value;

            [FieldOffset(0)]
            public byte B00;
            [FieldOffset(1)]
            public byte B01;
            [FieldOffset(2)]
            public byte B02;
            [FieldOffset(3)]
            public byte B03;
            [FieldOffset(4)]
            public byte B04;
            [FieldOffset(5)]
            public byte B05;

            [FieldOffset(6)]
            public byte B06;
            [FieldOffset(7)]
            public byte B07;
            [FieldOffset(8)]
            public byte B08;
            [FieldOffset(9)]
            public byte B09;

            [FieldOffset(10)]
            public byte B10;
            [FieldOffset(11)]
            public byte B11;

            [FieldOffset(12)]
            public byte B12;
            [FieldOffset(13)]
            public byte B13;
            [FieldOffset(14)]
            public byte B14;
            [FieldOffset(15)]
            public byte B15;
        }

        static readonly MethodInfo ReadGuid = typeof(Methods).GetMethod("_ReadGuid", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Guid _ReadGuid(TextReader reader)
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
            asStruct.B03 = ReadGuidByte(reader);
            asStruct.B02 = ReadGuidByte(reader);
            asStruct.B01 = ReadGuidByte(reader);
            asStruct.B00 = ReadGuidByte(reader);

            var c = reader.Read();
            if (c != '-') throw new DeserializationException("Expected -", reader, c == -1);

            asStruct.B05 = ReadGuidByte(reader);
            asStruct.B04 = ReadGuidByte(reader);

            c = reader.Read();
            if (c != '-') throw new DeserializationException("Expected -", reader, c == -1);

            asStruct.B07 = ReadGuidByte(reader);
            asStruct.B06 = ReadGuidByte(reader);

            c = reader.Read();
            if (c != '-') throw new DeserializationException("Expected -", reader, c == -1);

            asStruct.B08 = ReadGuidByte(reader);
            asStruct.B09 = ReadGuidByte(reader);

            c = reader.Read();
            if (c != '-') throw new DeserializationException("Expected -", reader, c == -1);

            asStruct.B10 = ReadGuidByte(reader);
            asStruct.B11 = ReadGuidByte(reader);
            asStruct.B12 = ReadGuidByte(reader);
            asStruct.B13 = ReadGuidByte(reader);
            asStruct.B14 = ReadGuidByte(reader);
            asStruct.B15 = ReadGuidByte(reader);

            return asStruct.Value;
        }

        static byte ReadGuidByte(TextReader reader)
        {
            var a = reader.Read();
            if (a == -1) throw new DeserializationException("Expected any character", reader, true);
            if (!((a >= '0' && a <= '9') || (a >= 'A' && a <= 'F') || (a >= 'a' && a <= 'f'))) throw new DeserializationException("Expected a hex number", reader, false);
            var b = reader.Read();
            if (b == -1) throw new DeserializationException("Expected any character", reader, true);
            if (!((b >= '0' && b <= '9') || (b >= 'A' && b <= 'F') || (b >= 'a' && b <= 'f'))) throw new DeserializationException("Expected a hex number", reader, false);

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

        static readonly MethodInfo Skip = typeof(Methods).GetMethod("_Skip", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _Skip(TextReader reader)
        {
            SkipWithLeadChar(reader, reader.Read());
        }

        static void SkipWithLeadChar(TextReader reader, int leadChar)
        {
            // skip null
            if (leadChar == 'n')
            {
                var c = reader.Read();
                if (c != 'u') throw new DeserializationException("Expected u", reader, c == -1);

                c = reader.Read();
                if (c != 'l') throw new DeserializationException("Expected l", reader, c == -1);

                c = reader.Read();
                if (c != 'l') throw new DeserializationException("Expected l", reader, c == -1);
                return;
            }

            // skip a string
            if (leadChar == '"')
            {
                SkipEncodedStringWithLeadChar(reader, leadChar);
                return;
            }

            // skip an object
            if (leadChar == '{')
            {
                SkipObject(reader, leadChar);
                return;
            }

            // skip a list
            if (leadChar == '[')
            {
                SkipList(reader, leadChar);
                return;
            }

            // skip a number
            if ((leadChar >= '0' && leadChar <= '9') || leadChar == '-')
            {
                SkipNumber(reader, leadChar);
                return;
            }

            // skip false
            if (leadChar == 'f')
            {
                var c = reader.Read();
                if (c != 'a') throw new DeserializationException("Expected a", reader, c == -1);

                c = reader.Read();
                if (c != 'l') throw new DeserializationException("Expected l", reader, c == -1);

                c = reader.Read();
                if (c != 's') throw new DeserializationException("Expected s", reader, c == -1);

                c = reader.Read();
                if (c != 'e') throw new DeserializationException("Expected e", reader, c == -1);
                return;
            }

            // skip true
            if (leadChar == 't')
            {
                var c = reader.Read();
                if (c != 'r') throw new DeserializationException("Expected r", reader, c == -1);

                c = reader.Read();
                if (c != 'u') throw new DeserializationException("Expected u", reader, c == -1);

                c = reader.Read();
                if (c != 'e') throw new DeserializationException("Expected e", reader, c == -1);
                return;
            }

            throw new DeserializationException("Expected digit, -, \", {, n, t, f, or [", reader, leadChar == -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SkipObject(TextReader reader, int leadChar)
        {
            if (leadChar != '{') throw new DeserializationException("Expected {", reader, leadChar == -1);

            int c;

            c = _ReadSkipWhitespace(reader);
            if (c == '}')
            {
                return;
            }
            SkipEncodedStringWithLeadChar(reader, c);
            c = _ReadSkipWhitespace(reader);
            if (c != ':') throw new DeserializationException("Expected :", reader, c == -1);
            c = _ReadSkipWhitespace(reader);
            SkipWithLeadChar(reader, c);

            while (true)
            {
                c = _ReadSkipWhitespace(reader);
                if (c == '}') return;
                if (c != ',') throw new DeserializationException("Expected ,", reader, c == -1);

                c = _ReadSkipWhitespace(reader);
                SkipEncodedStringWithLeadChar(reader, c);
                c = _ReadSkipWhitespace(reader);
                if (c != ':') throw new DeserializationException("Expected :", reader, c == -1);
                c = _ReadSkipWhitespace(reader);
                SkipWithLeadChar(reader, c);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SkipList(TextReader reader, int leadChar)
        {
            if (leadChar != '[') throw new DeserializationException("Expected [", reader, leadChar == -1);

            int c;

            c = _ReadSkipWhitespace(reader);
            if (c == ']')
            {
                return;
            }
            SkipWithLeadChar(reader, c);

            while (true)
            {
                c = _ReadSkipWhitespace(reader);
                if (c == ']') return;
                if (c != ',') throw new DeserializationException("Expected ], or ,", reader, c == -1);
                c = _ReadSkipWhitespace(reader);
                SkipWithLeadChar(reader, c);
            }
        }

        static MethodInfo SkipEncodedString = typeof(Methods).GetMethod("_SkipEncodedString", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _SkipEncodedString(TextReader reader)
        {
            SkipEncodedStringWithLeadChar(reader, reader.Read());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SkipEncodedStringWithLeadChar(TextReader reader, int leadChar)
        {
            if (leadChar != '"') throw new DeserializationException("Expected \"", reader, leadChar == -1);

            while (true)
            {
                var first = reader.Read();
                if (first == -1) throw new DeserializationException("Expected any character", reader, true);

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
                if (second == -1) throw new DeserializationException("Expected any character", reader, true);

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

                if (second != 'u') throw new DeserializationException("Unrecognized escape sequence", reader, false);

                // now we're in an escape sequence, we expect 4 hex #s; always
                var u = reader.Read();
                if (!((u >= '0' && u <= '9') || (u >= 'A' && u <= 'F') || (u >= 'a' && u <= 'f'))) throw new DeserializationException("Expected hex digit", reader, u == -1);
                u = reader.Read();
                if (!((u >= '0' && u <= '9') || (u >= 'A' && u <= 'F') || (u >= 'a' && u <= 'f'))) throw new DeserializationException("Expected hex digit", reader, u == -1);
                u = reader.Read();
                if (!((u >= '0' && u <= '9') || (u >= 'A' && u <= 'F') || (u >= 'a' && u <= 'f'))) throw new DeserializationException("Expected hex digit", reader, u == -1);
                u = reader.Read();
                if (!((u >= '0' && u <= '9') || (u >= 'A' && u <= 'F') || (u >= 'a' && u <= 'f'))) throw new DeserializationException("Expected hex digit", reader, u == -1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SkipNumber(TextReader reader, int leadChar)
        {
            // leadChar should be a start of the number

            var seenDecimal = false;
            var seenExponent = false;

            while (true)
            {
                var c = reader.Peek();
                if (c == -1) throw new DeserializationException("Unexpected end of reader", reader, true);

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

                    throw new DeserializationException("Expected -, or a digit", reader, false);
                }

                return;
            }
        }

        static readonly MethodInfo ConsumeWhiteSpace = typeof(Methods).GetMethod("_ConsumeWhiteSpace", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _ConsumeWhiteSpace(TextReader reader)
        {
            int c;
            while ((c = reader.Peek()) != -1)
            {
                if (!IsWhiteSpace(c)) return;

                reader.Read();
            }
        }

        static readonly MethodInfo ReadSkipWhitespace = typeof(Methods).GetMethod("_ReadSkipWhitespace", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int _ReadSkipWhitespace(TextReader reader)
        {
            int c;
            do { c = reader.Read(); }
            while (IsWhiteSpace(c));
            return c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        static readonly MethodInfo ReadEncodedString = typeof(Methods).GetMethod("_ReadEncodedString", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string _ReadEncodedString(TextReader reader, ref StringBuilder commonSb)
        {
            commonSb = commonSb ?? new StringBuilder();

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

        static readonly MethodInfo ReadEncodedStringWithCharArray = typeof(Methods).GetMethod("_ReadEncodedStringWithCharArray", BindingFlags.Static | BindingFlags.NonPublic);
        static string _ReadEncodedStringWithCharArray(TextReader reader, ref char[] buffer)
        {
            var idx = 0;
            InitDynamicBuffer(ref buffer);

            while (true)
            {
                while (idx < buffer.Length - 1)
                {
                    var first = reader.Read();
                    if (first == -1) throw new DeserializationException("Expected any character", reader, true);

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
                    if (second == -1) throw new DeserializationException("Expected any character", reader, true);

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

                    if (second != 'u') throw new DeserializationException("Unrecognized escape sequence", reader, false);

                    // now we're in an escape sequence, we expect 4 hex #s; always
                    buffer[idx++] = ReadHexQuad2(reader);
                }

                GrowDynamicBuffer(ref buffer);
            }

        complete: return new string(buffer, 0, idx);
        }

        static char ReadHexQuad2(TextReader reader)
        {
            int unescaped;
            int c;

            c = reader.Read();
            if (c >= '0' && c <= '9') {
                unescaped = (c - '0') << 12;
            } else if (c >= 'A' && c <= 'F') {
                unescaped = (10 + c - 'A') << 12;
            } else if (c >= 'a' && c <= 'f') {
                unescaped = (10 + c - 'a') << 12;
            } else {
                throw new DeserializationException("Expected hex digit, found: " + c, reader, c == -1);
            }

            c = reader.Read();
            if (c >= '0' && c <= '9') {
                unescaped |= (c - '0') << 8;
            } else if (c >= 'A' && c <= 'F') {
                unescaped |= (10 + c - 'A') << 8;
            } else if (c >= 'a' && c <= 'f') {
                unescaped |= (10 + c - 'a') << 8;
            } else {
                throw new DeserializationException("Expected hex digit, found: " + c, reader, c == -1);
            }

            c = reader.Read();
            if (c >= '0' && c <= '9') {
                unescaped |= (c - '0') << 4;
            } else if (c >= 'A' && c <= 'F') {
                unescaped |= (10 + c - 'A') << 4;
            } else if (c >= 'a' && c <= 'f') {
                unescaped |= (10 + c - 'a') << 4;
            } else {
                throw new DeserializationException("Expected hex digit, found: " + c, reader, c == -1);
            }

            c = reader.Read();
            if (c >= '0' && c <= '9') {
                unescaped |= c - '0';
            } else if (c >= 'A' && c <= 'F') {
                unescaped |= 10 + c - 'A';
            } else if (c >= 'a' && c <= 'f') {
                unescaped |= 10 + c - 'a';
            } else {
                throw new DeserializationException("Expected hex digit, found: " + c, reader, c == -1);
            }

            return (char)unescaped;
        }

        static readonly MethodInfo ReadEncodedStringWithBuffer = typeof(Methods).GetMethod("_ReadEncodedStringWithBuffer", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string _ReadEncodedStringWithBuffer(TextReader reader, char[] buffer, ref StringBuilder commonSb)
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

        static readonly MethodInfo ReadEncodedChar = typeof(Methods).GetMethod("_ReadEncodedChar", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static char _ReadEncodedChar(TextReader reader)
        {
            var first = reader.Read();
            if (first == -1) throw new DeserializationException("Expected any character", reader, true);

            if (first != '\\') return (char)first;

            var second = reader.Read();
            if (second == -1) throw new DeserializationException("Expected any character", reader, true);

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

            if (second != 'u') throw new DeserializationException("Unrecognized escape sequence", reader, false);

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

                throw new DeserializationException("Expected hex digit, found: " + c, reader, c == -1);
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

                throw new DeserializationException("Expected hex digit, found: " + c, reader, c == -1);
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

                throw new DeserializationException("Expected hex digit, found: " + c, reader, c == -1);
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

                throw new DeserializationException("Expected hex digit, found: " + c, reader, c == -1);
            }

        finished:
            if (ret < char.MinValue || ret > char.MaxValue) throw new DeserializationException("Encoded character out of System.Char range, found: " + ret, reader, false);

            return (char)ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadHexQuad(TextReader reader)
        {
            int unescaped = 0;

            //char1:
            {
                var c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected hex digit, instead reader ended", reader, true);

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

                throw new DeserializationException("Expected hex digit, found: " + c, reader, false);
            }

        char2:
            unescaped *= 16;
            {
                var c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected hex digit, instead reader ended", reader, true);

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

                throw new DeserializationException("Expected hex digit, found: " + c, reader, false);
            }

        char3:
            unescaped *= 16;
            {
                var c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected hex digit, instead reader ended", reader, true);

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

                throw new DeserializationException("Expected hex digit, found: " + c, reader, false);
            }

        char4:
            unescaped *= 16;
            {
                var c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected hex digit, instead reader ended", reader, true);

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

                throw new DeserializationException("Expected hex digit, found: " + c, reader, false);
            }

        finished:
            return unescaped;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ReadHexQuadToBuilder(TextReader reader, StringBuilder commonSb)
        {
            var encodedChar = 0;

            //char1:
            {
                var c = reader.Read();

                if (c == -1) throw new DeserializationException("Expected hex digit, stream ended instead", reader, true);

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

                if (c == -1) throw new DeserializationException("Expected hex digit, stream ended instead", reader, true);

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

                if (c == -1) throw new DeserializationException("Expected hex digit, stream ended instead", reader, true);

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

                if (c == -1) throw new DeserializationException("Expected hex digit, stream ended instead", reader, true);

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

        static readonly MethodInfo ParseEnum = typeof(Methods).GetMethod("_ParseEnum", BindingFlags.NonPublic | BindingFlags.Static);
        static TEnum _ParseEnum<TEnum>(string asStr, TextReader reader)
            where TEnum : struct
        {
            TEnum ret;
            if (!TryParseEnum<TEnum>(asStr, out ret))
            {
                throw new DeserializationException("Unexpected value for " + typeof(TEnum).Name + ": " + asStr, reader, false);
            }

            return ret;
        }

        static bool TryParseEnum<TEnum>(string asStr, out TEnum parsed)
            where TEnum : struct
        {
            return EnumValues<TEnum>.TryParse(asStr, out parsed);
        }

        static readonly MethodInfo ReadFlagsEnum = typeof(Methods).GetMethod("_ReadFlagsEnum", BindingFlags.NonPublic | BindingFlags.Static);
        static TEnum _ReadFlagsEnum<TEnum>(TextReader reader, ref StringBuilder commonSb)
            where TEnum : struct
        {
            commonSb = commonSb ?? new StringBuilder();

            var ret = default(TEnum);

            while (true)
            {
                var c = _ReadEncodedChar(reader);

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
                            throw new DeserializationException("Expected " + typeof(TEnum).Name + ", found: " + asStr, reader, false);
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

        static readonly MethodInfo ReadMicrosoftTimeSpan = typeof(Methods).GetMethod("_ReadMicrosoftTimeSpan", BindingFlags.NonPublic | BindingFlags.Static);
        static TimeSpan _ReadMicrosoftTimeSpan(TextReader reader, char[] buffer)
        {
            var strLen = ReadTimeSpanInto(reader, buffer);

            if (strLen == 0)
            {
                throw new DeserializationException("Unexpected empty string", reader, false);
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

                    throw new DeserializationException("Unexpected character .", reader, false);
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

                    throw new DeserializationException("Unexpected character :", reader, false);
                }

                if (c < '0' || c > '9')
                {
                    throw new DeserializationException("Expected digit, found " + c, reader, false);
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
                throw new DeserializationException("Missing required portion of TimeSpan", reader, false);
            }

            var msInt = 0;
            if (fraction != 0)
            {
                var sizeOfFraction = strLen - (ixOfLastPeriod + 1);

                if (sizeOfFraction > 7)
                {
                    throw new DeserializationException("Fractional part of TimeSpan too large", reader, false);
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

        static readonly MethodInfo ReadRFC1123Date = typeof(Methods).GetMethod("_ReadRFC1123Date", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DateTime _ReadRFC1123Date(TextReader reader)
        {
            // ddd, dd MMM yyyy HH:mm:ss GMT'"

            var dayOfWeek = ReadRFC1123DayOfWeek(reader);

            var c = reader.Read();
            if (c != ',') throw new DeserializationException("Expected ,", reader, c == -1);

            c = reader.Read();
            if (c != ' ') throw new DeserializationException("Expected [space]", reader, c == -1);

            var day = 0;
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
            day += (c - '0');
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
            day *= 10;
            day += (c - '0');

            c = reader.Read();
            if (c != ' ') throw new DeserializationException("Expected [space]", reader, c == -1);

            var month = ReadRFC1123Month(reader);

            c = reader.Read();
            if (c != ' ') throw new DeserializationException("Expected [space]", reader, c == -1);

            var year = 0;
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
            year += (c - '0');
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
            year *= 10;
            year += (c - '0');
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
            year *= 10;
            year += (c - '0');
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
            year *= 10;
            year += (c - '0');

            c = reader.Read();
            if (c != ' ') throw new DeserializationException("Expected [space]", reader, c == -1);

            var hour = 0;
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
            hour += (c - '0');
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
            hour *= 10;
            hour += (c - '0');

            c = reader.Read();
            if (c != ':') throw new DeserializationException("Expected :", reader, c == -1);

            var min = 0;
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
            min += (c - '0');
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
            min *= 10;
            min += (c - '0');

            c = reader.Read();
            if (c != ':') throw new DeserializationException("Expected :", reader, c == -1);

            var sec = 0;
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
            sec += (c - '0');
            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
            sec *= 10;
            sec += (c - '0');

            c = reader.Read();
            if (c != ' ') throw new DeserializationException("Expected [space]", reader, c == -1);

            c = reader.Read();
            if (c != 'G') throw new DeserializationException("Expected G", reader, c == -1);
            c = reader.Read();
            if (c != 'M') throw new DeserializationException("Expected M", reader, c == -1);
            c = reader.Read();
            if (c != 'T') throw new DeserializationException("Expected T", reader, c == -1);

            var ret = new DateTime(year, month, day, hour, min, sec, DateTimeKind.Utc);

            if (ret.DayOfWeek != (DayOfWeek)dayOfWeek)
            {
                throw new DeserializationException("RFC1123 DateTime claimed to be [" + (DayOfWeek)dayOfWeek + "], but really was [" + ret.DayOfWeek + "]", reader, false);
            }

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte ReadRFC1123Month(TextReader reader)
        {
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected month, instead stream ended", reader, true);

            // Jan | Jun | Jul
            if (c == 'J')
            {
                c = reader.Read();
                if (c == 'a')
                {
                    c = reader.Read();
                    if (c != 'n') throw new DeserializationException("Expected n", reader, c == -1);

                    return 1;
                }

                if (c != 'u') throw new DeserializationException("Expected u", reader, c == -1);

                c = reader.Read();
                if (c == 'n') return 6;
                if (c == 'l') return 7;

                throw new DeserializationException("Expected n, or l", reader, c == -1);
            }

            // Feb
            if (c == 'F')
            {
                c = reader.Read();
                if (c != 'e') throw new DeserializationException("Expected e", reader, c == -1);
                c = reader.Read();
                if (c != 'b') throw new DeserializationException("Expected b", reader, c == -1);

                return 2;
            }

            // Mar | May
            if (c == 'M')
            {
                c = reader.Read();
                if (c != 'a') throw new DeserializationException("Expected a", reader, c == -1);

                c = reader.Read();
                if (c == 'r') return 3;
                if (c == 'y') return 5;

                throw new DeserializationException("Expected r, or y", reader, c == -1);
            }

            // Apr | Aug
            if (c == 'A')
            {
                c = reader.Read();
                if (c == 'p')
                {
                    c = reader.Read();
                    if (c != 'r') throw new DeserializationException("Expected r", reader, c == -1);

                    return 4;
                }

                if (c == 'u')
                {
                    c = reader.Read();
                    if (c != 'g') throw new DeserializationException("Expected g", reader, c == -1);

                    return 8;
                }

                throw new DeserializationException("Expected p, or u", reader, c == -1);
            }

            // Sep
            if (c == 'S')
            {
                c = reader.Read();
                if (c != 'e') throw new DeserializationException("Expected e", reader, c == -1);
                c = reader.Read();
                if (c != 'p') throw new DeserializationException("Expected p", reader, c == -1);

                return 9;
            }

            // Oct
            if (c == 'O')
            {
                c = reader.Read();
                if (c != 'c') throw new DeserializationException("Expected c", reader, c == -1);
                c = reader.Read();
                if (c != 't') throw new DeserializationException("Expected t", reader, c == -1);

                return 10;
            }

            // Nov
            if (c == 'N')
            {
                c = reader.Read();
                if (c != 'o') throw new DeserializationException("Expected o", reader, c == -1);
                c = reader.Read();
                if (c != 'v') throw new DeserializationException("Expected v", reader, c == -1);

                return 11;
            }

            // Dec
            if (c == 'D')
            {
                c = reader.Read();
                if (c != 'e') throw new DeserializationException("Expected e", reader, c == -1);
                c = reader.Read();
                if (c != 'c') throw new DeserializationException("Expected c", reader, c == -1);

                return 12;
            }

            throw new DeserializationException("Expected J, F, M, A, S, O, N, or D", reader, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DayOfWeek ReadRFC1123DayOfWeek(TextReader reader)
        {
            var c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected day of week, instead stream ended", reader, true);

            // Mon
            if (c == 'M')
            {
                c = reader.Read();
                if (c != 'o') throw new DeserializationException("Expected o", reader, c == -1);

                c = reader.Read();
                if (c != 'n') throw new DeserializationException("Expected n", reader, c == -1);

                return DayOfWeek.Monday;
            }

            // Tue | Thu
            if (c == 'T')
            {
                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected h or u; instead stream ended", reader, true);

                if (c == 'u')
                {
                    c = reader.Read();
                    if (c != 'e') throw new DeserializationException("Expected e", reader, c == -1);

                    return DayOfWeek.Tuesday;
                }

                if (c == 'h')
                {
                    c = reader.Read();
                    if (c != 'u') throw new DeserializationException("Expected u", reader, c == -1);

                    return DayOfWeek.Thursday;
                }

                throw new DeserializationException("Expected h or u", reader, false);
            }

            // Wed
            if (c == 'W')
            {
                c = reader.Read();
                if (c != 'e') throw new DeserializationException("Expected e", reader, c == -1);

                c = reader.Read();
                if (c != 'd') throw new DeserializationException("Expected d", reader, c == -1);

                return DayOfWeek.Wednesday;
            }

            // Fri
            if (c == 'F')
            {
                c = reader.Read();
                if (c != 'r') throw new DeserializationException("Expected r", reader, c == -1);

                c = reader.Read();
                if (c != 'i') throw new DeserializationException("Expected i", reader, c == -1);

                return DayOfWeek.Friday;
            }

            // Sat | Sun
            if (c == 'S')
            {
                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected a or u; instead stream ended", reader, true);

                if (c == 'a')
                {
                    c = reader.Read();
                    if (c != 't') throw new DeserializationException("Expected t", reader, c == -1);

                    return DayOfWeek.Saturday;
                }

                if (c == 'u')
                {
                    c = reader.Read();
                    if (c != 'n') throw new DeserializationException("Expected n", reader, c == -1);

                    return DayOfWeek.Sunday;
                }

                throw new DeserializationException("Expected a or u", reader, false);
            }

            throw new DeserializationException("Expected M, T, W, F, or S", reader, false);
        }

        static readonly MethodInfo ReadMicrosoftDateTimeOffset = typeof(Methods).GetMethod("_ReadMicrosoftDateTimeOffset", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static DateTimeOffset _ReadMicrosoftDateTimeOffset(TextReader reader)
        {
            const long EpochTicks = 621355968000000000L;
            const long MillisecondsToTicks = 10000L;

            var millis = _ReadInt64(reader);

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
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
            var hour = (c - '0');

            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
            hour *= 10;
            hour += (c - '0');

            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
            var min = (c - '0');

            c = reader.Read();
            if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);
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

        public static readonly MethodInfo ThrowNoDefinedValueInEnum = typeof(Methods).GetMethod("_ThrowNoDefinedValueInEnum", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _ThrowNoDefinedValueInEnum(string message, TextReader reader)
        {
            throw new DeserializationException(message, reader, false);
        }

        static readonly MethodInfo ReadBool = typeof(Methods).GetMethod("_ReadBool", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool _ReadBool(TextReader reader)
        {
            int firstChar = reader.Read();
            if (firstChar == -1)
            {
                throw new DeserializationException("Expected: 'true or false', but the reader ended", reader, true);
            }

            if (firstChar == 't')
            {
                int c;

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected character: 'r', but the reader ended", reader, true);
                if (c != 'r') throw new DeserializationException("Expected character: 'r'", reader, false);

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected character: 'u', but the reader ended", reader, true);
                if (c != 'u') throw new DeserializationException("Expected character: 'u'", reader, false);

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected character: 'e', but the reader ended", reader, true);
                if (c != 'e') throw new DeserializationException("Expected character: 'e'", reader, false);

                return true;
            }

            if (firstChar == 'f')
            {
                int c;

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected character: 'a', but the reader ended", reader, true);
                if (c != 'a') throw new DeserializationException("Expected character: 'a'", reader, false);

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected character: 'l', but the reader ended", reader, true);
                if (c != 'l') throw new DeserializationException("Expected character: 'l'", reader, false);

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected character: 's', but the reader ended", reader, true);
                if (c != 's') throw new DeserializationException("Expected character: 's'", reader, false);

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Expected character: 'e', but the reader ended", reader, true);
                if (c != 'e') throw new DeserializationException("Expected character: 'e'", reader, false);

                return false;
            }

            throw new DeserializationException("Expected: 'true or false'", reader, false);
        }

        static readonly MethodInfo IsNull = typeof(Methods).GetMethod("_IsNull", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool _IsNull(TextReader reader)
        {
            // peek, so if it's not 'n' the next consumer can read the whole value
            var first = reader.Peek();
            if (first == -1)
            {
                throw new DeserializationException("Unexpected end of stream while expecting null or value", reader, true);
            }

            if (first != 'n') return false;

            reader.Read();  // skip 'n'

            int c;

            c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected character: 'u', but the reader ended", reader, true);
            if (c != 'u') throw new DeserializationException("Expected character: 'u'", reader, false);

            c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected character: 'l', but the reader ended", reader, true);
            if (c != 'l') throw new DeserializationException("Expected character: 'l'", reader, false);

            c = reader.Read();
            if (c == -1) throw new DeserializationException("Expected character: 'l', but the reader ended", reader, true);
            if (c != 'l') throw new DeserializationException("Expected character: 'l'", reader, false);

            return true;
        }
    }
}
