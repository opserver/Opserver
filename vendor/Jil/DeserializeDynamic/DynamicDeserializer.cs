using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jil.DeserializeDynamic
{
    sealed partial class DynamicDeserializer
    {
        internal static bool UseFastNumberParsing = true;
        internal static bool UseFastIntegerConversion = true;

        public static ObjectBuilder Deserialize(TextReader reader, Options options)
        {
            var ret = new ObjectBuilder(options);

            _DeserializeMember(reader, ret);

            Methods.ConsumeWhiteSpace(reader);
            var c = reader.Peek();
            if (c != -1) throw new DeserializationException("Expected end of stream", reader, true);

            return ret;
        }

        public static MethodInfo GetDeserializeMember(bool readingFromString)
        {
            return
                !readingFromString ?
                    DeserializeMember :
                    DeserializeMemberThunkReader;
        }

        static MethodInfo DeserializeMember = typeof(DynamicDeserializer).GetMethod("_DeserializeMember", BindingFlags.NonPublic | BindingFlags.Static);
        static void _DeserializeMember(TextReader reader, ObjectBuilder builder)
        {
            Methods.ConsumeWhiteSpace(reader);

            var c = reader.Read();

            switch (c)
            {
                case '"': DeserializeString(reader, builder); return;
                case '[': DeserializeArray(reader, builder); return;
                case '{': DeserializeObject(reader, builder); return;
                case 'n': DeserializeNull(reader, builder); return;
                case 't': DeserializeTrue(reader, builder); return;
                case 'f': DeserializeFalse(reader, builder); return;
                case '-': DeserializeNumber('-', reader, builder); return;
            }

            if (c >= '0' && c <= '9')
            {
                DeserializeNumber((char)c, reader, builder);
                return;
            }

            if (c == -1) throw new DeserializationException("Unexpected end of stream", reader, true);

            throw new DeserializationException("Expected \", [, {, n, t, f, -, 0, 1, 2, 3, 4, 5, 6, 7, 8, or 9; found " + (char)c, reader, false);
        }

        static void DeserializeTrue(TextReader reader, ObjectBuilder builder)
        {
            var c = reader.Read();
            if (c != 'r') throw new DeserializationException("Expected r", reader, c == -1);
            c = reader.Read();
            if (c != 'u') throw new DeserializationException("Expected u", reader, c == -1);
            c = reader.Read();
            if (c != 'e') throw new DeserializationException("Expected e", reader, c == -1);

            builder.PutTrue();
        }

        static void DeserializeFalse(TextReader reader, ObjectBuilder builder)
        {
            var c = reader.Read();
            if (c != 'a') throw new DeserializationException("Expected a", reader, c == -1);
            c = reader.Read();
            if (c != 'l') throw new DeserializationException("Expected l", reader, c == -1);
            c = reader.Read();
            if (c != 's') throw new DeserializationException("Expected s", reader, c == -1);
            c = reader.Read();
            if (c != 'e') throw new DeserializationException("Expected e", reader, c == -1);

            builder.PutFalse();
        }

        static void DeserializeNull(TextReader reader, ObjectBuilder builder)
        {
            var c = reader.Read();
            if (c != 'u') throw new DeserializationException("Expected u", reader, c == -1);
            c = reader.Read();
            if (c != 'l') throw new DeserializationException("Expected l", reader, c == -1);
            c = reader.Read();
            if (c != 'l') throw new DeserializationException("Expected l", reader, c == -1);

            builder.PutNull();
        }

        static void DeserializeString(TextReader reader, ObjectBuilder builder)
        {
            var str = Methods.ReadEncodedStringWithBuffer(reader, builder.CommonCharBuffer, ref builder.CommonStringBuffer);

            builder.PutString(str);
        }

        static void DeserializeArray(TextReader reader, ObjectBuilder builder)
        {
            int c;
            builder.StartArray();

            while(true)
            {
                Methods.ConsumeWhiteSpace(reader);

                c = reader.Peek();
                if (c == -1) throw new DeserializationException("Unexpected end of stream", reader, true);
                if (c == ']')
                {
                    reader.Read();  // skip the ]
                    break;
                }

                _DeserializeMember(reader, builder);
                Methods.ConsumeWhiteSpace(reader);
                c = reader.Read();

                if(c == ',') continue;
                if(c == ']') break;

                throw new DeserializationException("Expected , or ], found "+(char)c, reader, c == -1);
            }

            builder.EndArray();
        }

        static void DeserializeObject(TextReader reader, ObjectBuilder builder)
        {
            int c;
            builder.StartObject();

            while (true)
            {
                Methods.ConsumeWhiteSpace(reader);

                c = reader.Peek();
                if (c == -1) throw new DeserializationException("Unexpected end of stream", reader, true);
                if (c == '}')
                {
                    reader.Read();  // skip }
                    break;
                }

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Unexpected end of stream", reader, true);
                if (c != '"') throw new DeserializationException("Expected \", found " + (char)c, reader, false);

                builder.StartObjectMember();
                DeserializeString(reader, builder);

                Methods.ConsumeWhiteSpace(reader);
                c = reader.Read();
                if (c == -1) throw new DeserializationException("Unexpected end of stream", reader, true);
                if (c != ':') throw new DeserializationException("Expected :, found " + (char)c, reader, false);

                _DeserializeMember(reader, builder);

                builder.EndObjectMember();

                Methods.ConsumeWhiteSpace(reader);
                c = reader.Read();

                if (c == ',') continue;
                if (c == '}') break;

                if (c == -1) throw new DeserializationException("Unexpected end of stream", reader, true);

                throw new DeserializationException("Expected , or }, found " + (char)c, reader, false);
            }

            builder.EndObject();
        }

        static void DeserializeNumber(char leadingChar, TextReader reader, ObjectBuilder builder)
        {
            if (!UseFastNumberParsing)
            {
                var number = Methods.ReadDouble(leadingChar, reader, ref builder.CommonStringBuffer);

                builder.PutNumber(number);

                return;
            }

            bool negative;
            ulong beforeDot;
            long afterE;
            uint? afterDot;
            ulong? afterDotLong;
            byte afterDotLen;
            byte extraOrdersOfMagnitude;

            if (leadingChar == '-')
            {
                var next = reader.Read();
                if (next != '-' && !(next >= '0' && next <= '9')) throw new DeserializationException("Expected -, or digit", reader, next == -1);

                leadingChar = (char)next;
                negative = true;
            }
            else
            {
                negative = false;
            }

            beforeDot = Methods.ReadULong(leadingChar, reader, out extraOrdersOfMagnitude);
            var c = reader.Peek();
            if (c == '.')
            {
                reader.Read();
                c = reader.Read();
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", reader, c == -1);

                Methods.ReadUIntOrULong((char)c, reader, out afterDotLen, out afterDot, out afterDotLong);

                c = reader.Peek();
            }
            else
            {
                afterDotLong = afterDot = afterDotLen = 0;
            }

            if (c == 'e' || c == 'E')
            {
                reader.Read();
                c = reader.Read();
                if (c == '+')
                {
                    c = reader.Read();
                }
                if (c != '-' && !(c >= '0' || c <= '9')) throw new DeserializationException("Expected -, +, or digit", reader, c == -1);
                afterE = Methods.ReadLong((char)c, reader);
            }
            else
            {
                afterE = 0;
            }

            if (extraOrdersOfMagnitude != 0)
            {
                try
                {
                    checked
                    {
                        afterE += extraOrdersOfMagnitude;
                    }
                }
                catch (OverflowException)
                {
                    throw new DeserializationException("Number too large to be parsed encountered", reader, false);
                }
            }

            // TODO: handle the case where afterDot is null, instead using the value in afterDotLong
            ulong afterDotVal = afterDot.HasValue ? afterDot.Value : afterDotLong.Value;
            builder.PutFastNumber(negative, beforeDot, afterDotVal, afterDotLen, afterE);
        }
    }
}
