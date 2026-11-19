using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jil.Common;
using Sigil.NonGeneric;
using System.Reflection;
using System.Globalization;

namespace Jil.Deserialize
{
    class InlineDeserializer<ForType>
    {
        public static bool UseFastFloatingPointMethods = true;
        public static bool UseCharArrayOverStringBuilder = true;
        public static bool UseNameAutomata = true;
        public static bool UseNameAutomataForEnums = true;
        public static bool UseNameAutomataSwitches = true;
        public static bool UseNameAutomataBinarySearch = true;
        public static bool UseFastRFC1123Method = true;
        public static bool UseFastUnionLookup = true;

        const string CharBufferName = "char_buffer";
        const string StringBuilderName = "string_builder";

        readonly Type OptionsType;
        readonly DateTimeFormat DateFormat;
        readonly SerializationNameFormat SerializationNameFormat;
        readonly bool ReadingFromString;
        readonly bool PreferIndirectSerialization;
        readonly bool AllPrimitivesThroughHelpers;

        bool UsingCharBuffer;
        HashSet<Type> IndirectTypes;

        Emit Emit;

        int NextName;

        public InlineDeserializer(
            Type optionsType, 
            DateTimeFormat dateFormat, 
            SerializationNameFormat serializationNameFormat, 
            bool readingFromString,
            bool preferIndirectSerialization,
            bool allPrimitivesThroughHelpers
        )
        {
            OptionsType = optionsType;
            DateFormat = dateFormat;
            SerializationNameFormat = serializationNameFormat;
            ReadingFromString = readingFromString;

            PreferIndirectSerialization = preferIndirectSerialization;
            AllPrimitivesThroughHelpers = allPrimitivesThroughHelpers;

            NextName = 1;
        }

        string GetNextName()
        {
            var ret = "id_n" + NextName;
            NextName++;

            return ret;
        }

        void SetProperty(PropertyInfo prop)
        {
            // Top of stack
            // value
            // object(*?)

            var setMtd = prop.SetMethod;

            if (setMtd.IsVirtual)
            {
                Emit.CallVirtual(setMtd);
            }
            else
            {
                Emit.Call(setMtd);
            }
        }

        void AddGlobalVariables()
        {
            var involvedTypes = typeof(ForType).InvolvedTypes();

            var hasStringyTypes =
                involvedTypes.Contains(typeof(string)) ||
                involvedTypes.Any(t => t.IsUserDefinedType());  // for member names

            var needsCharBuffer =
                hasStringyTypes ||
                involvedTypes.Any(t => t.IsNumberType()) ||         // we use `ref char[]` for these, so they're kind of stringy
                (involvedTypes.Contains(typeof(DateTime)) && DateFormat == DateTimeFormat.ISO8601) ||
                (involvedTypes.Contains(typeof(TimeSpan)) && (DateFormat == DateTimeFormat.ISO8601 || DateFormat == DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch));

            if (needsCharBuffer)
            {
                UsingCharBuffer = true;

                Emit.DeclareLocal<char[]>(CharBufferName);

                Emit.LoadConstant(Methods.CharBufferSize);  // int
                Emit.NewArray<char>();                      // char[]
                Emit.StoreLocal(CharBufferName);            // --empty--
            }

            // we can't know, for sure, that a StringBuilder will be needed w/o seeing the data
            //   but we can save the slot on the stack in rare cases
            var mayNeedStringBuilder =
                involvedTypes.Contains(typeof(string)) ||
                involvedTypes.Contains(typeof(float)) ||
                involvedTypes.Contains(typeof(double)) ||
                involvedTypes.Contains(typeof(decimal)) ||
                involvedTypes.Any(t => t.IsEnum()) ||
                involvedTypes.Any(t => t.IsUserDefinedType()) ||
                (!UseNameAutomataForEnums && involvedTypes.Any(t => t.IsEnum()));

            if (mayNeedStringBuilder)
            {
                var gonnaUseAStringBuilderAnyway = (!UseNameAutomataForEnums && involvedTypes.Any(t => t.IsEnum()));

                if (!UseCharArrayOverStringBuilder || gonnaUseAStringBuilderAnyway)
                {
                    Emit.DeclareLocal<StringBuilder>(StringBuilderName);
                }
            }
        }

        void LoadCharBufferAddress()
        {
            Emit.LoadLocalAddress(CharBufferName);
        }

        void LoadCharBuffer()
        {
            Emit.LoadLocal(CharBufferName);
        }

        void LoadStringBuilder()
        {
            Emit.LoadLocalAddress(StringBuilderName);
        }

        static MethodInfo TextReader_Read = typeof(TextReader).GetMethod("Read", Type.EmptyTypes);
        static MethodInfo ThunkReader_Read = typeof(ThunkReader).GetMethod("Read", Type.EmptyTypes);
        void ReadCharFromStream()
        {
            Emit.LoadArgument(0);                   // (TextReader|ref ThunkReader)
            if (ReadingFromString)
            {
                Emit.Call(ThunkReader_Read);        // int
            }
            else
            {
                Emit.CallVirtual(TextReader_Read);  // int
            }
        }


        void RawReadChar(Action endOfStream)
        {
            var haveChar = Emit.DefineLabel(GetNextName());

            ReadCharFromStream();                       // int
            Emit.Duplicate();                           // int int
            Emit.LoadConstant(-1);                      // int int -1
            Emit.UnsignedBranchIfNotEqual(haveChar);    // int
            Emit.Pop();                                 // --empty--
            endOfStream();                              // --empty--

            Emit.MarkLabel(haveChar);                   // int
        }

        void ThrowExpectedButEnded(char c)
        {
            ThrowExpectedButEnded("" + c);
        }

        static readonly ConstructorInfo DeserializationException_Cons_string_ThunkWriter_bool = typeof(DeserializationException).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(string), typeof(ThunkReader).MakeByRefType(), typeof(bool) });
        static readonly ConstructorInfo DeserializationException_Cons_string_TextReader_bool = typeof(DeserializationException).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(string), typeof(TextReader), typeof(bool) });
        void ThrowStringStreamBool()
        {
            // top of stack is:
            // string
            // (TextReader|ref ThunkReader)
            // bool

            if (ReadingFromString)
            {
                Emit.NewObject(DeserializationException_Cons_string_ThunkWriter_bool);          // DeserializationException
            }
            else
            {
                Emit.NewObject(DeserializationException_Cons_string_TextReader_bool);           // DeserializationException
            }
        }

        void ThrowExpectedButEnded(params object[] ps)
        {
            Emit.LoadConstant("Expected: " + string.Join(", ", ps) + "; but the reader ended"); // string
            Emit.LoadArgument(0);                                                               // string TextReader
            Emit.LoadConstant(true);                                                            // string TextReader bool
            ThrowStringStreamBool();                                                            // DeserializationException
            Emit.Throw();                                                                       // --empty--
        }

        void ThrowExpectedButEnded(string s)
        {
            Emit.LoadConstant("Expected character: '" + s + "', but the reader ended"); // string
            Emit.LoadArgument(0);                                                       // string TextReader
            Emit.LoadConstant(true);                                                    // string TextReader bool
            ThrowStringStreamBool();                                                    // DeserializationException
            Emit.Throw();                                                               // --empty--
        }

        void ThrowExpected(char c)
        {
            Emit.LoadConstant("Expected character: '" + c + "'");                   // string
            Emit.LoadArgument(0);                                                   // string TextReader
            Emit.LoadConstant(false);                                               // string TextReader bool
            ThrowStringStreamBool();                                                // DeserializationException
            Emit.Throw();                                                           // --empty--
        }

        void ThrowIfEmptyAndWasExpecting(params object[] ps)
        {
            // top of stack:
            // int

            var notEmpty = Emit.DefineLabel(GetNextName());

            Emit.Duplicate();                           // int int
            Emit.LoadConstant(-1);                      // int int -1
            Emit.UnsignedBranchIfNotEqual(notEmpty);    // int

            Emit.Pop();                                                                         // --empty--
            Emit.LoadConstant("Expected: " + string.Join(", ", ps) + "; but the reader ended"); // string
            Emit.LoadArgument(0);                                                               // string TextReader
            Emit.LoadConstant(true);                                                            // string TextReader bool
            ThrowStringStreamBool();                                                            // DeserializationException
            Emit.Throw();                                                                       // --empty--

            Emit.MarkLabel(notEmpty);                   // int
        }

        void ThrowExpected(params object[] ps)
        {
            Emit.LoadConstant("Expected: " + string.Join(", ", ps));    // string
            Emit.LoadArgument(0);                                       // string TextReader
            Emit.LoadConstant(false);                                   // string TextReader bool
            ThrowStringStreamBool();                                    // DeserializationException
            Emit.Throw();                                               // --empty--
        }

        void ThrowExpected(char[] cs)
        {
            var ps = cs.Cast<object>().ToArray();
            ThrowExpected(ps);
        }

        void ExpectChar(char c)
        {
            var gotRightChar = Emit.DefineLabel(GetNextName());
            var gotAChar = Emit.DefineLabel(GetNextName());

            ReadCharFromStream();                       // int
            Emit.LoadConstant((int)c);                  // int int
            Emit.Duplicate();                           // int int int
            Emit.LoadConstant(-1);                      // int int int -1
            Emit.UnsignedBranchIfNotEqual(gotAChar);    // int int
            ThrowExpectedButEnded(c);                   // int int

            Emit.MarkLabel(gotAChar);                   // --empty--
            Emit.BranchIfEqual(gotRightChar);           // --empty--
            ThrowExpected(c);                           // --empty--

            Emit.MarkLabel(gotRightChar);               // --empty--
        }

        void CheckChar(char c)
        {
            var gotRightChar = Emit.DefineLabel(GetNextName());
            var gotAChar = Emit.DefineLabel(GetNextName());

            Emit.Duplicate();                           // int int
            Emit.LoadConstant(-1);                      // int int -1
            Emit.UnsignedBranchIfNotEqual(gotAChar);    // int
            ThrowExpectedButEnded(c);                   // --empty--

            Emit.MarkLabel(gotAChar);                   // int
            Emit.LoadConstant((int)c);                  // int int
            Emit.BranchIfEqual(gotRightChar);           // --empty--
            ThrowExpected(c);                           // --empty--

            Emit.MarkLabel(gotRightChar);                // --empty--
        }

        void ExpectQuote()
        {
            ExpectChar('"');
        }

        void CheckQuote()
        {
            CheckChar('"');
        }

        void ExpectRawCharOrNull(char c, Action ifChar, Action ifNull)
        {
            var gotChar = Emit.DefineLabel(GetNextName());
            var gotN = Emit.DefineLabel(GetNextName());
            var done = Emit.DefineLabel(GetNextName());

            RawReadChar(() => ThrowExpectedButEnded(c, "null"));    // int
            Emit.Duplicate();                                       // int int
            Emit.LoadConstant((int)c);                              // int int int
            Emit.BranchIfEqual(gotChar);                            // int 
            Emit.LoadConstant((int)'n');                            // int n
            Emit.BranchIfEqual(gotN);                               // --empty--
            ThrowExpected(c, "null");                               // --empty--

            Emit.MarkLabel(gotChar);                        // int
            Emit.Pop();                                     // --empty--
            ifChar();                                       // ???
            Emit.Branch(done);                              // ???

            Emit.MarkLabel(gotN);                           // --empty--
            ExpectChar('u');                                // --empty--
            ExpectChar('l');                                // --empty--
            ExpectChar('l');                                // --empty--
            ifNull();                                       // ???

            Emit.MarkLabel(done);                           // --empty--
        }

        void ExpectQuoteOrNull(Action ifQuote, Action ifNull)
        {
            ExpectRawCharOrNull('"', ifQuote, ifNull);
        }

        void ReadChar()
        {
            ExpectQuote();          // --empty--
            ReadEncodedChar();      // char
            ExpectQuote();          // char
        }

        void ReadEncodedChar()
        {
            Emit.LoadArgument(0);                                       // TextReader
            Emit.Call(Methods.GetReadEncodedChar(ReadingFromString));   // char
        }

        void CallReadEncodedString()
        {
            // Stack starts
            // TextReader

            if (UseCharArrayOverStringBuilder)
            {
                LoadCharBufferAddress();                                                    // TextReader char[]
                Emit.Call(Methods.GetReadEncodedStringWithCharArray(ReadingFromString));    // string
            }
            else
            {
                if (UsingCharBuffer)
                {
                    LoadCharBuffer();                                                       // TextReader char[]
                    LoadStringBuilder();                                                    // TextReader char[] StringBuilder
                    Emit.Call(Methods.GetReadEncodedStringWithBuffer(ReadingFromString));   // string
                }
                else
                {
                    LoadStringBuilder();                                        // TextReader StringBuilder
                    Emit.Call(Methods.GetReadEncodedString(ReadingFromString)); // string
                }
            }
        }

        void ReadString()
        {
            ExpectQuoteOrNull(
                delegate
                {
                    // --empty--
                    Emit.LoadArgument(0);                   // TextReader
                    CallReadEncodedString();                // string
                },
                delegate
                {
                    Emit.LoadNull();                        // null
                }
            );
        }

        void ReadNumber(Type numberType)
        {
            Emit.LoadArgument(0);               // TextReader

            if (numberType == typeof(byte))
            {
                Emit.Call(Methods.GetReadUInt8(ReadingFromString));     // byte
                return;
            }

            if (numberType == typeof(sbyte))
            {
                Emit.Call(Methods.GetReadInt8(ReadingFromString));      // sbyte
                return;
            }

            if (numberType == typeof(short))
            {
                Emit.Call(Methods.GetReadInt16(ReadingFromString));     // short
                return;
            }

            if (numberType == typeof(ushort))
            {
                Emit.Call(Methods.GetReadUInt16(ReadingFromString));    // ushort
                return;
            }

            if (numberType == typeof(int))
            {
                Emit.Call(Methods.GetReadInt32(ReadingFromString));     // int
                return;
            }

            if (numberType == typeof(uint))
            {
                Emit.Call(Methods.GetReadUInt32(ReadingFromString));    // uint
                return;
            }

            if (numberType == typeof(long))
            {
                Emit.Call(Methods.GetReadInt64(ReadingFromString));     // long
                return;
            }

            if (numberType == typeof(ulong))
            {
                Emit.Call(Methods.GetReadUInt64(ReadingFromString));    // ulong
                return;
            }

            if (UseFastFloatingPointMethods)
            {
                LoadCharBufferAddress();                    // TextReader char[]

                if (numberType == typeof(double))
                {
                    Emit.Call(Methods.GetReadDoubleFast(ReadingFromString));   // double
                    return;
                }

                if (numberType == typeof(float))
                {
                    Emit.Call(Methods.GetReadSingleFast(ReadingFromString));      // float
                    return;
                }

                if (numberType == typeof(decimal))
                {
                    Emit.Call(Methods.GetReadDecimalFast(ReadingFromString)); // decimal
                    return;
                }
            }
            else
            {
                if (UseCharArrayOverStringBuilder)
                {
                    LoadCharBufferAddress();                    // TextReader char[]

                    if (numberType == typeof(double))
                    {
                        Emit.Call(Methods.GetReadDoubleCharArray(ReadingFromString));   // double
                        return;
                    }

                    if (numberType == typeof(float))
                    {
                        Emit.Call(Methods.GetReadSingleCharArray(ReadingFromString));  // float
                        return;
                    }

                    if (numberType == typeof(decimal))
                    {
                        Emit.Call(Methods.GetReadDecimalCharArray(ReadingFromString)); // decimal
                        return;
                    }
                }
                else
                {
                    LoadStringBuilder();                    // TextReader StringBuilder

                    if (numberType == typeof(double))
                    {
                        Emit.Call(Methods.GetReadDouble(ReadingFromString));   // double
                        return;
                    }

                    if (numberType == typeof(float))
                    {
                        Emit.Call(Methods.GetReadSingle(ReadingFromString));  // float
                        return;
                    }

                    if (numberType == typeof(decimal))
                    {
                        Emit.Call(Methods.GetReadDecimal(ReadingFromString)); // decimal
                        return;
                    }
                }
            }

            throw new ConstructionException("Unexpected number type: " + numberType);
        }

        void ReadBool()
        {
            if(AllPrimitivesThroughHelpers)
            {
                Emit.LoadArgument(0);
                Emit.Call(Methods.GetReadBool(ReadingFromString));  // bool
                return;
            }

            var endOfStream = Emit.DefineLabel(GetNextName());
            var mayBeTrue = Emit.DefineLabel(GetNextName());
            var mayBeFalse = Emit.DefineLabel(GetNextName());
            var done = Emit.DefineLabel(GetNextName());

            RawReadChar(() => Emit.Branch(endOfStream));    // int
            Emit.Duplicate();                               // int int
            Emit.LoadConstant('t');                         // int int 't'
            Emit.BranchIfEqual(mayBeTrue);                  // int
            Emit.LoadConstant('f');                         // int 'f'
            Emit.BranchIfEqual(mayBeFalse);                 // --empty--

            // end of stream **AND** not true or false case
            Emit.MarkLabel(endOfStream);                    // --empty--
            ThrowExpectedButEnded("true or false");         // --empty--

            Emit.MarkLabel(mayBeTrue);      // int
            Emit.Pop();                     // --empty--
            ExpectChar('r');                // --empty--
            ExpectChar('u');                // --empty--
            ExpectChar('e');                // --empty--

            Emit.LoadConstant(true);        // bool
            Emit.Branch(done);              // bool

            Emit.MarkLabel(mayBeFalse);     // --empty--
            ExpectChar('a');                // --empty--
            ExpectChar('l');                // --empty--
            ExpectChar('s');                // --empty--
            ExpectChar('e');                // --empty--

            Emit.LoadConstant(false);       // bool

            Emit.MarkLabel(done);           // bool
        }

        void ReadGuid()
        {
            ExpectQuote();
            Emit.LoadArgument(0);                               // TextReader
            Emit.Call(Methods.GetReadGuid(ReadingFromString));  // Guid
            ExpectQuote();                                      // Guid
        }

        static readonly ConstructorInfo DateTimeOffsetConst = typeof(DateTimeOffset).GetConstructor(new[] { typeof(DateTime) });
        void ReadDateTimeOffset()
        {
            if (DateFormat == DateTimeFormat.MillisecondsSinceUnixEpoch ||
                DateFormat == DateTimeFormat.SecondsSinceUnixEpoch)
            {
                // All of these formats lack a timezone offset
                // So let's just reuse the DateTime reader
                ReadDate();                             // DateTime
                Emit.NewObject(DateTimeOffsetConst);    // DateTimeOffset
                return;
            }

            if (DateFormat == DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch)
            {
                ReadMicrosoftDateTimeOffset();  // DateTimeOffset;
                return;
            }

            ExpectQuote();                      // --empty--
            Emit.LoadArgument(0);               // TextReader
            if (UseCharArrayOverStringBuilder)
            {
                LoadCharBufferAddress();
                Emit.Call(Methods.GetReadISO8601DateWithOffsetWithCharArray(ReadingFromString));    // DateTimeOffset
                ExpectQuote();                                                                      // DateTimeOffset
            }
            else
            {
                LoadCharBuffer();
                Emit.Call(Methods.GetReadISO8601DateWithOffset(ReadingFromString)); // DateTimeOffset
                ExpectQuote();                                                      // DateTimeOffset
            }
        }

        void ReadTimeSpan()
        {
            switch (DateFormat)
            {
                case DateTimeFormat.SecondsSinceUnixEpoch: ReadSecondsTimeSpan(); break;
                case DateTimeFormat.MillisecondsSinceUnixEpoch: ReadMillisecondsTimeSpan(); break;
                case DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch: ReadMicrosoftStyleTimeSpan(); break;
                case DateTimeFormat.ISO8601: ReadISO8601TimeSpan(); break;
                default: throw new Exception("Unexpected DateTimeFormat: " + DateFormat);
            }
        }

        static readonly MethodInfo TimeSpan_FromSeconds = typeof(TimeSpan).GetMethod("FromSeconds", new[] { typeof(double) });
        void ReadSecondsTimeSpan()
        {
            const double TicksPerSecond = 10000000;

            var maxSecs = TimeSpan.MaxValue.TotalSeconds;
            var minSecs = TimeSpan.MinValue.TotalSeconds;

            var maxTicks = TimeSpan.MaxValue.Ticks;
            var minTicks = TimeSpan.MinValue.Ticks;

            var isMax = Emit.DefineLabel(GetNextName());
            var isMin = Emit.DefineLabel(GetNextName());
            var done = Emit.DefineLabel(GetNextName());

            ReadPrimitive(typeof(double));          // double
            Emit.Duplicate();                       // double double
            Emit.LoadConstant(maxSecs);             // double double double
            Emit.BranchIfGreaterOrEqual(isMax);     // double

            Emit.Duplicate();                       // double double
            Emit.LoadConstant(minSecs);             // double double double
            Emit.BranchIfLessOrEqual(isMin);        // double

            Emit.LoadConstant(TicksPerSecond);      // double double
            Emit.Multiply();                        // double
            Emit.Convert<long>();                   // long
            Emit.NewObject<TimeSpan, long>();       // TimeSpan
            Emit.Branch(done);                      // TimeSpan

            Emit.MarkLabel(isMax);                  // double
            Emit.Pop();                             // --empty--
            Emit.LoadConstant(maxTicks);            // long
            Emit.NewObject<TimeSpan, long>();       // TimeSpan
            Emit.Branch(done);                      // TimeSpan

            Emit.MarkLabel(isMin);                  // double
            Emit.Pop();                             // --empty--
            Emit.LoadConstant(minTicks);            // long
            Emit.NewObject<TimeSpan, long>();       // TimeSpan

            Emit.MarkLabel(done);                   // TimeSpan
        }

        void ReadMillisecondsTimeSpan()
        {
            const double TicksPerMillisecond = 10000;

            var maxMs = TimeSpan.MaxValue.TotalMilliseconds;
            var minMs = TimeSpan.MinValue.TotalMilliseconds;

            var maxTicks = TimeSpan.MaxValue.Ticks;
            var minTicks = TimeSpan.MinValue.Ticks;

            var isMax = Emit.DefineLabel(GetNextName());
            var isMin = Emit.DefineLabel(GetNextName());
            var done = Emit.DefineLabel(GetNextName());

            ReadPrimitive(typeof(double));          // double
            Emit.Duplicate();                       // double double
            Emit.LoadConstant(maxMs);               // double double double
            Emit.BranchIfGreaterOrEqual(isMax);     // double

            Emit.Duplicate();                       // double double
            Emit.LoadConstant(minMs);               // double double double
            Emit.BranchIfLessOrEqual(isMin);        // double

            Emit.LoadConstant(TicksPerMillisecond); // double double
            Emit.Multiply();                        // double
            Emit.Convert<long>();                   // long
            Emit.NewObject<TimeSpan, long>();       // TimeSpan
            Emit.Branch(done);                      // TimeSpan

            Emit.MarkLabel(isMax);                  // double
            Emit.Pop();                             // --empty--
            Emit.LoadConstant(maxTicks);            // long
            Emit.NewObject<TimeSpan, long>();       // TimeSpan
            Emit.Branch(done);                      // TimeSpan

            Emit.MarkLabel(isMin);                  // double
            Emit.Pop();                             // --empty--
            Emit.LoadConstant(minTicks);            // long
            Emit.NewObject<TimeSpan, long>();       // TimeSpan

            Emit.MarkLabel(done);                   // TimeSpan
        }

        void ReadMicrosoftStyleTimeSpan()
        {
            Emit.LoadArgument(0);                                               // TextReader
            LoadCharBuffer();                                                   // TextReader char[]
            Emit.Call(Methods.GetReadMicrosoftTimeSpan(ReadingFromString));    // TimeSpan
        }

        void ReadISO8601TimeSpan()
        {
            Emit.LoadArgument(0);                                           // TextReader
            LoadCharBuffer();                                               // TextReader char[]
            Emit.Call(Methods.GetReadISO8601TimeSpan(ReadingFromString));   // TimeSpan
        }

        void ReadDate()
        {
            switch (DateFormat)
            {
                case DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch: ReadMicrosoftDateTime(); break;
                case DateTimeFormat.MillisecondsSinceUnixEpoch: ReadMillisecondsDateTime(); break;
                case DateTimeFormat.SecondsSinceUnixEpoch: ReadSecondsDateTime(); break;
                case DateTimeFormat.ISO8601: ReadISO8601DateTime(); break;
                case DateTimeFormat.RFC1123: ReadRFC1123DateTime(); break;
                default: throw new ConstructionException("Unexpected DateTimeFormat: " + DateFormat);
            }

        }

        void ReadMicrosoftDateTimeOffset()
        {
            ExpectQuote();                                      // --empty--
            ExpectChar('\\');                                   // --empty--
            ExpectChar('/');                                    // --empty--
            ExpectChar('D');                                    // --empty--
            ExpectChar('a');                                    // --empty--
            ExpectChar('t');                                    // --empty--
            ExpectChar('e');                                    // --empty--
            ExpectChar('(');                                    // --empty--
            Emit.LoadArgument(0);                               // TextReader      
            Emit.Call(Methods.GetReadMicrosoftDateTimeOffset(ReadingFromString)); // DateTimeOffset
            ExpectChar(')');                                                        // DateTimeOffset
            ExpectChar('\\');                                                       // DateTimeOffset
            ExpectChar('/');                                                        // DateTimeOffset

            ExpectQuote();                  // DateTimeOffset
        }

        void ReadMicrosoftDateTime()
        {
            ExpectQuote();                                      // --empty--
            ExpectChar('\\');                                   // --empty--
            ExpectChar('/');                                    // --empty--
            ExpectChar('D');                                    // --empty--
            ExpectChar('a');                                    // --empty--
            ExpectChar('t');                                    // --empty--
            ExpectChar('e');                                    // --empty--
            ExpectChar('(');                                    // --empty--
            ReadPrimitive(typeof(long));                        // long
            Emit.LoadArgument(0);                               // long TextReader
            Emit.Call(Methods.GetDiscardMicrosoftTimeZoneOffset(ReadingFromString)); // long
            ExpectChar(')');                                    // long
            ExpectChar('\\');                                   // long
            ExpectChar('/');                                    // long

            // convert MS into ticks
            Emit.LoadConstant(10000L);              // long long
            Emit.Multiply();                        // long
            Emit.LoadConstant(621355968000000000L); // long long
            Emit.Add();                             // long

            Emit.LoadConstant((int)DateTimeKind.Utc);       // long DateTimeKind
            Emit.NewObject<DateTime, long, DateTimeKind>(); // DateTime

            ExpectQuote();                  // DateTime
        }

        void ReadMillisecondsDateTime()
        {
            ReadPrimitive(typeof(long));            // long
            Emit.LoadConstant(10000L);              // long long
            Emit.Multiply();                        // long
            Emit.LoadConstant(621355968000000000L); // long long
            Emit.Add();                             // long

            Emit.LoadConstant((int)DateTimeKind.Utc);       // long DateTimeKind
            Emit.NewObject<DateTime, long, DateTimeKind>(); // DateTime
        }

        void ReadSecondsDateTime()
        {
            ReadPrimitive(typeof(long));            // long
            Emit.LoadConstant(10000000L);           // long long
            Emit.Multiply();                        // long
            Emit.LoadConstant(621355968000000000L); // long long
            Emit.Add();                             // long

            Emit.LoadConstant((int)DateTimeKind.Utc);       // long DateTimeKind
            Emit.NewObject<DateTime, long, DateTimeKind>(); // DateTime
        }

        void ReadISO8601DateTime()
        {
            ExpectQuote();                      // --empty--
            Emit.LoadArgument(0);               // TextReader
            if (UseCharArrayOverStringBuilder)
            {
                LoadCharBufferAddress();
                Emit.Call(Methods.GetReadISO8601DateWithCharArray(ReadingFromString));  // DateTime
                ExpectQuote();                                                          // DateTime
            }
            else
            {
                LoadCharBuffer();
                Emit.Call(Methods.GetReadISO8601Date(ReadingFromString));   // DateTime
                ExpectQuote();                                              // DateTime
            }
        }

        static readonly MethodInfo DateTime_TryParseExact = typeof(DateTime).GetMethod("TryParseExact", new[] { typeof(string), typeof(string), typeof(IFormatProvider), typeof(DateTimeStyles), typeof(DateTime).MakeByRefType() });
        static readonly MethodInfo CultureInfo_InvariantCulture = typeof(CultureInfo).GetProperty("InvariantCulture").GetMethod;
        void ReadRFC1123DateTime()
        {
            if (!UseFastRFC1123Method)
            {
                var universal = (int)DateTimeStyles.AdjustToUniversal;

                ReadPrimitive(typeof(string));              // string
                Emit.LoadConstant("R");                     // string string
                Emit.Call(CultureInfo_InvariantCulture);    // string string CultureInfo
                Emit.LoadConstant(universal);               // string string CultureInfo int

                using (var loc = Emit.DeclareLocal<DateTime>(GetNextName()))
                {
                    var success = Emit.DefineLabel(GetNextName());

                    Emit.LoadLocalAddress(loc);             // string string CultureInfo int DateTime&
                    Emit.Call(DateTime_TryParseExact);      // bool
                    Emit.BranchIfTrue(success);             // --empty--

                    Emit.LoadConstant("Couldn't parse RFC1123 DateTime");                   // string
                    Emit.LoadArgument(0);                                                   // string TextReader
                    Emit.LoadConstant(false);                                               // string TextReader bool
                    ThrowStringStreamBool();
                    Emit.Throw();

                    Emit.MarkLabel(success);    // --empty--
                    Emit.LoadLocal(loc);        // DateTime
                }

                return;
            }

            ExpectQuote();                                              // --empty--
            Emit.LoadArgument(0);                                       // TextReader
            Emit.Call(Methods.GetReadRFC1123Date(ReadingFromString));   // DateTime
            ExpectQuote();                                              // DateTime
        }

        void ReadPrimitive(Type primitiveType)
        {
            if (primitiveType == typeof(bool))
            {
                ReadBool();
                return;
            }

            if (primitiveType == typeof(char))
            {
                ReadChar();
                return;
            }

            if (primitiveType == typeof(string))
            {
                ReadString();
                return;
            }

            if (primitiveType == typeof(Guid))
            {
                ReadGuid();
                return;
            }

            if (primitiveType == typeof(TimeSpan))
            {
                ReadTimeSpan();
                return;
            }

            if (primitiveType == typeof(DateTime))
            {
                ReadDate();
                return;
            }

            if (primitiveType == typeof(DateTimeOffset))
            {
                ReadDateTimeOffset();
                return;
            }

            ReadNumber(primitiveType);
        }

        void ReadPrimitiveTypeWrapper(Type primitiveTypeWrapper)
        {
            var primitiveMember = primitiveTypeWrapper.GetPrimitiveWrapperPropertyOrField();
            Type wrappedType;
            if (primitiveMember is FieldInfo)
            {
                wrappedType = ((FieldInfo)primitiveMember).FieldType;
            }
            else
            {
                wrappedType = ((PropertyInfo)primitiveMember).PropertyType;
            }

            var wrapperConst = primitiveTypeWrapper.GetConstructor(new[] { wrappedType });
            if (wrapperConst != null)
            {
                // use ctor
                ReadPrimitive(wrappedType); // value
                Emit.NewObject(wrapperConst); // object
            }
            else
            {
                var defaultConst = primitiveTypeWrapper.GetConstructor(Type.EmptyTypes);
                if (defaultConst == null)
                {
                    throw
                        new ConstructionException(
                            string.Format("Primitive wrapper {0} needs a default constructor, or a constructor taking a single {1} parameter",
                                primitiveTypeWrapper.FullName,
                                wrappedType.FullName
                            )
                        );
                }
                Emit.NewObject(defaultConst);
                using (var loc = Emit.DeclareLocal(primitiveTypeWrapper, GetNextName()))
                {
                    // wrapper
                    Emit.StoreLocal(loc);       // --empty--
                    Emit.LoadLocal(loc);        // wrapper
                    ReadPrimitive(wrappedType); // wrapper value

                    if (primitiveMember is FieldInfo)
                    {
                        Emit.StoreField((FieldInfo)primitiveMember);    // --empty--
                    }
                    else
                    {
                        SetProperty((PropertyInfo)primitiveMember);      // --empty--
                    }
                    Emit.LoadLocal(loc);        // wrapper
                }
            }
        }

        void ConsumeWhiteSpace()
        {
            Emit.LoadArgument(0);                                       // TextReader
            Emit.Call(Methods.GetConsumeWhiteSpace(ReadingFromString)); // --empty--
        }

        void ReadSkipWhitespace()
        {
            Emit.LoadArgument(0);                                           // TextReader
            Emit.Call(Methods.GetReadSkipWhitespace(ReadingFromString));    // int
        }

        void ExpectEndOfStream()
        {
            var success = Emit.DefineLabel(GetNextName());

            Emit.LoadArgument(1);                   // int
            Emit.LoadConstant(0);                   // int
            Emit.UnsignedBranchIfNotEqual(success); // --empty --
            ReadCharFromStream();                   // int
            Emit.LoadConstant(-1);                  // int -1
            Emit.BranchIfEqual(success);            // --empty--

            Emit.LoadConstant("Expected end of stream");    // string
            Emit.LoadArgument(0);                           // string TextReader
            Emit.LoadConstant(false);                       // string TextReader bool
            ThrowStringStreamBool();                        // DeserializationException
            Emit.Throw();                                   // --empty--

            Emit.MarkLabel(success);        // --empty--
        }

        static readonly MethodInfo TextReader_Peek = typeof(TextReader).GetMethod("Peek", BindingFlags.Public | BindingFlags.Instance);
        static readonly MethodInfo ThunkReader_Peek = typeof(ThunkReader).GetMethod("Peek", BindingFlags.Public | BindingFlags.Instance);
        void RawPeekChar()
        {
            Emit.LoadArgument(0);                   // TextReader

            if (!ReadingFromString)
            {
                Emit.CallVirtual(TextReader_Peek);      // int
            }
            else
            {
                Emit.Call(ThunkReader_Peek);            // int
            }
        }

        void ReadFlagsEnum(Type enumType)
        {
            ExpectQuote();                  // --empty--

            var specific = Methods.GetReadFlagsEnum(ReadingFromString).MakeGenericMethod(enumType);

            Emit.LoadArgument(0);           // TextReader
            LoadStringBuilder();            // TextReader StringBuilder&
            Emit.Call(specific);            // enum
        }

        void ReadEnum(Type enumType, bool cameFromNullable)
        {
            if (Enum.GetNames(enumType).Count() == 0)
            {
                if (cameFromNullable)
                {
                    var message = enumType.FullName + "  has no defined values and thus cannot be deserialized";
                    var constr = ReadingFromString ?
                        DeserializationException_Cons_string_ThunkWriter_bool :
                        DeserializationException_Cons_string_TextReader_bool;

                    // need to proxy this through a different method, just so Sigil can't tell
                    //   it's unreachable code
                    var mtd = Methods.GetThrowNoDefinedValueInEnum(ReadingFromString);

                    Emit.LoadConstant(message);     // string
                    Emit.LoadArgument(0);           // TextReader
                    Emit.Call(mtd);                 // --empty--

                    // fall through
                }
                else
                {
                    throw new ConstructionException(enumType.FullName + " has no values, and cannot be deserialized; add a value, make nullable, or configure to treat as integer");
                }
            }

            if (UseNameAutomataForEnums)
            {
                var setterLookup = typeof(EnumLookup<>).MakeGenericType(enumType);

                MethodInfo getVal;
                if (ReadingFromString)
                {
                    getVal = setterLookup.GetMethod("GetEnumValueThunkReader", new[] { typeof(ThunkReader).MakeByRefType() });
                }
                else
                {
                    getVal = setterLookup.GetMethod("GetEnumValue", new[] { typeof(TextReader) });
                }

                ExpectQuote();
                Emit.LoadArgument(0);   // TextReader
                Emit.Call(getVal);      // emum

                return;
            }

            if (enumType.IsFlagsEnum())
            {
                ReadFlagsEnum(enumType);
                return;
            }

            var specific = Methods.GetParseEnum(ReadingFromString).MakeGenericMethod(enumType);

            ExpectQuote();                  // --empty--
            Emit.LoadArgument(0);           // TextReader
            CallReadEncodedString();        // string
            Emit.LoadArgument(0);           // TextReader
            Emit.Call(specific);            // enum
        }

        void LoadConstantOfType(object val, Type type)
        {
            if (type == typeof(byte))
            {
                Emit.LoadConstant((byte)val);
                return;
            }

            if (type == typeof(sbyte))
            {
                Emit.LoadConstant((sbyte)val);
                return;
            }

            if (type == typeof(short))
            {
                Emit.LoadConstant((short)val);
                return;
            }

            if (type == typeof(ushort))
            {
                Emit.LoadConstant((ushort)val);
                return;
            }

            if (type == typeof(int))
            {
                Emit.LoadConstant((int)val);
                return;
            }

            if (type == typeof(uint))
            {
                Emit.LoadConstant((uint)val);
                return;
            }

            if (type == typeof(long))
            {
                Emit.LoadConstant((long)val);
                return;
            }

            if (type == typeof(ulong))
            {
                Emit.LoadConstant((ulong)val);
                return;
            }

            throw new ConstructionException("Unexpected type: " + type);
        }

        void ReadNullable(MemberInfo nullableMember, Type nullableType)
        {
            var underlying = Nullable.GetUnderlyingType(nullableType);
            var nullableConst = nullableType.GetConstructor(new[] { underlying });
            var done = Emit.DefineLabel(GetNextName());

            if (AllPrimitivesThroughHelpers)
            {
                var isNull = Emit.DefineLabel(GetNextName());

                using (var loc = Emit.DeclareLocal(nullableType, GetNextName()))
                {

                    Emit.LoadArgument(0);                               // TextReader
                    Emit.Call(Methods.GetIsNull(ReadingFromString));    // bool
                    Emit.BranchIfTrue(isNull);                          // --empty--

                    Build(nullableMember, underlying, fromNullable: true);  // underlying
                    Emit.NewObject(nullableConst);                          // nullableType
                    Emit.Branch(done);                                      // nullableType

                    Emit.MarkLabel(isNull);                         // --empty--
                    Emit.LoadLocalAddress(loc);                     // nullableType*
                    Emit.InitializeObject(nullableType);            // --empty--
                    Emit.LoadLocal(loc);                            // nullableType
                }

                Emit.MarkLabel(done);                               // nullableType

                return;
            }

            var maybeNull = Emit.DefineLabel(GetNextName());
            

            using (var loc = Emit.DeclareLocal(nullableType, GetNextName()))
            {
                RawPeekChar();                      // int
                Emit.LoadConstant('n');             // int n
                Emit.BranchIfEqual(maybeNull);      // --empty--

                Build(nullableMember, underlying, fromNullable: true);  // underlying
                Emit.NewObject(nullableConst);      // nullableType
                Emit.Branch(done);                  // nullableType

                Emit.MarkLabel(maybeNull);          // --empty--
                ExpectChar('n');                    // --empty--
                ExpectChar('u');                    // --empty--
                ExpectChar('l');                    // --empty--
                ExpectChar('l');                    // --empty--

                Emit.LoadLocalAddress(loc);             // nullableType*
                Emit.InitializeObject(nullableType);    // --empty--
                Emit.LoadLocal(loc);                    // nullableType

                Emit.MarkLabel(done);               // nullableType
            }
        }

        void ReadList(MemberInfo listMember, Type listType, bool isSet)
        {
            var elementType = isSet
                ? listType.GetSetInterface().GetGenericArguments()[0]
                : listType.GetListInterface().GetGenericArguments()[0];

            if (listType.IsGenericType())
            {
                if (!isSet && listType.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    listType = typeof(List<>).MakeGenericType(elementType);
                }
                else
                {
                    if (isSet && listType.GetGenericTypeDefinition() == typeof(ISet<>))
                    {
                        listType = typeof(HashSet<>).MakeGenericType(elementType);
                    }
                }
            }

            var addMtd = listType.GetMethod("Add");

            var doRead = Emit.DefineLabel(GetNextName());
            var done = Emit.DefineLabel(GetNextName());
            var doneSkipChar = Emit.DefineLabel(GetNextName());

            var isArray = listType.IsArray;

            Sigil.Label doneSkipCharNull = null;

            if (isArray)
            {
                listType = typeof(List<>).MakeGenericType(elementType);
                addMtd = listType.GetMethod("Add");
                doneSkipCharNull = Emit.DefineLabel(GetNextName());
            }

            using (var loc = Emit.DeclareLocal(listType, GetNextName()))
            {
                Action loadList;

                if (!listType.IsValueType())
                {
                    loadList = () => Emit.LoadLocal(loc);

                    ExpectRawCharOrNull(
                        '[',
                        () => { },
                        () =>
                        {
                            Emit.LoadNull();

                            if (isArray)
                            {
                                Emit.Branch(doneSkipCharNull);
                            }
                            else
                            {
                                Emit.Branch(doneSkipChar);
                            }
                        }
                    );
                    Emit.Branch(doRead);
                }
                else
                {
                    loadList = () => Emit.LoadLocalAddress(loc);

                    ExpectChar('[');
                }

                Emit.MarkLabel(doRead);                 // --empty--
                if (listType.IsValueType())
                {
                    Emit.LoadLocalAddress(loc);         // listType*
                    Emit.InitializeObject(listType);    // --empty--
                }
                else
                {
                    var listCons = listType.GetPublicOrPrivateConstructor();
                    if (listCons == null) throw new ConstructionException("Expected a parameterless constructor for " + listType);

                    Emit.NewObject(listCons);   // listType
                    Emit.StoreLocal(loc);                                       // --empty--
                }

                // first step unrolled, cause ',' isn't legal
                ConsumeWhiteSpace();                            // --empty--
                loadList();                                     // listType
                RawPeekChar();                                  // listType int 
                Emit.LoadConstant(']');                         // listType int ']'
                Emit.BranchIfEqual(done);                       // listType(*?)
                Build(listMember, elementType);                             // listType(*?) elementType
                Emit.CallVirtual(addMtd);                       // --empty--
                if (isSet) Emit.Pop();                          // if it's a set, Add() returns a bool which we need to pop off the stack

                var startLoop = Emit.DefineLabel(GetNextName());
                var nextItem = Emit.DefineLabel(GetNextName());

                Emit.MarkLabel(startLoop);                      // --empty--
                loadList();                                     // listType(*?)
                ReadSkipWhitespace();                           // listType(*?) int
                ThrowIfEmptyAndWasExpecting(",", "]");          // listType(*?) int

                Emit.Duplicate();                               // listType(*?) int int
                Emit.LoadConstant(',');                         // listType(*?) int int ','
                Emit.BranchIfEqual(nextItem);                   // listType(*?) int
                Emit.LoadConstant(']');                         // listType(*?) int ']'
                Emit.BranchIfEqual(doneSkipChar);               // listType(*?)

                // didn't get what we expected
                ThrowExpected(",", "]");

                Emit.MarkLabel(nextItem);           // listType(*?) int
                Emit.Pop();                         // listType(*?)
                ConsumeWhiteSpace();                // listType(*?)
                Build(listMember, elementType);                 // listType(*?) elementType
                Emit.CallVirtual(addMtd);           // --empty--
                if (isSet) Emit.Pop();              // if it's a set, Add() returns a bool which we need to pop off the stack
                Emit.Branch(startLoop);             // --empty--

                Emit.MarkLabel(done);               // listType(*?)
                ReadCharFromStream();               // listType(*?) int
                Emit.Pop();                         // listType(*?)

                Emit.MarkLabel(doneSkipChar);       // listType(*?)

                if (isArray)
                {
                    var toArray = listType.GetMethod("ToArray");
                    Emit.Call(toArray);             // elementType[]

                    Emit.MarkLabel(doneSkipCharNull);
                }
            }
        }

        void ReadDictionary(MemberInfo dictionaryMember, Type dictType)
        {
            var keyType = dictType.GetDictionaryInterface().GetGenericArguments()[0];

            var keyIsString = keyType == typeof(string);
            var keyIsInteger = keyType.IsIntegerNumberType();
            var keyIsEnum = keyType.IsEnum();

            if (!(keyIsString || keyIsInteger || keyIsEnum)) throw new ConstructionException("Only dictionaries with strings, integers, or enums for keys can be deserialized");
            var valType = dictType.GetDictionaryInterface().GetGenericArguments()[1];

            if (dictType.IsGenericType() && dictType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valType);
            }

            var addMtd = dictType.GetDictionaryInterface().GetMethod("Add", new[] { keyType, valType });

            var done = Emit.DefineLabel(GetNextName());
            var doneSkipChar = Emit.DefineLabel(GetNextName());

            if (!dictType.IsValueType())
            {
                ExpectRawCharOrNull(
                    '{',
                    () => { },
                    () =>
                    {
                        Emit.LoadNull();
                        Emit.Branch(doneSkipChar);
                    }
                );
            }
            else
            {
                ExpectChar('{');
            }

            using (var loc = Emit.DeclareLocal(dictType, GetNextName()))
            {
                Action loadDict;
                if (dictType.IsValueType())
                {
                    Emit.LoadLocalAddress(loc);         // dictType*
                    Emit.InitializeObject(dictType);    // --empty--

                    loadDict = () => Emit.LoadLocalAddress(loc);
                }
                else
                {
                    var dictCons = dictType.GetPublicOrPrivateConstructor();
                    if (dictCons == null) throw new ConstructionException("Expected a parameterless constructor for " + dictType);

                    Emit.NewObject(dictCons);                                   // dictType
                    Emit.StoreLocal(loc);                                       // --empty--

                    loadDict = () => Emit.LoadLocal(loc);
                }

                var loopStart = Emit.DefineLabel(GetNextName());

                ConsumeWhiteSpace();        // --empty--
                loadDict();                 // dictType(*?)
                RawPeekChar();              // dictType(*?) int 
                Emit.LoadConstant('}');     // dictType(*?) int '}'
                Emit.BranchIfEqual(done);   // dictType(*?)
                if (keyType == typeof(string))
                {
                    Build(dictionaryMember, typeof(string));  // dictType(*?) string
                }
                else
                {
                    if (keyIsInteger || (keyIsEnum && dictionaryMember != null && dictionaryMember.ShouldConvertEnum(keyType)))
                    {
                        ExpectQuote();          // dictType(*?)
                        Build(dictionaryMember, keyType);         // dictType(*?) integer
                        ExpectQuote();          // dictType(*?) integer
                    }
                    else
                    {
                        Build(dictionaryMember, keyType);         // dictType(*?) enum
                    }
                }
                ReadSkipWhitespace();        // dictType(*?) (integer|string|enum)
                CheckChar(':');              // dictType(*?) (integer|string|enum)
                ConsumeWhiteSpace();         // dictType(*?) (integer|string|enum)
                Build(dictionaryMember, valType);              // dictType(*?) (integer|string|enum) valType
                Emit.CallVirtual(addMtd);    // --empty--

                var nextItem = Emit.DefineLabel(GetNextName());

                Emit.MarkLabel(loopStart);              // --empty--
                loadDict();                             // dictType(*?)
                ReadSkipWhitespace();                   // dictType(*?) int 
                ThrowIfEmptyAndWasExpecting(",", "}");  // dictType(*?) int

                Emit.Duplicate();               // dictType(*?) int int
                Emit.LoadConstant(',');         // dictType(*?) int int ','
                Emit.BranchIfEqual(nextItem);   // dictType(*?) int
                Emit.LoadConstant('}');         // dictType(*?) int '}'
                Emit.BranchIfEqual(doneSkipChar); // dictType(*?)

                // didn't get what we expected
                ThrowExpected(",", "}");

                Emit.MarkLabel(nextItem);           // dictType(*?) int
                Emit.Pop();                         // dictType(*?)
                ConsumeWhiteSpace();                // dictType(*?)
                if (keyType == typeof(string))
                {
                    Build(dictionaryMember, typeof(string));  // dictType(*?) string
                }
                else
                {
                    if (keyIsInteger || (keyIsEnum && dictionaryMember != null && dictionaryMember.ShouldConvertEnum(keyType)))
                    {
                        ExpectQuote();          // dictType(*?)
                        Build(dictionaryMember, keyType);         // dictType(*?) integer
                        ExpectQuote();          // dictType(*?) integer
                    }
                    else
                    {
                        Build(dictionaryMember, keyType);         // dictType(*?) enum
                    }
                }
                ReadSkipWhitespace();               // dictType(*?) (integer|string|enum)
                CheckChar(':');                     // dictType(*?) (integer|string|enum)
                ConsumeWhiteSpace();                // dictType(*?) (integer|string|enum)
                Build(dictionaryMember, valType);                     // dictType(*?) (integer|string|enum) valType
                Emit.CallVirtual(addMtd);           // --empty--
                Emit.Branch(loopStart);             // --empty--
            }

            Emit.MarkLabel(done);               // dictType(*?)
            ReadCharFromStream();               // dictType(*?) int
            Emit.Pop();                         // dictType(*?)

            Emit.MarkLabel(doneSkipChar);       // dictType(*?)
        }

        void SkipObjectMember()
        {
            Emit.LoadArgument(0);                           // TextReader
            Emit.Call(Methods.GetSkip(ReadingFromString));  // --empty--
        }

        void LoadRecursiveTypeDelegate(Type recursiveType)
        {
            var typeCache = typeof(TypeCache<,>).MakeGenericType(OptionsType, recursiveType);

            FieldInfo thunk;
            if (ReadingFromString)
            {
                thunk = typeCache.GetField("StringThunk", BindingFlags.Public | BindingFlags.Static);
            }
            else
            {
                thunk = typeCache.GetField("Thunk", BindingFlags.Public | BindingFlags.Static);
            }

            Emit.LoadField(thunk);
        }

        void ReadObject(Type objType)
        {
            var isAnonymous = objType.IsAnonymouseClass();

            if (isAnonymous)
            {
                if (UseNameAutomata)
                {
                    ReadAnonymousObjectAutomata(objType);
                    return;
                }

                ReadAnonymousObjectDictionaryLookup(objType);
                return;
            }

            if (UseNameAutomata)
            {
                ReadObjectAutomata(objType);
                return;
            }

            ReadObjectDictionaryLookup(objType);
        }

        void SkipAllMembers(Sigil.Label done, Sigil.Label doneSkipChar)
        {
            var continueSkipping = Emit.DefineLabel(GetNextName());

            var skipEncodedString = Methods.GetSkipEncodedString(ReadingFromString);

            // first pass, doesn't check for ,
            ConsumeWhiteSpace();                // objType
            RawPeekChar();                      // objType char
            Emit.LoadConstant('}');             // objType char '}'
            Emit.BranchIfEqual(done);           // objType

            Emit.LoadArgument(0);                   // objType TextReader
            Emit.Call(skipEncodedString);           // objType
            ReadSkipWhitespace();                   // objType
            CheckChar(':');                         // objType
            ConsumeWhiteSpace();                    // objType
            SkipObjectMember();                     // objType
            Emit.Branch(continueSkipping);          // objType

            // second (and third, and fourth, and ...) does check for ,
            Emit.MarkLabel(continueSkipping);   // objType
            ConsumeWhiteSpace();                // objType
            RawPeekChar();                      // objType char
            Emit.LoadConstant('}');             // objType char '}'
            Emit.BranchIfEqual(done);           // objType

            // we Peek'd the }, we can just read for ','
            ExpectChar(',');                    // objType

            ConsumeWhiteSpace();                    // objType
            Emit.LoadArgument(0);                   // objType TextReader
            Emit.Call(skipEncodedString);           // objType
            ReadSkipWhitespace();                   // objType
            CheckChar(':');                         // objType
            ConsumeWhiteSpace();                    // objType
            SkipObjectMember();                     // objType
            Emit.Branch(continueSkipping);          // objType

            Emit.MarkLabel(done);               // objType
            ReadCharFromStream();               // objType int
            Emit.Pop();                         // objType

            Emit.MarkLabel(doneSkipChar);   // objType(*?)
        }

        void ReadObjectAutomata(Type objType)
        {
            var done = Emit.DefineLabel(GetNextName());
            var doneSkipChar = Emit.DefineLabel(GetNextName());

            if (!objType.IsValueType())
            {
                ExpectRawCharOrNull(
                    '{',
                    () => { },
                    () =>
                    {
                        Emit.LoadNull();
                        Emit.Branch(doneSkipChar);
                    }
                );
            }
            else
            {
                ExpectChar('{');
            }

            using (var loc = Emit.DeclareLocal(objType, GetNextName()))
            {
                Action loadObj;
                if (objType.IsValueType())
                {
                    Emit.LoadLocalAddress(loc);     // objType*
                    Emit.InitializeObject(objType); // --empty--

                    loadObj = () => Emit.LoadLocalAddress(loc);
                }
                else
                {
                    var cons = objType.GetPublicOrPrivateConstructor();
                    if (cons == null) throw new ConstructionException("Expected a parameterless constructor for " + objType);

                    Emit.NewObject(cons);   // objType
                    Emit.StoreLocal(loc);   // --empty--

                    loadObj = () => Emit.LoadLocal(loc);
                }

                var loopStart = Emit.DefineLabel(GetNextName());
                var serializationType = SerializationNameFormat.GetGenericTypeArgument();

                var setterLookup = typeof(SetterLookup<,>).MakeGenericType(objType, serializationType);
                var setterGetSetters = setterLookup.GetMethod("GetSetters", BindingFlags.Public | BindingFlags.Static);

                var setters = (Dictionary<string, MemberInfo[]>)setterGetSetters.Invoke(null, new object[0]);

                // special case object w/ no deserializable properties
                if (setters.Count == 0)
                {
                    loadObj();                          // objType(*?)

                    if (objType.IsValueType())
                    {
                        Emit.LoadObject(objType);       // objType
                    }

                    SkipAllMembers(done, doneSkipChar); // objType

                    return;
                }

                var orderedSetters =
                    setters
                    .OrderBy(kv => kv.Key)
                    .Select((kv, i) => new { Index = i, Name = kv.Key, Setters = kv.Value, Label = Emit.DefineLabel(GetNextName()) })
                    .ToList();

                MethodInfo findSetterIdx;
                if (ReadingFromString)
                {
                    findSetterIdx = setterLookup.GetMethod("FindSetterIndexThunkReader", new[] { typeof(ThunkReader).MakeByRefType() });
                }
                else
                {
                    findSetterIdx = setterLookup.GetMethod("FindSetterIndex", new[] { typeof(TextReader) });
                }

                var inOrderLabels =
                    orderedSetters
                    .Select(o => o.Label)
                    .ToArray();

                ConsumeWhiteSpace();        // --empty--
                loadObj();                  // objType(*?)
                RawPeekChar();              // objType(*?) int 
                Emit.LoadConstant('}');     // objType(*?) int '}'
                Emit.BranchIfEqual(done);   // objType(*?)
                ReadSkipWhitespace();       // objType(*?)

                CheckQuote();               // objType(*?)
                Emit.LoadArgument(0);       // objType(*?) TextReader
                Emit.Call(findSetterIdx);   // objType(*?) int

                ReadSkipWhitespace();       // objType(*?) int
                CheckChar(':');             // objType(*?) int
                ConsumeWhiteSpace();        // objType(*?) int

                var readingMember = Emit.DefineLabel(GetNextName());
                Emit.MarkLabel(readingMember);  // objType(*?) int

                using (var oLoc = Emit.DeclareLocal<int>(GetNextName()))
                {
                    var isMember = Emit.DefineLabel(GetNextName());

                    Emit.Duplicate();                       // objType(*?) int int
                    Emit.StoreLocal(oLoc);                  // objType(*?) int
                    Emit.LoadConstant(0);                   // objType(*?) int int
                    Emit.BranchIfGreaterOrEqual(isMember);  // objType(*?)

                    Emit.Pop();                     // --empty--
                    SkipObjectMember();             // --empty--
                    Emit.Branch(loopStart);         // --empty--

                    Emit.MarkLabel(isMember);       // objType(*?)
                    Emit.LoadLocal(oLoc);           // objType(*?) int
                    Emit.Switch(inOrderLabels);     // objType(*?)

                    // fallthrough case
                    ThrowExpected("a member name"); // --empty--
                }

                foreach (var kv in orderedSetters)
                {
                    var memberName = kv.Name;
                    var label = kv.Label;
                    var members = kv.Setters;

                    if (members.Length == 1)
                    {
                        var member = members[0];

                        Emit.MarkLabel(label);      // objType(*?)
                        ReadAndSetMember(member);   // --empty--
                        Emit.Branch(loopStart);     // --empty--
                    }
                    else
                    {
                        Emit.MarkLabel(label);                              // objType(*?)
                        ReadAndSetDiscriminantUnion(memberName, members);   // --empty--
                        Emit.Branch(loopStart);                             // --empty--
                    }
                }

                var nextItem = Emit.DefineLabel(GetNextName());

                Emit.MarkLabel(loopStart);              // --empty--
                loadObj();                              // objType(*?)
                ReadSkipWhitespace();                   // objType(*?) int 
                ThrowIfEmptyAndWasExpecting(",", "}");  // objType(*?) int
                Emit.Duplicate();                       // objType(*?) int int
                Emit.LoadConstant(',');                 // objType(*?) int int ','
                Emit.BranchIfEqual(nextItem);           // objType(*?) int
                Emit.LoadConstant('}');                 // objType(*?) int '}'
                Emit.BranchIfEqual(doneSkipChar);       // objType(*?)

                // didn't get what we expected
                ThrowExpected(",", "}");

                Emit.MarkLabel(nextItem);           // objType(*?) int
                Emit.Pop();                         // objType(*?)
                ReadSkipWhitespace();

                CheckQuote();
                Emit.LoadArgument(0);               // TextReader
                Emit.Call(findSetterIdx);           // int

                ReadSkipWhitespace();               // objType(*?) int
                CheckChar(':');                     // objType(*?) int
                ConsumeWhiteSpace();                // objType(*?) int
                Emit.Branch(readingMember);         // objType(*?) int
            }

            Emit.MarkLabel(done);               // objType(*?)
            ReadCharFromStream();               // objType(*?) int
            Emit.Pop();                         // objType(*?)

            Emit.MarkLabel(doneSkipChar);       // objType(*?)

            if (objType.IsValueType())
            {
                Emit.LoadObject(objType);       // objType
            }
        }

        void ReadAndSetMember(MemberInfo member)
        {
            // Stack starts
            // objType(*?)

            var memberType = member.ReturnType();

            var memberAttr = member.GetCustomAttribute<JilDirectiveAttribute>();
            if (memberType.IsEnum() && memberAttr != null && memberAttr.TreatEnumerationAs != null)
            {
                var underlyingEnumType = Enum.GetUnderlyingType(memberType);

                Build(member, memberAttr.TreatEnumerationAs);   // objType(*?) SerializeEnumerationAsType
                Emit.Convert(underlyingEnumType);       // objType(*?) memberType
            }
            else
            {
                Build(member, memberType);          // objType(*?) memberType
            }

            if (member is FieldInfo)
            {
                Emit.StoreField((FieldInfo)member); // --empty--
            }
            else
            {
                SetProperty((PropertyInfo)member);  // --empty--
            }
        }

        static readonly MethodInfo Type_GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);
        void ReadAndSetDiscriminantUnion(string memberName, MemberInfo[] union)
        {
            Dictionary<char, MemberInfo> discriminants;
            MemberInfo unionTypeIndicator;
            Dictionary<UnionCharsets, MemberInfo> charsets;
            bool allowsNull;

            string errorMessage;
            if (!Utils.CheckUnionLegality(DateFormat, memberName, union, out discriminants, out unionTypeIndicator, out charsets, out allowsNull, out errorMessage))
            {
                throw new ConstructionException(errorMessage);
            }

            var refMembers = discriminants.Where(kv => !kv.Value.ReturnType().IsValueType()).Select(kv => kv.Value).ToList();
            var nullableMembers = discriminants.Where(kv => kv.Value.ReturnType().IsNullableType()).Select(kv => kv.Value).ToList();
            var expected = discriminants.Keys.ToArray();

            var streamNotEmpty = Emit.DefineLabel(GetNextName());
            var end = Emit.DefineLabel(GetNextName());

            RawPeekChar();                                  // objType(*?) int
            Emit.Duplicate();                               // objType(*?) int int
            Emit.LoadConstant(-1);                          // objType(*?) int int -1
            Emit.UnsignedBranchIfNotEqual(streamNotEmpty);  // objType(*?) int

            ThrowExpectedButEnded(expected);                // --empty--

            Emit.MarkLabel(streamNotEmpty);                 // objType(*?) int

            // _sigh_ strong names fuck with dynamic linking the UnionConfigLookup ASM
            //    just have to live with it for now
#if !STRONG_NAME
            var canUseFastUnionLookup = true;
#else
            var canUseFastUnionLookup = false;
#endif

            if (UseFastUnionLookup && canUseFastUnionLookup)
            {
                var allCharsets = UnionCharsets.None;
                foreach (var kv in charsets)
                {
                    allCharsets |= kv.Key;
                }

                var config = UnionConfigLookup.Get(allCharsets, allowsNull);
                var lookup = typeof(UnionLookup<>).MakeGenericType(config);

                var min = (int)lookup.GetField("MinimumChar").GetValue(null);
                var lookupArr = lookup.GetField("Lookup");
                var lookupArrLen = ((int[])lookupArr.GetValue(null)).Length;
                var charsetsInOrder = (UnionCharsets[])lookup.GetField("CharsetOrder").GetValue(null);

                var miss = Emit.DefineLabel(GetNextName());
                var labels =
                    charsetsInOrder
                        .Select(
                            c =>
                            {
                                MemberInfo member;

                                if (!charsets.TryGetValue(c, out member)) member = null;

                                return Tuple.Create(Emit.DefineLabel(GetNextName()), member, c);
                            }
                        )
                        .ToArray();
                var labelsArr = labels.Select(t => t.Item1).ToArray();

                Emit.LoadConstant(min);                 // objType(*?) int int
                Emit.Subtract();                        // objType(*?) int
                Emit.Duplicate();                       // objType(*?) int int
                Emit.LoadConstant(lookupArrLen);        // objType(*?) int int int
                Emit.BranchIfGreaterOrEqual(miss);      // objType(*?) int
                Emit.Duplicate();                       // objType(*?) int int
                Emit.LoadConstant(0);                   // objType(*?) int int int
                Emit.BranchIfLess(miss);                // objType(*?) int
                using (var loc = Emit.DeclareLocal<int>(GetNextName()))
                {
                    Emit.StoreLocal(loc);               // objType(*?)
                    Emit.LoadField(lookupArr);          // objType(*?) int[]
                    Emit.LoadLocal(loc);                // objType(*?) int[] int
                }
                Emit.LoadElement<int>();                // objType(*?) int
                Emit.Switch(labelsArr);                 // objType(*?)

                // fall through
                ThrowExpected(expected);                // --empty--

                foreach (var t in labels)
                {
                    var label = t.Item1;
                    var member = t.Item2;
                    var charset = t.Item3;

                    Emit.MarkLabel(label);                  // objType(*?)

                    if (charset == UnionCharsets.Null)
                    {
                        // have to special case null, since it can mean clearning multiple members
                        //   as there isn't a 1-to-1 type-to-member map for the null value

                        ExpectNullSetMembersToDefaultAndClearUnionTypeIndicator(refMembers, nullableMembers, unionTypeIndicator);

                        Emit.Pop();             // --empty--
                        Emit.Branch(end);       // --empty--
                    }
                    else
                    {
                        // set the indicator, __if__ one has actually be registered
                        //   we don't want to do any work if they don't care to
                        if (unionTypeIndicator != null)
                        {
                            Emit.Duplicate();                           // objType(*?) objType(*?)
                            Emit.LoadConstant(member.ReturnType());     // objType(*?) objType(*?) RuntimeTypeHandle
                            Emit.Call(Type_GetTypeFromHandle);          // objType(*?) objType(*?) Type

                            if (unionTypeIndicator is FieldInfo)
                            {
                                var asField = (FieldInfo)unionTypeIndicator;
                                Emit.StoreField(asField);               // objType(*?)
                            }
                            else
                            {
                                var asProp = (PropertyInfo)unionTypeIndicator;
                                SetProperty(asProp);                    // objType(*?)
                            }
                        }

                        ReadAndSetMember(member);                   // --empty--
                        Emit.Branch(end);                           // --empty--
                    }
                }

                Emit.MarkLabel(miss);           // objType(*?) int
                ThrowExpected(expected);        // --empty--

                Emit.MarkLabel(end);            // --empty--
            }
            else
            {
                if (allowsNull)
                {
                    var notNull = Emit.DefineLabel(GetNextName());

                    Emit.Duplicate();                           // objType(*?) int int
                    Emit.LoadConstant('n');                     // objType(*?) int int 'n'
                    Emit.UnsignedBranchIfNotEqual(notNull);     // objType(*?) int

                    Emit.Pop();
                    ExpectNullSetMembersToDefaultAndClearUnionTypeIndicator(refMembers, nullableMembers, unionTypeIndicator);

                    Emit.Pop();                                 // --empty--
                    Emit.Branch(end);                           // --empty--

                    Emit.MarkLabel(notNull);                    // objType(*?) int
                }

                foreach (var charToMember in discriminants)
                {
                    var c = charToMember.Key;
                    var member = charToMember.Value;
                    var nextChar = Emit.DefineLabel(GetNextName());

                    Emit.Duplicate();                           // objType(*?) int int
                    Emit.LoadConstant((int)c);                  // objType(*?) int int int
                    Emit.UnsignedBranchIfNotEqual(nextChar);    // objType(*?) int

                    Emit.Pop();                                 // objType(*?)

                    // set the indicator, __if__ one has actually be registered
                    //   we don't want to do any work if they don't care to
                    if (unionTypeIndicator != null)
                    {
                        Emit.Duplicate();                           // objType(*?) objType(*?)
                        Emit.LoadConstant(member.ReturnType());     // objType(*?) objType(*?) RuntimeTypeHandle
                        Emit.Call(Type_GetTypeFromHandle);          // objType(*?) objType(*?) Type

                        if (unionTypeIndicator is FieldInfo)
                        {
                            var asField = (FieldInfo)unionTypeIndicator;
                            Emit.StoreField(asField);               // objType(*?)
                        }
                        else
                        {
                            var asProp = (PropertyInfo)unionTypeIndicator;
                            SetProperty(asProp);                    // objType(*?)
                        }
                    }

                    ReadAndSetMember(member);                   // --empty--
                    Emit.Branch(end);                           // --empty--

                    Emit.MarkLabel(nextChar);                   // objType(*?) int
                }

                ThrowExpected(expected);                        // --empty--

                Emit.MarkLabel(end);                            // --empty--
            }
        }

        void ExpectNullSetMembersToDefaultAndClearUnionTypeIndicator(List<MemberInfo> refMembers, List<MemberInfo> nullableMembers, MemberInfo unionTypeIndicator)
        {
            // top of stack
            // ------------
            // objType(*?)

            ExpectChar('n');                            // objType(*?)
            ExpectChar('u');                            // objType(*?)
            ExpectChar('l');                            // objType(*?)
            ExpectChar('l');                            // objType(*?)

            foreach (var r in refMembers)
            {
                Emit.Duplicate();                       // objType(*?) objType(*?)
                Emit.LoadNull();                        // objType(*?) objType(*?) null
                var asProp = r as PropertyInfo;
                if (asProp != null)
                {
                    SetProperty(asProp);                // objType(*?)
                }
                else
                {
                    Emit.StoreField((FieldInfo)r);      // objType(*?)
                }
            }

            foreach (var n in nullableMembers)
            {
                Emit.Duplicate();                       // objType(*?) objType(*?)
                using (var nLoc = Emit.DeclareLocal(n.ReturnType(), GetNextName()))
                {
                    Emit.LoadLocalAddress(nLoc);            // objType(*?) objType(*?) Nullable<?>*
                    Emit.InitializeObject(n.ReturnType());  // objType(*?) objType(*?)
                    Emit.LoadLocal(nLoc);                   // objType(*?) objType(*?) Nullable<?>
                }
                var asProp = n as PropertyInfo;
                if (asProp != null)
                {
                    SetProperty(asProp);                // objType(*?)
                }
                else
                {
                    Emit.StoreField((FieldInfo)n);      // objType(*?)
                }
            }

            if (unionTypeIndicator != null)
            {
                Emit.Duplicate();                       // objType(*?) objType(*?)
                Emit.LoadNull();                        // objType(*?) objType(*?) null
                var asProp = unionTypeIndicator as PropertyInfo;
                if (asProp != null)
                {
                    SetProperty(asProp);                // objType(*?)
                }
                else
                {
                    var asField = (FieldInfo)unionTypeIndicator;
                    Emit.StoreField(asField);           // objType(*?)
                }
            }
        }

        void ReadObjectDictionaryLookup(Type objType)
        {
            var done = Emit.DefineLabel(GetNextName());
            var doneSkipChar = Emit.DefineLabel(GetNextName());

            if (!objType.IsValueType())
            {
                ExpectRawCharOrNull(
                    '{',
                    () => { },
                    () =>
                    {
                        Emit.LoadNull();
                        Emit.Branch(doneSkipChar);
                    }
                );
            }
            else
            {
                ExpectChar('{');
            }

            using (var loc = Emit.DeclareLocal(objType, GetNextName()))
            {
                Action loadObj;
                if (objType.IsValueType())
                {
                    Emit.LoadLocalAddress(loc);     // objType*
                    Emit.InitializeObject(objType); // --empty--

                    loadObj = () => Emit.LoadLocalAddress(loc);
                }
                else
                {
                    var cons = objType.GetPublicOrPrivateConstructor();
                    if (cons == null) throw new ConstructionException("Expected a parameterless constructor for " + objType);

                    Emit.NewObject(cons);   // objType
                    Emit.StoreLocal(loc);   // --empty--

                    loadObj = () => Emit.LoadLocal(loc);
                }

                var loopStart = Emit.DefineLabel(GetNextName());
                var serializationType = SerializationNameFormat.GetGenericTypeArgument();

                var setterLookup = typeof(SetterLookup<,>).MakeGenericType(objType, serializationType);

                var setters = (Dictionary<string, MemberInfo[]>)setterLookup.GetMethod("GetSetters", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[0]);

                // special case object w/ no deserializable properties
                if (setters.Count == 0)
                {
                    loadObj();                      // objType(*?)

                    if (objType.IsValueType())
                    {
                        Emit.LoadObject(objType);   // objType
                    }

                    SkipAllMembers(done, doneSkipChar); // objType

                    return;
                }

                var tryGetValue = typeof(Dictionary<string, int>).GetMethod("TryGetValue");

                var order = setterLookup.GetField("Lookup", BindingFlags.Public | BindingFlags.Static);
                var orderInst = (Dictionary<string, int>)order.GetValue(null);
                var labels = setters.ToDictionary(d => d.Key, d => Emit.DefineLabel(GetNextName()));

                var inOrderLabels = labels.OrderBy(l => orderInst[l.Key]).Select(l => l.Value).ToArray();

                ConsumeWhiteSpace();        // --empty--
                loadObj();                  // objType(*?)
                RawPeekChar();              // objType(*?) int 
                Emit.LoadConstant('}');     // objType(*?) int '}'
                Emit.BranchIfEqual(done);   // objType(*?)
                Emit.LoadField(order);      // objType(*?) Dictionary<string, int> string
                Build(null, typeof(string));      // obType(*?) Dictionary<string, int> string
                ReadSkipWhitespace();       // objType(*?) Dictionary<string, int> string
                CheckChar(':');             // objType(*?) Dictionary<string, int> string
                ConsumeWhiteSpace();        // objType(*?) Dictionary<string, int> string

                var readingMember = Emit.DefineLabel(GetNextName());
                Emit.MarkLabel(readingMember);  // objType(*?) Dictionary<string, int> string

                using (var oLoc = Emit.DeclareLocal<int>(GetNextName()))
                {
                    var isMember = Emit.DefineLabel(GetNextName());

                    Emit.LoadLocalAddress(oLoc);    // objType(*?) Dictionary<string, int> string int*
                    Emit.Call(tryGetValue);         // objType(*?) bool
                    Emit.BranchIfTrue(isMember);    // objType(*?)

                    Emit.Pop();                     // --empty--
                    SkipObjectMember();             // --empty--
                    Emit.Branch(loopStart);         // --empty--

                    Emit.MarkLabel(isMember);       // objType(*?)
                    Emit.LoadLocal(oLoc);           // objType(*?) int
                    Emit.Switch(inOrderLabels);     // objType(*?)

                    // fallthrough case
                    ThrowExpected("a member name"); // --empty--
                }

                foreach (var kv in labels)
                {
                    var memberName = kv.Key;
                    var label = kv.Value;
                    var members = setters[kv.Key];

                    if (members.Length == 1)
                    {
                        var member = members[0];

                        Emit.MarkLabel(label);      // objType(*?)
                        ReadAndSetMember(member);   // --empty--
                        Emit.Branch(loopStart);     // --empty--
                    }
                    else
                    {
                        Emit.MarkLabel(label);                              // objType(*?)
                        ReadAndSetDiscriminantUnion(memberName, members);   // --empty--
                        Emit.Branch(loopStart);                             // --empty--
                    }
                }

                var nextItem = Emit.DefineLabel(GetNextName());

                Emit.MarkLabel(loopStart);              // --empty--
                loadObj();                              // objType(*?)
                ReadSkipWhitespace();                   // objType(*?) int 
                ThrowIfEmptyAndWasExpecting(",", "}");  // objType(*?) int

                Emit.Duplicate();                       // objType(*?) int int
                Emit.LoadConstant(',');                 // objType(*?) int int ','
                Emit.BranchIfEqual(nextItem);           // objType(*?) int
                Emit.LoadConstant('}');                 // objType(*?) int '}'
                Emit.BranchIfEqual(doneSkipChar);       // objType(*?)

                // didn't get what we expected
                ThrowExpected(",", "}");

                Emit.MarkLabel(nextItem);           // objType(*?) int
                Emit.Pop();                         // objType(*?)
                ConsumeWhiteSpace();
                Emit.LoadField(order);              // objType(*?) Dictionary<string, int> string
                Build(null, typeof(string));              // objType(*?) Dictionary<string, int> string
                ReadSkipWhitespace();               // objType(*?) Dictionary<string, int> string
                CheckChar(':');                     // objType(*?) Dictionary<string, int> string
                ConsumeWhiteSpace();                // objType(*?) Dictionary<string, int> string
                Emit.Branch(readingMember);         // objType(*?) Dictionary<string, int> string
            }

            Emit.MarkLabel(done);               // objType(*?)
            ReadCharFromStream();               // objType(*?) int
            Emit.Pop();                         // objType(*?)

            Emit.MarkLabel(doneSkipChar);       // objType(*?)

            if (objType.IsValueType())
            {
                Emit.LoadObject(objType);       // objType
            }
        }

        void ReadAnonymousObjectDictionaryLookup(Type objType)
        {
            var doneNotNull = Emit.DefineLabel(GetNextName());
            var doneNull = Emit.DefineLabel(GetNextName());

            ExpectRawCharOrNull(
                '{',
                () => { },
                () =>
                {
                    Emit.Branch(doneNull);
                }
            );

            var cons = objType.GetConstructors().Single();

            var setterLookup = typeof(AnonymousTypeLookup<>).MakeGenericType(objType);
            var propertyMap = (Dictionary<string, Tuple<Type, int>>)setterLookup.GetField("ParametersToTypeAndIndex").GetValue(null);

            if (propertyMap.Count == 0)
            {
                var doneSkipChar = Emit.DefineLabel(GetNextName());

                Emit.NewObject(cons);           // objType
                Emit.Branch(doneNotNull);       // objType

                Emit.MarkLabel(doneNull);       // null
                Emit.LoadNull();                // null
                Emit.Branch(doneSkipChar);

                Emit.MarkLabel(doneNotNull);    // objType

                var doneSkipping = Emit.DefineLabel(GetNextName());

                SkipAllMembers(doneSkipping, doneSkipChar); // objType

                return;
            }

            var order = setterLookup.GetField("Lookup", BindingFlags.Public | BindingFlags.Static);
            var tryGetValue = typeof(Dictionary<string, int>).GetMethod("TryGetValue");
            var orderInst = (Dictionary<string, int>)order.GetValue(null);

            var localMap = new Dictionary<string, Sigil.Local>();
            foreach (var kv in propertyMap)
            {
                localMap[kv.Key] = Emit.DeclareLocal(kv.Value.Item1, GetNextName());
            }

            var labels = orderInst.ToDictionary(d => d.Key, d => Emit.DefineLabel(GetNextName()));
            var inOrderLabels = labels.OrderBy(l => orderInst[l.Key]).Select(l => l.Value).ToArray();

            var loopStart = Emit.DefineLabel(GetNextName());

            ConsumeWhiteSpace();                // --empty--
            RawPeekChar();                      // int 
            Emit.LoadConstant('}');             // int '}'
            Emit.BranchIfEqual(doneNotNull);    // --empty--
            Emit.LoadField(order);              // Dictionary<string, int> string
            Build(null, typeof(string));              // Dictionary<string, int> string
            ReadSkipWhitespace();               // Dictionary<string, int> string
            CheckChar(':');                     // Dictionary<string, int> string
            ConsumeWhiteSpace();                // Dictionary<string, int> string

            var readingMember = Emit.DefineLabel(GetNextName());
            Emit.MarkLabel(readingMember);      // Dictionary<string, int> string

            using (var oLoc = Emit.DeclareLocal<int>(GetNextName()))
            {
                var isMember = Emit.DefineLabel(GetNextName());

                Emit.LoadLocalAddress(oLoc);    // Dictionary<string, int> string int*
                Emit.Call(tryGetValue);         // bool
                Emit.BranchIfTrue(isMember);    // --empty--

                SkipObjectMember();             // --empty--
                Emit.Branch(loopStart);         // --empty--

                Emit.MarkLabel(isMember);       // --empty--
                Emit.LoadLocal(oLoc);           // int
                Emit.Switch(inOrderLabels);     // --empty--

                // fallthrough case
                ThrowExpected("a member name"); // --empty--
            }

            foreach (var kv in labels)
            {
                var label = kv.Value;
                var local = localMap[kv.Key];

                Emit.MarkLabel(label);  // --empty--
                Build(null, local.LocalType); // localType

                Emit.StoreLocal(local); // --empty--

                Emit.Branch(loopStart); // --empty--
            }

            var nextItem = Emit.DefineLabel(GetNextName());

            Emit.MarkLabel(loopStart);              // --empty--
            ConsumeWhiteSpace();                    // --empty--
            RawPeekChar();                          // int 
            ThrowIfEmptyAndWasExpecting(",", "}");  // int

            Emit.Duplicate();                   // int int
            Emit.LoadConstant(',');             // int int ','
            Emit.BranchIfEqual(nextItem);       // int
            Emit.LoadConstant('}');             // int '}'
            Emit.BranchIfEqual(doneNotNull);    // --empty--

            // didn't get what we expected
            ThrowExpected(",", "}");

            Emit.MarkLabel(nextItem);           // int
            Emit.Pop();                         // --empty--
            ReadCharFromStream();               // int
            Emit.Pop();                         // --empty--
            ConsumeWhiteSpace();                // --empty--
            Emit.LoadField(order);              // Dictionary<string, int> string
            Build(null, typeof(string));              // Dictionary<string, int> string
            ReadSkipWhitespace();               // Dictionary<string, int> string
            CheckChar(':');                     // Dictionary<string, int> string
            ConsumeWhiteSpace();                // Dictionary<string, int> string
            Emit.Branch(readingMember);         // Dictionary<string, int> string

            Emit.MarkLabel(doneNotNull);        // --empty--
            ReadCharFromStream();               // int
            Emit.Pop();                         // --empty--

            var done = Emit.DefineLabel(GetNextName());

            foreach (var kv in propertyMap.OrderBy(o => o.Value.Item2))
            {
                var local = localMap[kv.Key];
                Emit.LoadLocal(local);
            }

            Emit.NewObject(cons);               // objType
            Emit.Branch(done);                  // objType

            Emit.MarkLabel(doneNull);           // --empty--
            Emit.LoadNull();                    // null

            Emit.MarkLabel(done);               // objType

            // free up all those locals for use again
            foreach (var kv in localMap)
            {
                kv.Value.Dispose();
            }
        }

        void ReadAnonymousObjectAutomata(Type objType)
        {
            var doneNotNull = Emit.DefineLabel(GetNextName());
            var doneNotNullPopSkipChar = Emit.DefineLabel(GetNextName());
            var doneNotNullSkipChar = Emit.DefineLabel(GetNextName());
            var doneNull = Emit.DefineLabel(GetNextName());

            ExpectRawCharOrNull(
                '{',
                () => { },
                () =>
                {
                    Emit.Branch(doneNull);
                }
            );

            var cons = objType.GetConstructors().Single();

            var setterLookup = typeof(AnonymousTypeLookup<>).MakeGenericType(objType);

            MethodInfo findConstructorParameterIndex;
            if (ReadingFromString)
            {
                findConstructorParameterIndex = setterLookup.GetMethod("FindConstructorParameterIndexThunkReader", new[] { typeof(ThunkReader).MakeByRefType() });
            }
            else
            {
                findConstructorParameterIndex = setterLookup.GetMethod("FindConstructorParameterIndex", new[] { typeof(TextReader) });
            }

            var propertyMap = (Dictionary<string, Tuple<Type, int>>)setterLookup.GetField("ParametersToTypeAndIndex").GetValue(null);

            if (propertyMap.Count == 0)
            {
                var doneSkipChar = Emit.DefineLabel(GetNextName());

                Emit.NewObject(cons);           // objType
                Emit.Branch(doneNotNull);       // objType

                Emit.MarkLabel(doneNull);       // null
                Emit.LoadNull();                // null
                Emit.Branch(doneSkipChar);

                Emit.MarkLabel(doneNotNull);    // objType

                var doneSkipping = Emit.DefineLabel(GetNextName());

                SkipAllMembers(doneSkipping, doneSkipChar); // objType

                return;
            }

            var localMap = new Dictionary<string, Sigil.Local>();
            foreach (var kv in propertyMap)
            {
                localMap[kv.Key] = Emit.DeclareLocal(kv.Value.Item1, GetNextName());
            }

            var labels =
                propertyMap
                .ToDictionary(d => d.Key, d => Emit.DefineLabel(GetNextName()));

            var inOrderLabels =
                propertyMap
                .OrderBy(l => l.Value.Item2)
                .Select(l => labels[l.Key])
                .ToArray();

            var loopStart = Emit.DefineLabel(GetNextName());

            ReadSkipWhitespace();                       // int 
            Emit.Duplicate();
            Emit.LoadConstant('}');                     // int '}'
            Emit.BranchIfEqual(doneNotNullPopSkipChar); // --empty--

            CheckQuote();                               // --empty--
            Emit.LoadArgument(0);                       // TextReader
            Emit.Call(findConstructorParameterIndex);   // int

            ReadSkipWhitespace();       // int
            CheckChar(':');             // int
            ConsumeWhiteSpace();        // int

            var readingMember = Emit.DefineLabel(GetNextName());
            Emit.MarkLabel(readingMember);  // int

            Emit.Switch(inOrderLabels);     // --empty--
            SkipObjectMember();             // --empty--

            foreach (var kv in labels)
            {
                var label = kv.Value;
                var local = localMap[kv.Key];

                Emit.MarkLabel(label);  // --empty--
                Build(null, local.LocalType); // localType

                Emit.StoreLocal(local); // --empty--

                Emit.Branch(loopStart); // --empty--
            }

            var nextItem = Emit.DefineLabel(GetNextName());

            Emit.MarkLabel(loopStart);                  // --empty--
            ReadSkipWhitespace();                       // int
            ThrowIfEmptyAndWasExpecting(",", "}");      // int

            Emit.Duplicate();                           // int int
            Emit.LoadConstant(',');                     // int int ','
            Emit.BranchIfEqual(nextItem);               // int
            Emit.LoadConstant('}');                     // int '}'
            Emit.BranchIfEqual(doneNotNullSkipChar);    // --empty--

            // didn't get what we expected
            ThrowExpected(",", "}");

            Emit.MarkLabel(nextItem);                   // int
            Emit.Pop();                                 // --empty--
            ReadSkipWhitespace();                       // --empty--

            CheckQuote();                               // --empty--
            Emit.LoadArgument(0);                       // TextReader
            Emit.Call(findConstructorParameterIndex);   // int

            ReadSkipWhitespace();               // int
            CheckChar(':');                     // int
            ConsumeWhiteSpace();                // int
            Emit.Branch(readingMember);         // int

            Emit.MarkLabel(doneNotNull);        // --empty--
            ReadCharFromStream();               // int

            Emit.MarkLabel(doneNotNullPopSkipChar);
            Emit.Pop();                         // --empty--

            Emit.MarkLabel(doneNotNullSkipChar);// --empty--

            var done = Emit.DefineLabel(GetNextName());

            foreach (var kv in propertyMap.OrderBy(o => o.Value.Item2))
            {
                var local = localMap[kv.Key];
                Emit.LoadLocal(local);
            }

            Emit.NewObject(cons);               // objType
            Emit.Branch(done);                  // objType

            Emit.MarkLabel(doneNull);           // --empty--
            Emit.LoadNull();                    // null

            Emit.MarkLabel(done);               // objType

            // free up all those locals for use again
            foreach (var kv in localMap)
            {
                kv.Value.Dispose();
            }
        }

        static ConstructorInfo OptionsCons = typeof(Options).GetConstructor(new[] { typeof(bool), typeof(bool), typeof(bool), typeof(DateTimeFormat), typeof(bool), typeof(UnspecifiedDateTimeKindBehavior), typeof(SerializationNameFormat) });
        static ConstructorInfo ObjectBuilderCons = typeof(Jil.DeserializeDynamic.ObjectBuilder).GetConstructor(new[] { typeof(Options) });
        void ReadDynamic()
        {
            using (var dyn = Emit.DeclareLocal<DeserializeDynamic.ObjectBuilder>(GetNextName()))
            {
                Emit.LoadArgument(0);                                                       // TextReader
                Emit.LoadConstant(false);                                                   // TextReader bool
                Emit.LoadConstant(false);                                                   // TextReader bool bool
                Emit.LoadConstant(false);                                                   // TextReader bool bool bool
                Emit.LoadConstant((byte)DateFormat);                                        // TextReader bool bool bool byte
                Emit.LoadConstant(false);                                                   // TextReader bool bool bool byte bool
                Emit.LoadConstant((byte)UnspecifiedDateTimeKindBehavior.IsLocal);           // TextReader bool bool bool byte bool byte
                Emit.LoadConstant((byte)SerializationNameFormat.Verbatim);                  // TextReader bool bool bool byte bool byte byte
                Emit.NewObject(OptionsCons);                                                // TextReader Options
                Emit.NewObject(ObjectBuilderCons);                                          // TextReader ObjectBuilder
                Emit.StoreLocal(dyn);                                                       // TextReader
                Emit.LoadLocal(dyn);                                                        // TextReader ObjectBuilder

                var deserializeDyn = Jil.DeserializeDynamic.DynamicDeserializer.GetDeserializeMember(ReadingFromString);

                Emit.Call(deserializeDyn);                                                  // --empty--
                Emit.LoadLocal(dyn);                                                        // ObjectBuilder
                Emit.LoadField(Jil.DeserializeDynamic.ObjectBuilder._BeingBuilt);           // JsonObject
            }
        }

        void Build(MemberInfo forMember, Type forType, bool allowRecursion = true, bool fromNullable = false)
        {
            // EXACT MATCH, this is the best way to detect `dynamic`
            if (forType == typeof(object))
            {
                ReadDynamic();
                return;
            }

            if (forType.IsNullableType())
            {
                ReadNullable(forMember, forType);
                return;
            }

            if (forType.IsPrimitiveType())
            {
                ReadPrimitive(forType);
                return;
            }

            if (forType.IsPrimitiveWrapper())
            {
                ReadPrimitiveTypeWrapper(forType);
                return;
            }

            if (forType.IsDictionaryType())
            {
                ReadDictionary(forMember, forType);
                return;
            }

            if (forType.IsGenericReadOnlyDictionary())
            {
                var keyType = forType.GetGenericArguments()[0];
                var valueType = forType.GetGenericArguments()[1];
                var fakeDictionary = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                ReadDictionary(forMember, fakeDictionary);
                return;
            }

            if (forType.IsListType())
            {
                ReadList(forMember, forType, false);
                return;
            }

            if (forType.IsSetType())
            {
                ReadList(forMember, forType, true);
                return;
            }

            // Final, special, case for ICollection<T>, IEnumerable<X>, and IReadOnlyList<T> if *not* a List<T> or an ISet<T>
            // We can make this work by just acting like it *is* a List<X>
            if (forType.IsConstructedGenericType)
            {
                var genTypeDef = forType.GetGenericTypeDefinition();
                if (genTypeDef == typeof(ICollection<>) || genTypeDef == typeof(IEnumerable<>) || genTypeDef == typeof(IReadOnlyList<>))
                {
                    var elementType = forType.GetGenericArguments()[0];
                    var fakeList = typeof(List<>).MakeGenericType(elementType);
                    ReadList(forMember, fakeList, false);
                    return;
                }
            }

            if (forType.IsEnum())
            {
                Type convertEnumTo;
                if (forMember != null && forMember.ShouldConvertEnum(forType, out convertEnumTo))
                {
                    var underlyingEnumType = Enum.GetUnderlyingType(forType);

                    Build(forMember, convertEnumTo);
                    Emit.Convert(underlyingEnumType);
                    return;
                }

                ReadEnum(forType, fromNullable);
                return;
            }

            if (allowRecursion && IndirectTypes.Contains(forType))
            {
                Type funcType;

                if (ReadingFromString)
                {
                    funcType = typeof(StringThunkDelegate<>).MakeGenericType(forType);
                }
                else
                {
                    funcType = typeof(Func<,,>).MakeGenericType(typeof(TextReader), typeof(int), forType);
                }
                var funcInvoke = funcType.GetMethod("Invoke");

                LoadRecursiveTypeDelegate(forType); // Func<TextReader, int, memberType>
                Emit.LoadArgument(0);               // Func<TextReader, int, memberType> TextReader
                Emit.LoadArgument(1);               // Func<TextReader, int, memberType> TextReader int
                Emit.LoadConstant(1);               // Func<TextReader, int, memberType> TextReader int int
                Emit.Add();                         // Func<TextReader, int, memberType> TextReader int
                Emit.Call(funcInvoke);              // memberType
                return;
            }

            if (forType.IsSimpleInterface())
            {
                var interfaceImpl = typeof(InterfaceImplementation<>).MakeGenericType(forType);
                var interfaceImplType = (Type)interfaceImpl.GetField("Proxy").GetValue(null);

                ReadObject(interfaceImplType);
                return;
            }

            ReadObject(forType);
        }

        HashSet<Type> FindAndPrimeIndirectTypes(Type forType)
        {
            var ret = forType.FindRecursiveOrReusedTypes();

            if(PreferIndirectSerialization)
            {
                var subTypes = forType.FindChildTypes();
                foreach(var t in subTypes)
                {
                    ret.Add(t);
                }
            }


            foreach (var primeType in ret)
            {
                MethodInfo loadMtd;
                if (ReadingFromString)
                {
                    loadMtd = typeof(TypeCache<,>).MakeGenericType(OptionsType, primeType).GetMethod("LoadFromString", BindingFlags.Public | BindingFlags.Static);
                }
                else
                {
                    loadMtd = typeof(TypeCache<,>).MakeGenericType(OptionsType, primeType).GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
                }

                loadMtd.Invoke(null, new object[0]);
            }

            return ret;
        }

        void BuildWithNew(Type forType)
        {
            AddGlobalVariables();

            ConsumeWhiteSpace();

            Build(null, forType, allowRecursion: false);

            // we have to consume this, otherwise we might succeed with invalid JSON
            ConsumeWhiteSpace();

            // We also must confirm that we read everything, again otherwise we might accept garbage as valid
            ExpectEndOfStream();

            Emit.Return();
        }

        public Func<TextReader, int, ForType> BuildWithNewDelegate(out int approximateILCount)
        {
            var forType = typeof(ForType);

            IndirectTypes = FindAndPrimeIndirectTypes(forType);

            Emit = Emit.NewDynamicMethod(forType, new[] { typeof(TextReader), typeof(int) }, doVerify: Utils.DoVerify);

            BuildWithNew(forType);

            var ret = Emit.CreateDelegate<Func<TextReader, int, ForType>>(out var il, Utils.DelegateOptimizationOptions);

            approximateILCount = il.Count(c => c == '\n');  // VERY approximate

            return ret;
        }

        public StringThunkDelegate<ForType> BuildFromStringWithNewDelegate(out int approximateILCount)
        {
            var forType = typeof(ForType);

            IndirectTypes = FindAndPrimeIndirectTypes(forType);

            Emit = Emit.NewDynamicMethod(forType, new[] { typeof(ThunkReader).MakeByRefType(), typeof(int) }, doVerify: Utils.DoVerify);

            BuildWithNew(forType);

            var ret = Emit.CreateDelegate<StringThunkDelegate<ForType>>(out var il, Utils.DelegateOptimizationOptions);

            approximateILCount = il.Count(c => c == '\n');  // VERY approximate

            return ret;
        }
    }

    static class InlineDeserializerHelper
    {
        static Func<TextReader, int, ReturnType> BuildAlwaysFailsWithFromStream<ReturnType>(Type optionsType)
        {
            var specificTypeCache = typeof(TypeCache<,>).MakeGenericType(optionsType, typeof(ReturnType));
            var stashField = specificTypeCache.GetField("ExceptionDuringBuildFromStream", BindingFlags.Static | BindingFlags.Public);

            var emit = Emit.NewDynamicMethod(typeof(ReturnType), new[] { typeof(TextReader), typeof(int) });
            emit.LoadConstant("Error occurred building a deserializer for " + typeof(ReturnType));
            emit.LoadField(stashField);
            emit.LoadConstant(false);
            emit.NewObject<DeserializationException, string, Exception, bool>();
            emit.Throw();

            return emit.CreateDelegate<Func<TextReader, int, ReturnType>>(Utils.DelegateOptimizationOptions);
        }

        static StringThunkDelegate<ReturnType> BuildAlwaysFailsWithFromString<ReturnType>(Type optionsType)
        {
            var specificTypeCache = typeof(TypeCache<,>).MakeGenericType(optionsType, typeof(ReturnType));
            var stashField = specificTypeCache.GetField("ExceptionDuringBuildFromString", BindingFlags.Static | BindingFlags.Public);

            var emit = Emit.NewDynamicMethod(typeof(ReturnType), new[] { typeof(ThunkReader).MakeByRefType(), typeof(int) });
            emit.LoadConstant("Error occurred building a deserializer for " + typeof(ReturnType));
            emit.LoadField(stashField);
            emit.LoadConstant(false);
            emit.NewObject<DeserializationException, string, Exception, bool>();
            emit.Throw();

            return emit.CreateDelegate<StringThunkDelegate<ReturnType>>(Utils.DelegateOptimizationOptions);
        }

        public static Func<TextReader, int, ReturnType> BuildFromStream<ReturnType>(Type optionsType, DateTimeFormat dateFormat, SerializationNameFormat serializationNameFormat, out Exception exceptionDuringBuild)
        {
            var obj = new InlineDeserializer<ReturnType>(optionsType, dateFormat, serializationNameFormat, readingFromString: false, preferIndirectSerialization: false, allPrimitivesThroughHelpers: false);

            Func<TextReader, int, ReturnType> ret;
            try
            {
                ret = obj.BuildWithNewDelegate(out var size);

                if(size > Utils.MAX_IL_INSTRUCTION_LIMIT)
                {
                    if (typeof(ReturnType).FindChildTypes().Any())
                    {
                        obj = new InlineDeserializer<ReturnType>(optionsType, dateFormat, serializationNameFormat, readingFromString: false, preferIndirectSerialization: true, allPrimitivesThroughHelpers: false);
                        ret = obj.BuildWithNewDelegate(out size);
                    }

                    if(size > Utils.MAX_IL_INSTRUCTION_LIMIT)
                    {
                        obj = new InlineDeserializer<ReturnType>(optionsType, dateFormat, serializationNameFormat, readingFromString: false, preferIndirectSerialization: true, allPrimitivesThroughHelpers: true);
                        ret = obj.BuildWithNewDelegate(out size);
                    }
                }

                exceptionDuringBuild = null;
            }
            catch (ConstructionException e)
            {
                exceptionDuringBuild = e;
                ret = BuildAlwaysFailsWithFromStream<ReturnType>(optionsType);
            }

            return ret;
        }

        public static StringThunkDelegate<ReturnType> BuildFromString<ReturnType>(Type optionsType, DateTimeFormat dateFormat, SerializationNameFormat serializationNameFormat, out Exception exceptionDuringBuild)
        {
            var obj = new InlineDeserializer<ReturnType>(optionsType, dateFormat, serializationNameFormat, readingFromString: true, preferIndirectSerialization: false, allPrimitivesThroughHelpers: false);

            StringThunkDelegate<ReturnType> ret;
            try
            {
                ret = obj.BuildFromStringWithNewDelegate(out var size);

                if (size > Utils.MAX_IL_INSTRUCTION_LIMIT)
                {
                    if (typeof(ReturnType).FindChildTypes().Any())
                    {
                        obj = new InlineDeserializer<ReturnType>(optionsType, dateFormat, serializationNameFormat, readingFromString: true, preferIndirectSerialization: true, allPrimitivesThroughHelpers: false);
                        ret = obj.BuildFromStringWithNewDelegate(out size);
                    }

                    if (size > Utils.MAX_IL_INSTRUCTION_LIMIT)
                    {
                        obj = new InlineDeserializer<ReturnType>(optionsType, dateFormat, serializationNameFormat, readingFromString: true, preferIndirectSerialization: true, allPrimitivesThroughHelpers: true);
                        ret = obj.BuildFromStringWithNewDelegate(out size);
                    }
                }

                exceptionDuringBuild = null;
            }
            catch (ConstructionException e)
            {
                exceptionDuringBuild = e;
                ret = BuildAlwaysFailsWithFromString<ReturnType>(optionsType);
            }

            return ret;
        }
    }
}
