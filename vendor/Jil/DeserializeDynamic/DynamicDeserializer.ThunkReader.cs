using Jil.Deserialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jil.DeserializeDynamic
{
    sealed partial class DynamicDeserializer
    {
        static MethodInfo DeserializeMemberThunkReader = typeof(DynamicDeserializer).GetMethod("_DeserializeMemberThunkReader", BindingFlags.NonPublic | BindingFlags.Static);
        static void _DeserializeMemberThunkReader(ref ThunkReader reader, ObjectBuilder builder)
        {
            Methods.ConsumeWhiteSpaceThunkReader(ref reader);

            var c = reader.Read();

            switch (c)
            {
                case '"': DeserializeStringThunkReader(ref reader, builder); return;
                case '[': DeserializeArrayThunkReader(ref reader, builder); return;
                case '{': DeserializeObjectThunkReader(ref reader, builder); return;
                case 'n': DeserializeNullThunkReader(ref reader, builder); return;
                case 't': DeserializeTrueThunkReader(ref reader, builder); return;
                case 'f': DeserializeFalseThunkReader(ref reader, builder); return;
                case '-': DeserializeNumberThunkReader('-', ref reader, builder); return;
            }

            if (c >= '0' && c <= '9')
            {
                DeserializeNumberThunkReader((char)c, ref reader, builder);
                return;
            }

            if (c == -1) throw new DeserializationException("Unexpected end of stream", ref reader, true);

            throw new DeserializationException("Expected \", [, {, n, t, f, -, 0, 1, 2, 3, 4, 5, 6, 7, 8, or 9; found " + (char)c, ref reader, false);
        }

        static void DeserializeTrueThunkReader(ref ThunkReader reader, ObjectBuilder builder)
        {
            var c = reader.Read();
            if (c != 'r') throw new DeserializationException("Expected r", ref reader, c == -1);
            c = reader.Read();
            if (c != 'u') throw new DeserializationException("Expected u", ref reader, c == -1);
            c = reader.Read();
            if (c != 'e') throw new DeserializationException("Expected e", ref reader, c == -1);

            builder.PutTrue();
        }

        static void DeserializeFalseThunkReader(ref ThunkReader reader, ObjectBuilder builder)
        {
            var c = reader.Read();
            if (c != 'a') throw new DeserializationException("Expected a", ref reader, c == -1);
            c = reader.Read();
            if (c != 'l') throw new DeserializationException("Expected l", ref reader, c == -1);
            c = reader.Read();
            if (c != 's') throw new DeserializationException("Expected s", ref reader, c == -1);
            c = reader.Read();
            if (c != 'e') throw new DeserializationException("Expected e", ref reader, c == -1);

            builder.PutFalse();
        }

        static void DeserializeNullThunkReader(ref ThunkReader reader, ObjectBuilder builder)
        {
            var c = reader.Read();
            if (c != 'u') throw new DeserializationException("Expected u", ref reader, c == -1);
            c = reader.Read();
            if (c != 'l') throw new DeserializationException("Expected l", ref reader, c == -1);
            c = reader.Read();
            if (c != 'l') throw new DeserializationException("Expected l", ref reader, c == -1);

            builder.PutNull();
        }

        static void DeserializeStringThunkReader(ref ThunkReader reader, ObjectBuilder builder)
        {
            var str = Methods.ReadEncodedStringWithBufferThunkReader(ref reader, builder.CommonCharBuffer, ref builder.CommonStringBuffer);

            builder.PutString(str);
        }

        static void DeserializeArrayThunkReader(ref ThunkReader reader, ObjectBuilder builder)
        {
            int c;
            builder.StartArray();

            while (true)
            {
                Methods.ConsumeWhiteSpaceThunkReader(ref reader);

                c = reader.Peek();
                if (c == -1) throw new DeserializationException("Unexpected end of stream", ref reader, true);
                if (c == ']')
                {
                    reader.Read();  // skip the ]
                    break;
                }

                _DeserializeMemberThunkReader(ref reader, builder);
                Methods.ConsumeWhiteSpaceThunkReader(ref reader);
                c = reader.Read();

                if (c == ',') continue;
                if (c == ']') break;

                throw new DeserializationException("Expected , or ], found " + (char)c, ref reader, c == -1);
            }

            builder.EndArray();
        }

        static void DeserializeObjectThunkReader(ref ThunkReader reader, ObjectBuilder builder)
        {
            int c;
            builder.StartObject();

            while (true)
            {
                Methods.ConsumeWhiteSpaceThunkReader(ref reader);

                c = reader.Peek();
                if (c == -1) throw new DeserializationException("Unexpected end of stream", ref reader, true);
                if (c == '}')
                {
                    reader.Read();  // skip }
                    break;
                }

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Unexpected end of stream", ref reader, true);
                if (c != '"') throw new DeserializationException("Expected \", found " + (char)c, ref reader, false);

                builder.StartObjectMember();
                DeserializeStringThunkReader(ref reader, builder);

                Methods.ConsumeWhiteSpaceThunkReader(ref reader);
                c = reader.Read();
                if (c == -1) throw new DeserializationException("Unexpected end of stream", ref reader, true);
                if (c != ':') throw new DeserializationException("Expected :, found " + (char)c, ref reader, false);

                _DeserializeMemberThunkReader(ref reader, builder);

                builder.EndObjectMember();

                Methods.ConsumeWhiteSpaceThunkReader(ref reader);
                c = reader.Read();

                if (c == ',') continue;
                if (c == '}') break;

                if (c == -1) throw new DeserializationException("Unexpected end of stream", ref reader, true);

                throw new DeserializationException("Expected , or }, found " + (char)c, ref reader, false);
            }

            builder.EndObject();
        }

        static void DeserializeNumberThunkReader(char leadingChar, ref ThunkReader reader, ObjectBuilder builder)
        {
            if (!UseFastNumberParsing)
            {
                var number = Methods.ReadDoubleThunkReader(leadingChar, ref reader, ref builder.CommonStringBuffer);

                builder.PutNumber(number);

                return;
            }

            bool negative;
            ulong beforeDot;
            long afterE;
            uint afterDot;
            byte afterDotLen;
            byte extraOrdersOfMagnitude;

            if (leadingChar == '-')
            {
                var next = reader.Read();
                if (next != '-' && !(next >= '0' && next <= '9')) throw new DeserializationException("Expected -, or digit", ref reader, next == -1);

                leadingChar = (char)next;
                negative = true;
            }
            else
            {
                negative = false;
            }

            beforeDot = Methods.ReadULongThunkReader(leadingChar, ref reader, out extraOrdersOfMagnitude);
            var c = reader.Peek();
            if (c == '.')
            {
                reader.Read();
                c = reader.Read();
                if (c < '0' || c > '9') throw new DeserializationException("Expected digit", ref reader, c == -1);

                afterDot = Methods.ReadUIntThunkReader((char)c, ref reader, out afterDotLen);

                c = reader.Peek();
            }
            else
            {
                afterDot = afterDotLen = 0;
            }

            if (c == 'e' || c == 'E')
            {
                reader.Read();
                c = reader.Read();
                if (c == '+')
                {
                    c = reader.Read();
                }
                if (c != '-' && !(c >= '0' || c <= '9')) throw new DeserializationException("Expected -, +, or digit", ref reader, c == -1);
                afterE = Methods.ReadLongThunkReader((char)c, ref reader);
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
                    throw new DeserializationException("Number too large to be parsed encountered", ref reader, false);
                }
            }

            builder.PutFastNumber(negative, beforeDot, afterDot, afterDotLen, afterE);
        }
    }
}
