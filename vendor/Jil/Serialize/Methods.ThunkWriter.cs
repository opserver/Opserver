using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Serialize
{
    static partial class Methods
    {
        static readonly MethodInfo WriteGuid_ThunkWriter = typeof(Methods).GetMethod("_WriteGuid_ThunkWriter", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _WriteGuid_ThunkWriter(ref ThunkWriter writer, Guid guid, char[] buffer)
        {
            // 1314FAD4-7505-439D-ABD2-DBD89242928C
            // 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ
            //
            // Guid is guaranteed to be a 36 character string

            // get all the dashes in place
            buffer[8] = '-';
            buffer[13] = '-';
            buffer[18] = '-';
            buffer[23] = '-';

            // Bytes are in a different order than you might expect
            // For: 35 91 8b c9 - 19 6d - 40 ea  - 97 79  - 88 9d 79 b7 53 f0 
            // Get: C9 8B 91 35   6D 19   EA 40    97 79    88 9D 79 B7 53 F0 
            // Ix:   0  1  2  3    4  5    6  7     8  9    10 11 12 13 14 15
            //
            // And we have to account for dashes
            //
            // So the map is like so:
            // bytes[0]  -> chars[3]  -> buffer[ 6, 7]
            // bytes[1]  -> chars[2]  -> buffer[ 4, 5]
            // bytes[2]  -> chars[1]  -> buffer[ 2, 3]
            // bytes[3]  -> chars[0]  -> buffer[ 0, 1]
            // bytes[4]  -> chars[5]  -> buffer[11,12]
            // bytes[5]  -> chars[4]  -> buffer[ 9,10]
            // bytes[6]  -> chars[7]  -> buffer[16,17]
            // bytes[7]  -> chars[6]  -> buffer[14,15]
            // bytes[8]  -> chars[8]  -> buffer[19,20]
            // bytes[9]  -> chars[9]  -> buffer[21,22]
            // bytes[10] -> chars[10] -> buffer[24,25]
            // bytes[11] -> chars[11] -> buffer[26,27]
            // bytes[12] -> chars[12] -> buffer[28,29]
            // bytes[13] -> chars[13] -> buffer[30,31]
            // bytes[14] -> chars[14] -> buffer[32,33]
            // bytes[15] -> chars[15] -> buffer[34,35]
            var visibleMembers = new GuidStruct(guid);

            // bytes[0]
            var b = visibleMembers.B00 * 2;
            buffer[6] = WriteGuidLookup[b];
            buffer[7] = WriteGuidLookup[b + 1];

            // bytes[1]
            b = visibleMembers.B01 * 2;
            buffer[4] = WriteGuidLookup[b];
            buffer[5] = WriteGuidLookup[b + 1];

            // bytes[2]
            b = visibleMembers.B02 * 2;
            buffer[2] = WriteGuidLookup[b];
            buffer[3] = WriteGuidLookup[b + 1];

            // bytes[3]
            b = visibleMembers.B03 * 2;
            buffer[0] = WriteGuidLookup[b];
            buffer[1] = WriteGuidLookup[b + 1];

            // bytes[4]
            b = visibleMembers.B04 * 2;
            buffer[11] = WriteGuidLookup[b];
            buffer[12] = WriteGuidLookup[b + 1];

            // bytes[5]
            b = visibleMembers.B05 * 2;
            buffer[9] = WriteGuidLookup[b];
            buffer[10] = WriteGuidLookup[b + 1];

            // bytes[6]
            b = visibleMembers.B06 * 2;
            buffer[16] = WriteGuidLookup[b];
            buffer[17] = WriteGuidLookup[b + 1];

            // bytes[7]
            b = visibleMembers.B07 * 2;
            buffer[14] = WriteGuidLookup[b];
            buffer[15] = WriteGuidLookup[b + 1];

            // bytes[8]
            b = visibleMembers.B08 * 2;
            buffer[19] = WriteGuidLookup[b];
            buffer[20] = WriteGuidLookup[b + 1];

            // bytes[9]
            b = visibleMembers.B09 * 2;
            buffer[21] = WriteGuidLookup[b];
            buffer[22] = WriteGuidLookup[b + 1];

            // bytes[10]
            b = visibleMembers.B10 * 2;
            buffer[24] = WriteGuidLookup[b];
            buffer[25] = WriteGuidLookup[b + 1];

            // bytes[11]
            b = visibleMembers.B11 * 2;
            buffer[26] = WriteGuidLookup[b];
            buffer[27] = WriteGuidLookup[b + 1];

            // bytes[12]
            b = visibleMembers.B12 * 2;
            buffer[28] = WriteGuidLookup[b];
            buffer[29] = WriteGuidLookup[b + 1];

            // bytes[13]
            b = visibleMembers.B13 * 2;
            buffer[30] = WriteGuidLookup[b];
            buffer[31] = WriteGuidLookup[b + 1];

            // bytes[14]
            b = visibleMembers.B14 * 2;
            buffer[32] = WriteGuidLookup[b];
            buffer[33] = WriteGuidLookup[b + 1];

            // bytes[15]
            b = visibleMembers.B15 * 2;
            buffer[34] = WriteGuidLookup[b];
            buffer[35] = WriteGuidLookup[b + 1];

            writer.Write(buffer, 0, 36);
        }

        static readonly MethodInfo CustomISO8601ToString_ThunkWriter = typeof(Methods).GetMethod("_CustomISO8601ToString_ThunkWriter", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _CustomISO8601ToString_ThunkWriter(ref ThunkWriter writer, DateTime dt, char[] buffer)
        {
            // "yyyy-mm-ddThh:mm:ss.fffffffZ"
            // 0123456789ABCDEFGHIJKL
            //
            // Yes, DateTime.Max is in fact guaranteed to have a 4 digit year (and no more)
            // f of 7 digits allows for 1 Tick level resolution

            buffer[0] = '"';

            uint val;

            // Year
            val = (uint)dt.Year;
            var digits = DigitPairs[(byte)(val % 100)];
            buffer[4] = digits.Second;
            buffer[3] = digits.First;
            digits = DigitPairs[(byte)(val / 100)];
            buffer[2] = digits.Second;
            buffer[1] = digits.First;

            // delimiter
            buffer[5] = '-';

            // Month
            digits = DigitPairs[dt.Month];
            buffer[7] = digits.Second;
            buffer[6] = digits.First;

            // Delimiter
            buffer[8] = '-';

            // Day
            digits = DigitPairs[dt.Day];
            buffer[10] = digits.Second;
            buffer[9] = digits.First;

            // Delimiter
            buffer[11] = 'T';

            digits = DigitPairs[dt.Hour];
            buffer[13] = digits.Second;
            buffer[12] = digits.First;

            // Delimiter
            buffer[14] = ':';

            digits = DigitPairs[dt.Minute];
            buffer[16] = digits.Second;
            buffer[15] = digits.First;

            // Delimiter
            buffer[17] = ':';

            digits = DigitPairs[dt.Second];
            buffer[19] = digits.Second;
            buffer[18] = digits.First;

            int fracEnd;
            var remainingTicks = (dt - new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second)).Ticks;
            if (remainingTicks > 0)
            {
                buffer[20] = '.';

                var fracPart = remainingTicks % 100;
                remainingTicks /= 100;
                if (fracPart > 0)
                {
                    digits = DigitPairs[fracPart];
                    buffer[27] = digits.Second;
                    buffer[26] = digits.First;
                    fracEnd = 28;
                }
                else
                {
                    fracEnd = 26;
                }

                fracPart = remainingTicks % 100;
                remainingTicks /= 100;
                if (fracPart > 0)
                {
                    digits = DigitPairs[fracPart];
                    buffer[25] = digits.Second;
                    buffer[24] = digits.First;
                }
                else
                {
                    if (fracEnd == 26)
                    {
                        fracEnd = 24;
                    }
                    else
                    {
                        buffer[25] = '0';
                        buffer[24] = '0';
                    }
                }

                fracPart = remainingTicks % 100;
                remainingTicks /= 100;
                if (fracPart > 0)
                {
                    digits = DigitPairs[fracPart];
                    buffer[23] = digits.Second;
                    buffer[22] = digits.First;
                }
                else
                {
                    if (fracEnd == 24)
                    {
                        fracEnd = 22;
                    }
                    else
                    {
                        buffer[23] = '0';
                        buffer[22] = '0';
                    }
                }

                fracPart = remainingTicks;
                buffer[21] = (char)('0' + fracPart);
            }
            else
            {
                fracEnd = 20;
            }

            buffer[fracEnd] = 'Z';
            buffer[fracEnd + 1] = '"';

            writer.Write(buffer, 0, fracEnd + 2);
        }

        static readonly MethodInfo CustomISO8601WithOffsetToString_ThunkWriter = typeof(Methods).GetMethod("_CustomISO8601WithOffsetToString_ThunkWriter", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _CustomISO8601WithOffsetToString_ThunkWriter(ref ThunkWriter writer, DateTime dt, int hours, int minutes, char[] buffer)
        {
            if (hours == 0 && minutes == 0)
            {
                // It's actually in UTC time, bail!
                _CustomISO8601ToString_ThunkWriter(ref writer, dt, buffer);
                return;
            }

            // "yyyy-mm-ddThh:mm:ss.fffffff+oo:oo"
            // 0123456789ABCDEFGHIJKLMNOPQRSTUVWXY
            //
            // Yes, DateTime.Max is in fact guaranteed to have a 4 digit year (and no more)
            // f of 7 digits allows for 1 Tick level resolution

            var offsetIsNegative = false;
            if (hours < 0 || minutes < 0)
            {
                offsetIsNegative = true;
                hours = -hours;
                minutes = -minutes;
            }

            buffer[0] = '"';

            uint val;

            // Year
            val = (uint)dt.Year;
            var digits = DigitPairs[(byte)(val % 100)];
            buffer[4] = digits.Second;
            buffer[3] = digits.First;
            digits = DigitPairs[(byte)(val / 100)];
            buffer[2] = digits.Second;
            buffer[1] = digits.First;

            // delimiter
            buffer[5] = '-';

            // Month
            digits = DigitPairs[dt.Month];
            buffer[7] = digits.Second;
            buffer[6] = digits.First;

            // Delimiter
            buffer[8] = '-';

            // Day
            digits = DigitPairs[dt.Day];
            buffer[10] = digits.Second;
            buffer[9] = digits.First;

            // Delimiter
            buffer[11] = 'T';

            digits = DigitPairs[dt.Hour];
            buffer[13] = digits.Second;
            buffer[12] = digits.First;

            // Delimiter
            buffer[14] = ':';

            digits = DigitPairs[dt.Minute];
            buffer[16] = digits.Second;
            buffer[15] = digits.First;

            // Delimiter
            buffer[17] = ':';

            digits = DigitPairs[dt.Second];
            buffer[19] = digits.Second;
            buffer[18] = digits.First;

            int fracEnd;
            var remainingTicks = (dt - new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second)).Ticks;
            if (remainingTicks > 0)
            {
                buffer[20] = '.';

                var fracPart = remainingTicks % 100;
                remainingTicks /= 100;
                if (fracPart > 0)
                {
                    digits = DigitPairs[fracPart];
                    buffer[27] = digits.Second;
                    buffer[26] = digits.First;
                    fracEnd = 28;
                }
                else
                {
                    fracEnd = 26;
                }

                fracPart = remainingTicks % 100;
                remainingTicks /= 100;
                if (fracPart > 0)
                {
                    digits = DigitPairs[fracPart];
                    buffer[25] = digits.Second;
                    buffer[24] = digits.First;
                }
                else
                {
                    if (fracEnd == 26)
                    {
                        fracEnd = 24;
                    }
                    else
                    {
                        buffer[25] = '0';
                        buffer[24] = '0';
                    }
                }

                fracPart = remainingTicks % 100;
                remainingTicks /= 100;
                if (fracPart > 0)
                {
                    digits = DigitPairs[fracPart];
                    buffer[23] = digits.Second;
                    buffer[22] = digits.First;
                }
                else
                {
                    if (fracEnd == 24)
                    {
                        fracEnd = 22;
                    }
                    else
                    {
                        buffer[23] = '0';
                        buffer[22] = '0';
                    }
                }

                fracPart = remainingTicks;
                buffer[21] = (char)('0' + fracPart);
            }
            else
            {
                fracEnd = 20;
            }

            if (offsetIsNegative)
            {
                buffer[fracEnd] = '-';
            }
            else
            {
                buffer[fracEnd] = '+';
            }

            digits = DigitPairs[hours];
            buffer[fracEnd + 1] = digits.First;
            buffer[fracEnd + 2] = digits.Second;
            buffer[fracEnd + 3] = ':';

            digits = DigitPairs[minutes];
            buffer[fracEnd + 4] = digits.First;
            buffer[fracEnd + 5] = digits.Second;

            buffer[fracEnd + 6] = '"';

            writer.Write(buffer, 0, fracEnd + 7);
        }

        static readonly MethodInfo WriteEncodedStringWithQuotesWithNullsInlineUnsafe_ThunkWriter = typeof(Methods).GetMethod("_WriteEncodedStringWithQuotesWithNullsInlineUnsafe_ThunkWriter", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void _WriteEncodedStringWithQuotesWithNullsInlineUnsafe_ThunkWriter(ref ThunkWriter writer, string strRef)
        {
            if (strRef == null)
            {
                writer.WriteValueConstant(ConstantString_Value.Null);
                return;
            }

            writer.WriteFormattingConstant(ConstantString_Formatting.Quote);

            fixed (char* strFixed = strRef)
            {
                char* str = strFixed;
                char c;
                var len = strRef.Length;

                while (len > 0)
                {
                    c = *str;
                    str++;
                    len--;

                    if (c == '\\')
                    {
                        writer.WriteCommonConstant(ConstantString_Common.DoubleBackSlash);
                        continue;
                    }

                    if (c == '"')
                    {
                        writer.WriteFormattingConstant(ConstantString_Formatting.BackSlashQuote);
                        continue;
                    }

                    // This is converted into an IL switch, so don't fret about lookup times
                    switch (c)
                    {
                        case '\u0000': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0000); continue;
                        case '\u0001': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0001); continue;
                        case '\u0002': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0002); continue;
                        case '\u0003': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0003); continue;
                        case '\u0004': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0004); continue;
                        case '\u0005': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0005); continue;
                        case '\u0006': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0006); continue;
                        case '\u0007': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0007); continue;
                        case '\u0008': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_b); continue;
                        case '\u0009': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_t); continue;
                        case '\u000A': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_n); continue;
                        case '\u000B': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_000B); continue;
                        case '\u000C': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_f); continue;
                        case '\u000D': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_r); continue;
                        case '\u000E': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_000E); continue;
                        case '\u000F': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_000F); continue;
                        case '\u0010': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0010); continue;
                        case '\u0011': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0011); continue;
                        case '\u0012': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0012); continue;
                        case '\u0013': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0013); continue;
                        case '\u0014': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0014); continue;
                        case '\u0015': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0015); continue;
                        case '\u0016': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0016); continue;
                        case '\u0017': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0017); continue;
                        case '\u0018': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0018); continue;
                        case '\u0019': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0019); continue;
                        case '\u001A': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001A); continue;
                        case '\u001B': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001B); continue;
                        case '\u001C': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001C); continue;
                        case '\u001D': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001D); continue;
                        case '\u001E': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001E); continue;
                        case '\u001F': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001F); continue;
                        default: writer.Write(c); continue;
                    }
                }
            }

            writer.WriteFormattingConstant(ConstantString_Formatting.Quote);
        }

        static readonly MethodInfo WriteEncodedStringWithQuotesWithNullsInlineJSONPUnsafe_ThunkWriter = typeof(Methods).GetMethod("_WriteEncodedStringWithQuotesWithNullsInlineJSONPUnsafe_ThunkWriter", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void _WriteEncodedStringWithQuotesWithNullsInlineJSONPUnsafe_ThunkWriter(ref ThunkWriter writer, string strRef)
        {
            if (strRef == null)
            {
                writer.WriteValueConstant(ConstantString_Value.Null);
                return;
            }

            writer.WriteFormattingConstant(ConstantString_Formatting.Quote);

            fixed (char* strFixed = strRef)
            {
                char* str = strFixed;
                char c;
                var len = strRef.Length;

                while (len > 0)
                {
                    c = *str;
                    str++;
                    len--;

                    if (c == '\\')
                    {
                        writer.WriteCommonConstant(ConstantString_Common.DoubleBackSlash);
                        continue;
                    }

                    if (c == '"')
                    {
                        writer.WriteFormattingConstant(ConstantString_Formatting.BackSlashQuote);
                        continue;
                    }

                    if (c == '\u2028')
                    {
                        writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_2028);
                        continue;
                    }

                    if (c == '\u2029')
                    {
                        writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_2029);
                        continue;
                    }

                    // This is converted into an IL switch, so don't fret about lookup times
                    switch (c)
                    {
                        case '\u0000': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0000); continue;
                        case '\u0001': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0001); continue;
                        case '\u0002': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0002); continue;
                        case '\u0003': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0003); continue;
                        case '\u0004': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0004); continue;
                        case '\u0005': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0005); continue;
                        case '\u0006': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0006); continue;
                        case '\u0007': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0007); continue;
                        case '\u0008': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_b); continue;
                        case '\u0009': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_t); continue;
                        case '\u000A': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_n); continue;
                        case '\u000B': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_000B); continue;
                        case '\u000C': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_f); continue;
                        case '\u000D': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_r); continue;
                        case '\u000E': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_000E); continue;
                        case '\u000F': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_000F); continue;
                        case '\u0010': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0010); continue;
                        case '\u0011': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0011); continue;
                        case '\u0012': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0012); continue;
                        case '\u0013': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0013); continue;
                        case '\u0014': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0014); continue;
                        case '\u0015': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0015); continue;
                        case '\u0016': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0016); continue;
                        case '\u0017': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0017); continue;
                        case '\u0018': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0018); continue;
                        case '\u0019': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0019); continue;
                        case '\u001A': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001A); continue;
                        case '\u001B': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001B); continue;
                        case '\u001C': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001C); continue;
                        case '\u001D': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001D); continue;
                        case '\u001E': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001E); continue;
                        case '\u001F': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001F); continue;
                        default: writer.Write(c); continue;
                    }
                }
            }

            writer.WriteFormattingConstant(ConstantString_Formatting.Quote);
        }

        static readonly MethodInfo WriteEncodedStringWithNullsInlineJSONPUnsafe_ThunkWriter = typeof(Methods).GetMethod("_WriteEncodedStringWithNullsInlineJSONPUnsafe_ThunkWriter", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void _WriteEncodedStringWithNullsInlineJSONPUnsafe_ThunkWriter(ref ThunkWriter writer, string strRef)
        {
            if (strRef == null)
            {
                writer.WriteValueConstant(ConstantString_Value.Null);
                return;
            }

            fixed (char* strFixed = strRef)
            {
                char* str = strFixed;
                char c;
                var len = strRef.Length;

                while (len > 0)
                {
                    c = *str;
                    str++;
                    len--;

                    if (c == '\\')
                    {
                        writer.WriteCommonConstant(ConstantString_Common.DoubleBackSlash);
                        continue;
                    }

                    if (c == '"')
                    {
                        writer.WriteFormattingConstant(ConstantString_Formatting.BackSlashQuote);
                        continue;
                    }

                    if (c == '\u2028')
                    {
                        writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_2028);
                        continue;
                    }

                    if (c == '\u2029')
                    {
                        writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_2029);
                        continue;
                    }

                    // This is converted into an IL switch, so don't fret about lookup times
                    switch (c)
                    {
                        case '\u0000': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0000); continue;
                        case '\u0001': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0001); continue;
                        case '\u0002': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0002); continue;
                        case '\u0003': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0003); continue;
                        case '\u0004': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0004); continue;
                        case '\u0005': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0005); continue;
                        case '\u0006': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0006); continue;
                        case '\u0007': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0007); continue;
                        case '\u0008': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_b); continue;
                        case '\u0009': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_t); continue;
                        case '\u000A': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_n); continue;
                        case '\u000B': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_000B); continue;
                        case '\u000C': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_f); continue;
                        case '\u000D': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_r); continue;
                        case '\u000E': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_000E); continue;
                        case '\u000F': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_000F); continue;
                        case '\u0010': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0010); continue;
                        case '\u0011': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0011); continue;
                        case '\u0012': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0012); continue;
                        case '\u0013': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0013); continue;
                        case '\u0014': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0014); continue;
                        case '\u0015': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0015); continue;
                        case '\u0016': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0016); continue;
                        case '\u0017': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0017); continue;
                        case '\u0018': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0018); continue;
                        case '\u0019': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0019); continue;
                        case '\u001A': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001A); continue;
                        case '\u001B': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001B); continue;
                        case '\u001C': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001C); continue;
                        case '\u001D': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001D); continue;
                        case '\u001E': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001E); continue;
                        case '\u001F': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001F); continue;
                        default: writer.Write(c); continue;
                    }
                }
            }
        }

        static readonly MethodInfo WriteEncodedStringWithNullsInlineUnsafe_ThunkWriter = typeof(Methods).GetMethod("_WriteEncodedStringWithNullsInlineUnsafe_ThunkWriter", BindingFlags.NonPublic | BindingFlags.Static);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void _WriteEncodedStringWithNullsInlineUnsafe_ThunkWriter(ref ThunkWriter writer, string strRef)
        {
            if (strRef == null)
            {
                writer.WriteValueConstant(ConstantString_Value.Null);
                return;
            }

            fixed (char* strFixed = strRef)
            {
                char* str = strFixed;
                char c;
                var len = strRef.Length;

                while (len > 0)
                {
                    c = *str;
                    str++;
                    len--;

                    if (c == '\\')
                    {
                        writer.WriteCommonConstant(ConstantString_Common.DoubleBackSlash);
                        continue;
                    }

                    if (c == '"')
                    {
                        writer.WriteFormattingConstant(ConstantString_Formatting.BackSlashQuote);
                        continue;
                    }

                    // This is converted into an IL switch, so don't fret about lookup times
                    switch (c)
                    {
                        case '\u0000': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0000); continue;
                        case '\u0001': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0001); continue;
                        case '\u0002': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0002); continue;
                        case '\u0003': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0003); continue;
                        case '\u0004': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0004); continue;
                        case '\u0005': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0005); continue;
                        case '\u0006': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0006); continue;
                        case '\u0007': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_0007); continue;
                        case '\u0008': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_b); continue;
                        case '\u0009': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_t); continue;
                        case '\u000A': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_n); continue;
                        case '\u000B': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_000B); continue;
                        case '\u000C': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_f); continue;
                        case '\u000D': writer.WriteCommonConstant(ConstantString_Common.EscapeSequence_r); continue;
                        case '\u000E': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_000E); continue;
                        case '\u000F': writer.Write000EscapeConstant(ConstantString_000Escape.EscapeSequence_000F); continue;
                        case '\u0010': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0010); continue;
                        case '\u0011': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0011); continue;
                        case '\u0012': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0012); continue;
                        case '\u0013': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0013); continue;
                        case '\u0014': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0014); continue;
                        case '\u0015': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0015); continue;
                        case '\u0016': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0016); continue;
                        case '\u0017': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0017); continue;
                        case '\u0018': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0018); continue;
                        case '\u0019': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_0019); continue;
                        case '\u001A': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001A); continue;
                        case '\u001B': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001B); continue;
                        case '\u001C': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001C); continue;
                        case '\u001D': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001D); continue;
                        case '\u001E': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001E); continue;
                        case '\u001F': writer.Write001EscapeConstant(ConstantString_001Escape.EscapeSequence_001F); continue;
                        default: writer.Write(c); continue;
                    }
                }
            }
        }

        static readonly MethodInfo CustomWriteInt_ThunkWriter = typeof(Methods).GetMethod("_CustomWriteInt_ThunkWriter", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _CustomWriteInt_ThunkWriter(ref ThunkWriter writer, int number, char[] buffer)
        {
            // Gotta special case this, we can't negate it
            if (number == int.MinValue)
            {
                writer.WriteMinConstant(ConstantString_Min.Int_MinValue);
                return;
            }

            var ptr = InlineSerializer<object>.CharBufferSize - 1;

            uint copy;
            if (number < 0)
            {
                writer.Write('-');
                copy = (uint)(-number);
            }
            else
            {
                copy = (uint)number;
            }

            do
            {
                byte ix = (byte)(copy % 100);
                copy /= 100;

                var chars = DigitPairs[ix];
                buffer[ptr--] = chars.Second;
                buffer[ptr--] = chars.First;
            } while (copy != 0);

            if (buffer[ptr + 1] == '0')
            {
                ptr++;
            }

            writer.Write(buffer, ptr + 1, InlineSerializer<object>.CharBufferSize - 1 - ptr);
        }

        static readonly MethodInfo CustomWriteIntUnrolledSigned_ThunkWriter = typeof(Methods).GetMethod("_CustomWriteIntUnrolledSigned_ThunkWriter", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _CustomWriteIntUnrolledSigned_ThunkWriter(ref ThunkWriter writer, int num, char[] buffer)
        {
            // Why signed integers?
            // Earlier versions of this code used unsigned integers, 
            //   but it turns out that's not ideal and here's why.
            // 
            // The signed version of the relevant code gets JIT'd down to:
            // instr       operands                    latency/throughput (approx. worst case; Haswell)
            // ========================================================================================
            // mov         ecx,###                      2 /  0.5 
            // cdq                                      1 /  -
            // idiv        eax,ecx                     29 / 11
            // mov         ecx,###                      2 /  0.5
            // cdq                                      1 /  -
            // idiv        eax,ecx                     29 / 11
            // movsx       edx,dl                       - /  0.5
            //
            // The unsigned version gets JIT'd down to:
            // instr       operands                    latency/throughput (approx. worst case; Haswell)
            // ========================================================================================
            // mov         ecx,###                       2 /  0.5
            // xor         edx,edx                       1 /  0.25
            // div         eax,ecx                      29 / 11
            // mov         ecx,###                       2 /  0.5
            // xor         edx,edx                       1 /  0.25
            // div         eax,ecx                      29 / 11
            // and         edx,###                       1 /  0.25
            //
            // In theory div (usigned division) is faster than idiv, and it probably is *but* cdq + cdq + movsx is
            //   faster than xor + xor + and; in practice it's fast *enough* to make up the difference.
            // have to special case this, we can't negate it
            if (num == int.MinValue)
            {
                writer.WriteMinConstant(ConstantString_Min.Int_MinValue);
                return;
            }

            int numLen;
            int number;

            if (num < 0)
            {
                writer.Write('-');
                number = -num;
            }
            else
            {
                number = num;
            }

            if (number < 1000)
            {
                if (number >= 100)
                {
                    writer.Write(DigitTriplets, number * 3, 3);
                }
                else
                {
                    if (number >= 10)
                    {
                        writer.Write(DigitTriplets, number * 3 + 1, 2);
                    }
                    else
                    {
                        writer.Write(DigitTriplets, number * 3 + 2, 1);
                    }
                }
                return;
            }
            var d012 = number % 1000 * 3;

            int d543;
            if (number < 1000000)
            {
                d543 = (number / 1000) * 3;
                if (number >= 100000)
                {
                    numLen = 6;
                    goto digit5;
                }
                else
                {
                    if (number >= 10000)
                    {
                        numLen = 5;
                        goto digit4;
                    }
                    else
                    {
                        numLen = 4;
                        goto digit3;
                    }
                }
            }
            d543 = (number / 1000) % 1000 * 3;

            int d876;
            if (number < 1000000000)
            {
                d876 = (number / 1000000) * 3;
                if (number >= 100000000)
                {
                    numLen = 9;
                    goto digit8;
                }
                else
                {
                    if (number >= 10000000)
                    {
                        numLen = 8;
                        goto digit7;
                    }
                    else
                    {
                        numLen = 7;
                        goto digit6;
                    }
                }
            }
            d876 = (number / 1000000) % 1000 * 3;

            numLen = 10;

            // uint is between 0 & 4,294,967,295 (in practice we only get to int.MaxValue, but that's the same # of digits)
            // so 1 to 10 digits

            // [01,]000,000-[99,]000,000
            var d9 = number / 1000000000;
            buffer[0] = (char)('0' + d9);

            digit8:
            buffer[1] = DigitTriplets[d876];
            digit7:
            buffer[2] = DigitTriplets[d876 + 1];
            digit6:
            buffer[3] = DigitTriplets[d876 + 2];

            digit5:
            buffer[4] = DigitTriplets[d543];
            digit4:
            buffer[5] = DigitTriplets[d543 + 1];
            digit3:
            buffer[6] = DigitTriplets[d543 + 2];

            buffer[7] = DigitTriplets[d012];
            buffer[8] = DigitTriplets[d012 + 1];
            buffer[9] = DigitTriplets[d012 + 2];

            writer.Write(buffer, 10 - numLen, numLen);
        }

        static readonly MethodInfo CustomWriteUInt_ThunkWriter = typeof(Methods).GetMethod("_CustomWriteUInt_ThunkWriter", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _CustomWriteUInt_ThunkWriter(ref ThunkWriter writer, uint number, char[] buffer)
        {
            var ptr = InlineSerializer<object>.CharBufferSize - 1;

            var copy = number;

            do
            {
                byte ix = (byte)(copy % 100);
                copy /= 100;

                var chars = DigitPairs[ix];
                buffer[ptr--] = chars.Second;
                buffer[ptr--] = chars.First;
            } while (copy != 0);

            if (buffer[ptr + 1] == '0')
            {
                ptr++;
            }

            writer.Write(buffer, ptr + 1, InlineSerializer<object>.CharBufferSize - 1 - ptr);
        }

        static readonly MethodInfo CustomWriteUIntUnrolled_ThunkWriter = typeof(Methods).GetMethod("_CustomWriteUIntUnrolled_ThunkWriter", BindingFlags.Static | BindingFlags.NonPublic);
        static void _CustomWriteUIntUnrolled_ThunkWriter(ref ThunkWriter writer, uint number, char[] buffer)
        {
            int numLen;

            if (number < 1000)
            {
                if (number >= 100)
                {
                    writer.Write(DigitTriplets, (int)(number * 3), 3);
                }
                else
                {
                    if (number >= 10)
                    {
                        writer.Write(DigitTriplets, (int)(number * 3 + 1), 2);
                    }
                    else
                    {
                        writer.Write(DigitTriplets, (int)(number * 3 + 2), 1);
                    }
                }
                return;
            }
            var d012 = number % 1000 * 3;

            uint d543;
            if (number < 1000000)
            {
                d543 = (number / 1000) * 3;
                if (number >= 100000)
                {
                    numLen = 6;
                    goto digit5;
                }
                else
                {
                    if (number >= 10000)
                    {
                        numLen = 5;
                        goto digit4;
                    }
                    else
                    {
                        numLen = 4;
                        goto digit3;
                    }
                }
            }
            d543 = (number / 1000) % 1000 * 3;

            uint d876;
            if (number < 1000000000)
            {
                d876 = (number / 1000000) * 3;
                if (number >= 100000000)
                {
                    numLen = 9;
                    goto digit8;
                }
                else
                {
                    if (number >= 10000000)
                    {
                        numLen = 8;
                        goto digit7;
                    }
                    else
                    {
                        numLen = 7;
                        goto digit6;
                    }
                }
            }
            d876 = (number / 1000000) % 1000 * 3;

            numLen = 10;

            // uint is between 0 & 4,294,967,295 (in practice we only get to int.MaxValue, but that's the same # of digits)
            // so 1 to 10 digits

            // [01,]000,000-[99,]000,000
            var d9 = number / 1000000000;
            buffer[0] = (char)('0' + d9);

            digit8:
            buffer[1] = DigitTriplets[d876];
            digit7:
            buffer[2] = DigitTriplets[d876 + 1];
            digit6:
            buffer[3] = DigitTriplets[d876 + 2];

            digit5:
            buffer[4] = DigitTriplets[d543];
            digit4:
            buffer[5] = DigitTriplets[d543 + 1];
            digit3:
            buffer[6] = DigitTriplets[d543 + 2];

            buffer[7] = DigitTriplets[d012];
            buffer[8] = DigitTriplets[d012 + 1];
            buffer[9] = DigitTriplets[d012 + 2];

            writer.Write(buffer, 10 - numLen, numLen);
        }

        static readonly MethodInfo CustomWriteLong_ThunkWriter = typeof(Methods).GetMethod("_CustomWriteLong_ThunkWriter", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _CustomWriteLong_ThunkWriter(ref ThunkWriter writer, long number, char[] buffer)
        {
            // Gotta special case this, we can't negate it
            if (number == long.MinValue)
            {
                writer.WriteMinConstant(ConstantString_Min.Long_MinValue);
                return;
            }

            var ptr = InlineSerializer<object>.CharBufferSize - 1;

            ulong copy;
            if (number < 0)
            {
                writer.Write('-');
                copy = (ulong)(-number);
            }
            else
            {
                copy = (ulong)number;
            }

            do
            {
                byte ix = (byte)(copy % 100);
                copy /= 100;

                var chars = DigitPairs[ix];
                buffer[ptr--] = chars.Second;
                buffer[ptr--] = chars.First;
            } while (copy != 0);

            if (buffer[ptr + 1] == '0')
            {
                ptr++;
            }

            writer.Write(buffer, ptr + 1, InlineSerializer<object>.CharBufferSize - 1 - ptr);
        }

        static readonly MethodInfo CustomWriteULong_ThunkWriter = typeof(Methods).GetMethod("_CustomWriteULong_ThunkWriter", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _CustomWriteULong_ThunkWriter(ref ThunkWriter writer, ulong number, char[] buffer)
        {
            var ptr = InlineSerializer<object>.CharBufferSize - 1;

            var copy = number;

            do
            {
                byte ix = (byte)(copy % 100);
                copy /= 100;

                var chars = DigitPairs[ix];
                buffer[ptr--] = chars.Second;
                buffer[ptr--] = chars.First;
            } while (copy != 0);

            if (buffer[ptr + 1] == '0')
            {
                ptr++;
            }

            writer.Write(buffer, ptr + 1, InlineSerializer<object>.CharBufferSize - 1 - ptr);
        }

        static readonly MethodInfo ProxyFloat_ThunkWriter = typeof(Methods).GetMethod("_ProxyFloat_ThunkWriter", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _ProxyFloat_ThunkWriter(ref ThunkWriter writer, float f)
        {
            writer.Write(f.ToString("R", CultureInfo.InvariantCulture));
        }

        static readonly MethodInfo ProxyDouble_ThunkWriter = typeof(Methods).GetMethod("_ProxyDouble_ThunkWriter", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _ProxyDouble_ThunkWriter(ref ThunkWriter writer, double d)
        {
            writer.Write(d.ToString("R", CultureInfo.InvariantCulture));
        }

        static readonly MethodInfo ProxyDecimal_ThunkWriter = typeof(Methods).GetMethod("_ProxyDecimal_ThunkWriter", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _ProxyDecimal_ThunkWriter(ref ThunkWriter writer, decimal d)
        {
            writer.Write(d);
        }

        public static readonly MethodInfo WriteTimeSpanISO8601_ThunkWriter = typeof(Methods).GetMethod("_WriteTimeSpanISO8601_ThunkWriter", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _WriteTimeSpanISO8601_ThunkWriter(ref ThunkWriter writer, TimeSpan ts, char[] buffer)
        {
            // can't negate this, have to handle it manually
            if (ts.Ticks == long.MinValue)
            {
                writer.Write("\"-P10675199DT2H48M5.4775808S\"");
                return;
            }

            writer.Write('"');

            if (ts.Ticks < 0)
            {
                writer.Write('-');
                ts = ts.Negate();
            }

            writer.Write('P');

            var days = ts.Days;
            var hours = ts.Hours;
            var minutes = ts.Minutes;
            var seconds = ts.Seconds;

            // days
            if (days > 0)
            {
                _CustomWriteIntUnrolledSigned_ThunkWriter(ref writer, days, buffer);
                writer.Write('D');
            }

            // time separator
            writer.Write('T');

            // hours
            if (hours > 0)
            {
                _CustomWriteIntUnrolledSigned_ThunkWriter(ref writer, hours, buffer);
                writer.Write('H');
            }

            // minutes
            if (minutes > 0)
            {
                _CustomWriteIntUnrolledSigned_ThunkWriter(ref writer, minutes, buffer);
                writer.Write('M');
            }

            // seconds
            _CustomWriteIntUnrolledSigned_ThunkWriter(ref writer, seconds, buffer);

            // fractional part
            {
                TwoDigits digits;
                int fracEnd;
                var endCount = 0;
                var remainingTicks = (ts - new TimeSpan(days, hours, minutes, seconds, 0)).Ticks;

                if (remainingTicks > 0)
                {
                    buffer[0] = '.';

                    var fracPart = remainingTicks % 100;
                    remainingTicks /= 100;
                    if (fracPart > 0)
                    {
                        digits = DigitPairs[fracPart];
                        buffer[7] = digits.Second;
                        buffer[6] = digits.First;
                        fracEnd = 8;
                    }
                    else
                    {
                        fracEnd = 6;
                    }

                    fracPart = remainingTicks % 100;
                    remainingTicks /= 100;
                    if (fracPart > 0)
                    {
                        digits = DigitPairs[fracPart];
                        buffer[5] = digits.Second;
                        buffer[4] = digits.First;
                    }
                    else
                    {
                        if (fracEnd == 6)
                        {
                            fracEnd = 4;
                        }
                        else
                        {
                            buffer[5] = '0';
                            buffer[4] = '0';
                        }
                    }

                    fracPart = remainingTicks % 100;
                    remainingTicks /= 100;
                    if (fracPart > 0)
                    {
                        digits = DigitPairs[fracPart];
                        buffer[3] = digits.Second;
                        buffer[2] = digits.First;
                    }
                    else
                    {
                        if (fracEnd == 4)
                        {
                            fracEnd = 2;
                        }
                        else
                        {
                            buffer[3] = '0';
                            buffer[2] = '0';
                        }
                    }

                    fracPart = remainingTicks;
                    buffer[1] = (char)('0' + fracPart);

                    endCount = fracEnd;
                }

                writer.Write(buffer, 0, endCount);
            }

            writer.Write("S\"");
        }

        static readonly MethodInfo WriteTimeSpanMicrosoft_ThunkWriter = typeof(Methods).GetMethod("_WriteTimeSpanMicrosoft_ThunkWriter", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _WriteTimeSpanMicrosoft_ThunkWriter(ref ThunkWriter writer, TimeSpan ts, char[] buffer)
        {
            // can't negate this, have to handle it manually
            if (ts.Ticks == long.MinValue)
            {
                writer.Write("\"-10675199.02:48:05.4775808\"");
                return;
            }

            writer.WriteFormattingConstant(ConstantString_Formatting.Quote);

            if (ts.Ticks < 0)
            {
                writer.Write('-');
                ts = ts.Negate();
            }

            var days = ts.Days;
            var hours = ts.Hours;
            var minutes = ts.Minutes;
            var secs = ts.Seconds;
            int endCount;

            TwoDigits digits;

            // days
            {
                if (days != 0)
                {
                    _CustomWriteInt_ThunkWriter(ref writer, days, buffer);
                    writer.Write('.');
                }
            }

            // hours
            {
                digits = DigitPairs[hours];
                buffer[0] = digits.First;
                buffer[1] = digits.Second;
            }

            buffer[2] = ':';

            // minutes
            {
                digits = DigitPairs[minutes];
                buffer[3] = digits.First;
                buffer[4] = digits.Second;
            }

            buffer[5] = ':';

            // seconds
            {
                digits = DigitPairs[secs];
                buffer[6] = digits.First;
                buffer[7] = digits.Second;
            }

            endCount = 8;

            // factional part
            {
                int fracEnd;
                var remainingTicks = (ts - new TimeSpan(ts.Days, ts.Hours, ts.Minutes, ts.Seconds, 0)).Ticks;
                if (remainingTicks > 0)
                {
                    buffer[8] = '.';

                    var fracPart = remainingTicks % 100;
                    remainingTicks /= 100;
                    if (fracPart > 0)
                    {
                        digits = DigitPairs[fracPart];
                        buffer[15] = digits.Second;
                        buffer[14] = digits.First;
                        fracEnd = 16;
                    }
                    else
                    {
                        fracEnd = 14;
                    }

                    fracPart = remainingTicks % 100;
                    remainingTicks /= 100;
                    if (fracPart > 0)
                    {
                        digits = DigitPairs[fracPart];
                        buffer[13] = digits.Second;
                        buffer[12] = digits.First;
                    }
                    else
                    {
                        if (fracEnd == 14)
                        {
                            fracEnd = 12;
                        }
                        else
                        {
                            buffer[13] = '0';
                            buffer[12] = '0';
                        }
                    }

                    fracPart = remainingTicks % 100;
                    remainingTicks /= 100;
                    if (fracPart > 0)
                    {
                        digits = DigitPairs[fracPart];
                        buffer[11] = digits.Second;
                        buffer[10] = digits.First;
                    }
                    else
                    {
                        if (fracEnd == 12)
                        {
                            fracEnd = 10;
                        }
                        else
                        {
                            buffer[11] = '0';
                            buffer[10] = '0';
                        }
                    }

                    fracPart = remainingTicks;
                    buffer[9] = (char)('0' + fracPart);

                    endCount = fracEnd;
                }
            }

            writer.Write(buffer, 0, endCount);

            writer.WriteFormattingConstant(ConstantString_Formatting.Quote);
        }

        static readonly MethodInfo CustomRFC1123_ThunkWriter = typeof(Methods).GetMethod("_CustomRFC1123_ThunkWriter", BindingFlags.NonPublic | BindingFlags.Static);
        static void _CustomRFC1123_ThunkWriter(ref ThunkWriter writer, DateTime dt)
        {
            writer.WriteFormattingConstant(ConstantString_Formatting.Quote);

            var dayOfWeek = (ConstantString_DaysOfWeek)(((byte)dt.DayOfWeek) * 3);
            writer.WriteDayOfWeek(dayOfWeek);
            writer.WriteFormattingConstant(ConstantString_Formatting.CommaSpace);

            {
                var day = DigitPairs[dt.Day];
                writer.Write(day.First);
                writer.Write(day.Second);
                writer.WriteFormattingConstant(ConstantString_Formatting.Space);
            }

            // compiles as a switch
            // not converted to a ConstantString_* enum because you can't pack these such that
            //   they fit in a cache line
            switch (dt.Month)
            {
                case 1: writer.Write("Jan "); break;
                case 2: writer.Write("Feb "); break;
                case 3: writer.Write("Mar "); break;
                case 4: writer.Write("Apr "); break;
                case 5: writer.Write("May "); break;
                case 6: writer.Write("Jun "); break;
                case 7: writer.Write("Jul "); break;
                case 8: writer.Write("Aug "); break;
                case 9: writer.Write("Sep "); break;
                case 10: writer.Write("Oct "); break;
                case 11: writer.Write("Nov "); break;
                case 12: writer.Write("Dec "); break;
            }

            {
                var year = dt.Year;
                var firstHalfYear = DigitPairs[year / 100];
                writer.Write(firstHalfYear.First);
                writer.Write(firstHalfYear.Second);

                var secondHalfYear = DigitPairs[year % 100];
                writer.Write(secondHalfYear.First);
                writer.Write(secondHalfYear.Second);
                writer.WriteFormattingConstant(ConstantString_Formatting.Space);
            }

            {
                var hour = DigitPairs[dt.Hour];
                writer.Write(hour.First);
                writer.Write(hour.Second);
                writer.WriteFormattingConstant(ConstantString_Formatting.Colon);
            }

            {
                var minute = DigitPairs[dt.Minute];
                writer.Write(minute.First);
                writer.Write(minute.Second);
                writer.WriteFormattingConstant(ConstantString_Formatting.Colon);
            }

            {
                var second = DigitPairs[dt.Second];
                writer.Write(second.First);
                writer.Write(second.Second);
            }

            writer.WriteValueConstant(ConstantString_Value.SpaceGMTQuote);
        }

        static readonly MethodInfo CustomWriteMicrosoftStyleWithOffset_ThunkWriter = typeof(Methods).GetMethod("_CustomWriteMicrosoftStyleWithOffset_ThunkWriter", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _CustomWriteMicrosoftStyleWithOffset_ThunkWriter(ref ThunkWriter writer, long utcTicks, int hours, int minutes, char[] buffer)
        {
            const long EpochTicks = 621355968000000000L;
            const long TicksToMilliseconds = 10000L;

            writer.WriteValueConstant(ConstantString_Value.Date);

            var delta = (utcTicks - EpochTicks) / TicksToMilliseconds;
            _CustomWriteLong_ThunkWriter(ref writer, delta, buffer);

            if (hours < 0 || minutes < 0)
            {
                writer.Write('-');
                hours = -hours;
                minutes = -minutes;
            }
            else
            {
                writer.Write('+');
            }

            var digits = DigitPairs[hours];
            writer.Write(digits.First);
            writer.Write(digits.Second);

            digits = DigitPairs[minutes];
            writer.Write(digits.First);
            writer.Write(digits.Second);

            writer.WriteValueConstant(ConstantString_Value.CloseDate);
        }
    }
}
