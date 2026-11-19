using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Serialize
{
    delegate void StringThunkDelegate<T>(ref ThunkWriter writer, T data, int depth);

    #region Strings and Enums and oh god why

    // Oh my god what is going one here?
    //
    // Basically I've embedded all the different common JSON strings
    //   into a bunch of losely grouped together arrays.
    //   Each of these arrays should fit in a single cache line (<= 64 bytes) 
    //   on a modern CPU.
    // 
    // Each array has a paired enum that is used to actually call 
    //   write on in the ThunkWriter.  Each enum value encodes 
    //   some index/length information (sometimes parts are inferred)
    //   which is used to pick where (and how much) of an array to copy
    //   The enums are all smaller than an long.

    enum ConstantString_Common : ushort
    {
        DoubleBackSlash = (0 << 8) | 2,
        EscapeSequence_2028 = (2 << 8) | 6,
        EscapeSequence_2029 = (8 << 8) | 6,
        EscapeSequence_b = (14 << 8) | 2,
        EscapeSequence_t = (16 << 8) | 2,
        EscapeSequence_n = (18 << 8) | 2,
        EscapeSequence_f = (20 << 8) | 2,
        EscapeSequence_r = (22 << 8) | 2
    }

    enum ConstantString_Formatting : ushort
    {
        Quote = (11 << 8) | 1,
        BackSlashQuote = (14 << 8) | 2,
        OpenCurlyBrace = (18 << 8) | 1,
        Comma = (16 << 8) | 1,
        CloseCurlyBrace = (19 << 8) | 1,
        OpenSquareBrace = (20 << 8) | 1,
        Space = (1 << 8) | 1,
        CommaSpace = (16 << 8) | 2,
        CloseSquareBrace = (21 << 8) | 1,
        QuoteColon = (11 << 8) | 2,
        QuoteColonSpace = (11 << 8) | 3,
        ColonSpace = (12 << 8) | 2,
        Colon = (12 << 8) | 1,
        NewLine = (0 << 8) | 1,
        NewLine1Space = (0 << 8) | 2,
        NewLine2Space = (0 << 8) | 3,
        NewLine3Space = (0 << 8) | 4,
        NewLine4Space = (0 << 8) | 5,
        NewLine5Space = (0 << 8) | 6,
        NewLine6Space = (0 << 8) | 7,
        NewLine7Space = (0 << 8) | 8,
        NewLine8Space = (0 << 8) | 9,
        NewLine9Space = (0 << 8) | 10,
        NewLine10Space = (0 << 8) | 11
    }

    enum ConstantString_Min : ushort
    {
        Int_MinValue = (0 << 8) | 11,
        Long_MinValue = (11 << 8) | 20
    }

    enum ConstantString_Value : ushort
    {
        Null = (0 << 8) | 4,
        Date = (4 << 8) | 8,
        CloseDate = (12 << 8) | 4,
        True = (16 << 8) | 4,
        False = (20 << 8) | 5,
        SpaceGMTQuote = (25 << 8) | 5
    }

    enum ConstantString_000Escape : byte
    {
        EscapeSequence_0000 = 0,
        EscapeSequence_0001 = 1,
        EscapeSequence_0002 = 2,
        EscapeSequence_0003 = 3,
        EscapeSequence_0004 = 4,
        EscapeSequence_0005 = 5,
        EscapeSequence_0006 = 6,
        EscapeSequence_0007 = 7,
        EscapeSequence_000B = 8,
        EscapeSequence_000E = 9,
        EscapeSequence_000F = 10
    }

    enum ConstantString_001Escape : byte
    {
        EscapeSequence_0010 = 0,
        EscapeSequence_0011 = 1,
        EscapeSequence_0012 = 2,
        EscapeSequence_0013 = 3,
        EscapeSequence_0014 = 4,
        EscapeSequence_0015 = 5,
        EscapeSequence_0016 = 6,
        EscapeSequence_0017 = 7,
        EscapeSequence_0018 = 8,
        EscapeSequence_0019 = 9,
        EscapeSequence_001A = 10,
        EscapeSequence_001B = 11,
        EscapeSequence_001C = 12,
        EscapeSequence_001D = 13,
        EscapeSequence_001E = 14,
        EscapeSequence_001F = 15
    }

    enum ConstantString_DaysOfWeek : byte
    {
        Sunday = 0,
        Monday = 3,
        Tuesday = 6,
        Wednesday = 9,
        Thursday = 12,
        Friday = 15,
        Saturday = 18
    }

    static class ThunkWriterCharArrays
    {
        public static readonly char[] Escape000Prefix = new char[] { '\\', 'u', '0', '0', '0' };
        public static readonly char[] Escape001Prefix = new char[] { '\\', 'u', '0', '0', '1' };

        public static readonly char[] ConstantString_Common_Chars = new char[] { '\\', '\\', '\\', 'u', '2', '0', '2', '8', '\\', 'u', '2', '0', '2', '9', '\\', 'b', '\\', 't', '\\', 'n', '\\', 'f', '\\', 'r' };
        public static readonly char[] ConstantString_Formatting_Chars = new char[] { '\n', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '"', ':', ' ', '\\', '"', ',', ' ', '{', '}', '[', ']' };
        public static readonly char[] ConstantString_Min_Chars = new char[] { '-', '2', '1', '4', '7', '4', '8', '3', '6', '4', '8', '-', '9', '2', '2', '3', '3', '7', '2', '0', '3', '6', '8', '5', '4', '7', '7', '5', '8', '0', '8' };
        public static readonly char[] ConstantString_Value_Chars = new char[] { 'n', 'u', 'l', 'l', '"', '\\', '/', 'D', 'a', 't', 'e', '(', ')', '\\', '/', '"', 't', 'r', 'u', 'e', 'f', 'a', 'l', 's', 'e', ' ', 'G', 'M', 'T', '"' };
        public static readonly char[] ConstantString_000Escape_Chars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', 'B', 'E', 'F' };
        public static readonly char[] ConstantString_001Escape_Chars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        public static readonly char[] ConstantString_DaysOfWeek = new char[] { 'S', 'u', 'n', 'M', 'o', 'n', 'T', 'u', 'e', 'W', 'e', 'd', 'T', 'h', 'u', 'F', 'r', 'i', 'S', 'a', 't' };
    }

    #endregion

    partial struct ThunkWriter
    {
        StringBuilder Builder;

        public static bool IsConstantCommonString(string str, out ConstantString_Common c)
        {
            switch (str)
            {
                case @"\\": c = ConstantString_Common.DoubleBackSlash; return true;
                case @"\u2028": c = ConstantString_Common.EscapeSequence_2028; return true;
                case @"\u2029": c = ConstantString_Common.EscapeSequence_2029; return true;
                case @"\b": c = ConstantString_Common.EscapeSequence_b; return true;
                case @"\t": c = ConstantString_Common.EscapeSequence_t; return true;
                case @"\n": c = ConstantString_Common.EscapeSequence_n; return true;
                case @"\f": c = ConstantString_Common.EscapeSequence_f; return true;
                case @"\r": c = ConstantString_Common.EscapeSequence_r; return true;
                default: c = 0; return false;
            }
        }

        public static bool IsConstantFormattingString(string str, out ConstantString_Formatting c)
        {
            switch(str)
            {
                case "\"": c = ConstantString_Formatting.Quote; return true;
                case "\\\"": c =  ConstantString_Formatting.BackSlashQuote; return true;
                case "{": c =  ConstantString_Formatting.OpenCurlyBrace; return true;
                case ",": c = ConstantString_Formatting.Comma; return true;
                case "}": c = ConstantString_Formatting.CloseCurlyBrace; return true;
                case "[": c = ConstantString_Formatting.OpenSquareBrace; return true;
                case " ": c = ConstantString_Formatting.Space; return true;
                case ", ": c = ConstantString_Formatting.CommaSpace; return true;
                case "]": c = ConstantString_Formatting.CloseSquareBrace; return true;
                case "\":": c = ConstantString_Formatting.QuoteColon; return true;
                case "\": ": c = ConstantString_Formatting.QuoteColonSpace; return true;
                case ": ": c = ConstantString_Formatting.ColonSpace; return true;
                case ":": c = ConstantString_Formatting.Colon; return true;
                case "\n": c = ConstantString_Formatting.NewLine; return true;
                case "\n ": c = ConstantString_Formatting.NewLine1Space; return true;
                case "\n  ": c = ConstantString_Formatting.NewLine2Space; return true;
                case "\n   ": c = ConstantString_Formatting.NewLine3Space; return true;
                case "\n    ": c = ConstantString_Formatting.NewLine4Space; return true;
                case "\n     ": c = ConstantString_Formatting.NewLine5Space; return true;
                case "\n      ": c = ConstantString_Formatting.NewLine6Space; return true;
                case "\n       ": c = ConstantString_Formatting.NewLine7Space; return true;
                case "\n        ": c = ConstantString_Formatting.NewLine8Space; return true;
                case "\n         ": c = ConstantString_Formatting.NewLine9Space; return true;
                case "\n          ": c = ConstantString_Formatting.NewLine10Space; return true;
                default: c = 0; return false;
            }
        }

        public static bool IsConstantMinString(string str, out ConstantString_Min c)
        {
            switch (str)
            {
                case "-2147483648": c = ConstantString_Min.Int_MinValue; return true;
                case "-9223372036854775808": c = ConstantString_Min.Long_MinValue; return true;
                default: c = 0; return false;
            }
        }

        public static bool IsConstantValueString(string str, out ConstantString_Value c)
        {
            switch (str)
            {
                case "null": c = ConstantString_Value.Null; return true;
                case "\"\\/Date(": c = ConstantString_Value.Date; return true;
                case ")\\/\"": c = ConstantString_Value.CloseDate; return true;
                case "true": c = ConstantString_Value.True; return true;
                case "false": c = ConstantString_Value.False; return true;
                case " GMT\"": c = ConstantString_Value.SpaceGMTQuote; return true;
                default: c = 0; return false;
            }
        }

        public static bool IsConstant000EscapeString(string str, out ConstantString_000Escape c)
        {
            switch (str)
            {
                case @"\u0000": c = ConstantString_000Escape.EscapeSequence_0000; return true;
                case @"\u0001": c = ConstantString_000Escape.EscapeSequence_0001; return true;
                case @"\u0002": c = ConstantString_000Escape.EscapeSequence_0002; return true;
                case @"\u0003": c = ConstantString_000Escape.EscapeSequence_0003; return true;
                case @"\u0004": c = ConstantString_000Escape.EscapeSequence_0004; return true;
                case @"\u0005": c = ConstantString_000Escape.EscapeSequence_0005; return true;
                case @"\u0006": c = ConstantString_000Escape.EscapeSequence_0006; return true;
                case @"\u0007": c = ConstantString_000Escape.EscapeSequence_0007; return true;
                case @"\u000B": c = ConstantString_000Escape.EscapeSequence_000B; return true;
                case @"\u000E": c = ConstantString_000Escape.EscapeSequence_000E; return true;
                case @"\u000F": c = ConstantString_000Escape.EscapeSequence_000F; return true;
                default: c = 0; return false;
            }
        }

        public static bool IsConstant001EscapeString(string str, out ConstantString_001Escape c)
        {
            switch (str)
            {
                case @"\u0010": c = ConstantString_001Escape.EscapeSequence_0010; return true;
                case @"\u0011": c = ConstantString_001Escape.EscapeSequence_0011; return true;
                case @"\u0012": c = ConstantString_001Escape.EscapeSequence_0012; return true;
                case @"\u0013": c = ConstantString_001Escape.EscapeSequence_0013; return true;
                case @"\u0014": c = ConstantString_001Escape.EscapeSequence_0014; return true;
                case @"\u0015": c = ConstantString_001Escape.EscapeSequence_0015; return true;
                case @"\u0016": c = ConstantString_001Escape.EscapeSequence_0016; return true;
                case @"\u0017": c = ConstantString_001Escape.EscapeSequence_0017; return true;
                case @"\u0018": c = ConstantString_001Escape.EscapeSequence_0018; return true;
                case @"\u0019": c = ConstantString_001Escape.EscapeSequence_0019; return true;
                case @"\u001A": c = ConstantString_001Escape.EscapeSequence_001A; return true;
                case @"\u000B": c = ConstantString_001Escape.EscapeSequence_001B; return true;
                case @"\u000C": c = ConstantString_001Escape.EscapeSequence_001C; return true;
                case @"\u000D": c = ConstantString_001Escape.EscapeSequence_001D; return true;
                case @"\u000E": c = ConstantString_001Escape.EscapeSequence_001E; return true;
                case @"\u000F": c = ConstantString_001Escape.EscapeSequence_001F; return true;
                default: c = 0; return false;
            }
        }

        public static bool IsConstantDaysOfWeek(string str, out ConstantString_DaysOfWeek c)
        {
            switch (str)
            {
                case "Sun": c = ConstantString_DaysOfWeek.Sunday; return true;
                case "Mon": c = ConstantString_DaysOfWeek.Monday; return true;
                case "Tue": c = ConstantString_DaysOfWeek.Tuesday; return true;
                case "Wed": c = ConstantString_DaysOfWeek.Wednesday; return true;
                case "Thu": c = ConstantString_DaysOfWeek.Thursday; return true;
                case "Fri": c = ConstantString_DaysOfWeek.Friday; return true;
                case "Sat": c = ConstantString_DaysOfWeek.Saturday; return true;
                default: c = 0; return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Init()
        {
            Builder = (Builder ?? new StringBuilder()).Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(float f)
        {
            Write(f.ToString(CultureInfo.InvariantCulture));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(double d)
        {
            Write(d.ToString(CultureInfo.InvariantCulture));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(decimal m)
        {
            Write(m.ToString(CultureInfo.InvariantCulture));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(char[] ch, int startIx, int len)
        {
            Builder.Append(ch, startIx, len);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(char ch)
        {
            Builder.Append(ch);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteCommonConstant(ConstantString_Common str)
        {
            var asUShort = (ushort)(ConstantString_Common)str;
            var ix = asUShort >> 8;
            var size = asUShort & 0xFF;

            Builder.Append(ThunkWriterCharArrays.ConstantString_Common_Chars, ix, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFormattingConstant(ConstantString_Formatting str)
        {
            var asUShort = (ushort)str;
            var ix = (asUShort >> 8);
            var len = asUShort & 0xFF;

            Builder.Append(ThunkWriterCharArrays.ConstantString_Formatting_Chars, ix, len);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteMinConstant(ConstantString_Min str)
        {
            var asUShort = (ushort)str;
            var ix = (asUShort >> 8);
            var len = asUShort & 0xFF;

            Builder.Append(ThunkWriterCharArrays.ConstantString_Min_Chars, ix, len);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteValueConstant(ConstantString_Value str)
        {
            var asUShort = (ushort)str;
            var ix = (asUShort >> 8);
            var len = asUShort & 0xFF;

            Builder.Append(ThunkWriterCharArrays.ConstantString_Value_Chars, ix, len);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write000EscapeConstant(ConstantString_000Escape str)
        {
            var ix = (byte)str;

            Builder.Append(ThunkWriterCharArrays.Escape000Prefix);
            Builder.Append(ThunkWriterCharArrays.ConstantString_000Escape_Chars[ix]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write001EscapeConstant(ConstantString_001Escape str)
        {
            var ix = (byte)str;

            Builder.Append(ThunkWriterCharArrays.Escape001Prefix);
            Builder.Append(ThunkWriterCharArrays.ConstantString_001Escape_Chars[ix]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDayOfWeek(ConstantString_DaysOfWeek str)
        {
            var ix = (byte)str;
            Builder.Append(ThunkWriterCharArrays.ConstantString_DaysOfWeek, ix, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(string strRef)
        {
            Builder.Append(strRef);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string StaticToString()
        {
            return Builder.ToString();
        }

        #region Slow Builds Only
        // these methods are only called to compare faster methods to serializing
        //   as such they need not be optimized

        public void Write(byte b)
        {
            Write(b.ToString());
        }
        public void Write(sbyte b)
        {
            Write(b.ToString());
        }
        public void Write(short b)
        {
            Write(b.ToString());
        }
        public void Write(ushort b)
        {
            Write(b.ToString());
        }
        public void Write(int b)
        {
            Write(b.ToString());
        }
        public void Write(uint b)
        {
            Write(b.ToString());
        }
        public void Write(long b)
        {
            Write(b.ToString());
        }
        public void Write(ulong b)
        {
            Write(b.ToString());
        }
        #endregion
    }
}
