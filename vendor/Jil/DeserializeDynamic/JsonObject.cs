using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Jil.DeserializeDynamic
{
    sealed partial class JsonObject : IDynamicMetaObjectProvider
    {
        enum JsonObjectType : byte
        {
            Array = 0,
            Object = 1,
            ObjectMember = 2,
            True = 3,
            False = 4,
            String = 5,
            Number = 6,
            FastNumber = 7
        }

        internal static readonly JsonObject True = new JsonObject { Type = JsonObjectType.True };
        internal static readonly JsonObject False = new JsonObject { Type = JsonObjectType.False };

        JsonObjectType Type;
        string StringValue;
        double NumberValue;
        JsonObject Parent;
        List<JsonObject> ArrayValue;
        Dictionary<string, JsonObject> ObjectMembers;

        bool FastNumberNegative;
        ulong FastNumberPart1;
        ulong FastNumberPart2;
        byte FastNumberPart2Length;
        long FastNumberPart3;

        JsonObject MemberPart1;
        JsonObject MemberPart2;

        Options Options;

        internal static JsonObject ForString(string str, Options options)
        {
            return new JsonObject { Type = JsonObjectType.String, StringValue = str, Options = options };
        }

        internal static JsonObject ForNumber(double num, Options options)
        {
            return new JsonObject { Type = JsonObjectType.Number, NumberValue = num, Options = options };
        }

        internal static JsonObject ForFastNumber(bool neg, ulong a, ulong b, byte bLen, long c, Options options)
        {
            return new JsonObject { Type = JsonObjectType.FastNumber, FastNumberNegative = neg, FastNumberPart1 = a, FastNumberPart2 = b, FastNumberPart2Length = bLen, FastNumberPart3 = c, Options = options };
        }

        internal static JsonObject NewArray(JsonObject parent)
        {
            return new JsonObject { Type = JsonObjectType.Array, Parent = parent, ArrayValue = new List<JsonObject>() };
        }

        internal static JsonObject NewObject(JsonObject parent)
        {
            return new JsonObject { Type = JsonObjectType.Object, Parent = parent, ObjectMembers = new Dictionary<string, JsonObject>() };
        }

        internal static JsonObject NewObjectMember(JsonObject parent)
        {
            return new JsonObject { Type = JsonObjectType.ObjectMember, Parent = parent };
        }

        internal JsonObject Pop()
        {
            if (Parent == null) return this;

            return Parent;
        }

        internal void Put(JsonObject other)
        {
            switch (Type)
            {
                case JsonObjectType.Array: ArrayValue.Add(other); return;
                case JsonObjectType.ObjectMember:
                    if (MemberPart1 == null)
                    {
                        MemberPart1 = other;
                    }
                    else
                    {
                        if (MemberPart2 == null)
                        {
                            MemberPart2 = other;
                            try
                            {
                                this.Parent.ObjectMembers.Add(MemberPart1.StringValue, MemberPart2);
                            }
                            catch (ArgumentException e)
                            {
                                throw new DeserializationException($"Tried to add duplicate member \"{MemberPart1.StringValue}\" to an object", e, false);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
                    return;
            }

            throw new InvalidOperationException();
        }

        public override string ToString()
        {
            return JSON.SerializeDynamic(this, Options.ISO8601PrettyPrint);
        }
    }
}
