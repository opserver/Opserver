using System;
using Jil.Common;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using System.Runtime.CompilerServices;
using Jil.Serialize;
using System.Collections;
using Sigil.NonGeneric;
using System.Reflection;

namespace Jil.SerializeDynamic
{
    class DynamicSerializer
    {
        static readonly Hashtable GetGetMemberCache = new Hashtable();
        static Func<object, object> GetGetMember(Type type, string memberName)
        {
            var key = Tuple.Create(type, memberName);
            var cached = (Func<object, object>)GetGetMemberCache[key];
            if (cached != null) return cached;

            var binder = (GetMemberBinder)Microsoft.CSharp.RuntimeBinder.Binder.GetMember(0, memberName, type, new[] { CSharpArgumentInfo.Create(0, null) });
            var callSite = CallSite<Func<CallSite, object, object>>.Create(binder);

            lock (GetGetMemberCache)
            {
                cached = (Func<object, object>)GetGetMemberCache[key];
                if (cached != null) return cached;

                GetGetMemberCache[key] = cached = (obj => callSite.Target.Invoke(callSite, obj));
            }

            return cached;
        }

        static string EnforceSerializationName(string name, Options opts)
        {
            if (opts.SerializationNameFormat == SerializationNameFormat.Verbatim) return name;

            return name.ToCamelCase();
        }

        static readonly ParameterExpression CachedParameterExp = Expression.Parameter(typeof(object));
        static void SerializeDynamicObject(IDynamicMetaObjectProvider dyn, TextWriter stream, Options opts, int depth)
        {
            var quoteColon = "\":";
            if (opts.ShouldPrettyPrint)
            {
                quoteColon = "\": ";
            }

            stream.Write('{');
            depth++;

            var asJilDyn = dyn as Jil.DeserializeDynamic.JsonObject;
            if (asJilDyn != null)
            {
                var first = true;
                foreach (var rawMemberName in asJilDyn.GetMemberNames())
                {
                    var val = asJilDyn.GetMember(rawMemberName);
                    var memberName = EnforceSerializationName(rawMemberName, opts);

                    if (val == null && opts.ShouldExcludeNulls) continue;

                    if (!first)
                    {
                        stream.Write(',');
                    }
                    first = false;

                    if (opts.ShouldPrettyPrint)
                    {
                        LineBreakAndIndent(stream, depth);
                    }

                    stream.Write('"');
                    memberName.JsonEscapeFast(jsonp: opts.IsJSONP, output: stream);
                    stream.Write(quoteColon);

                    Serialize(stream, val, opts, depth + 1);
                }

                depth--;
                if (opts.ShouldPrettyPrint)
                {
                    LineBreakAndIndent(stream, depth);
                }
            }
            else
            {
                var dynType = dyn.GetType();
                var metaObj = dyn.GetMetaObject(CachedParameterExp);

                var first = true;
                foreach (var rawMemberName in metaObj.GetDynamicMemberNames())
                {
                    var getter = GetGetMember(dynType, rawMemberName);
                    var memberName = EnforceSerializationName(rawMemberName, opts);
                    var val = getter(dyn);

                    if (val == null && opts.ShouldExcludeNulls) continue;

                    if (!first)
                    {
                        stream.Write(',');
                    }
                    first = false;

                    if (opts.ShouldPrettyPrint)
                    {
                        LineBreakAndIndent(stream, depth);
                    }

                    stream.Write('"');

                    memberName.JsonEscapeFast(jsonp: opts.IsJSONP, output: stream);
                    stream.Write(quoteColon);

                    Serialize(stream, val, opts, depth + 1);
                }

                depth--;
                if (opts.ShouldPrettyPrint)
                {
                    LineBreakAndIndent(stream, depth);
                }
            }

            stream.Write('}');
        }

        static void LineBreakAndIndent(TextWriter stream, int depth)
        {
            stream.Write('\n');

            switch(depth){
                case 0: return;
                case 1: stream.Write(' '); return;
                case 2: stream.Write("  "); return;
                case 3: stream.Write("   "); return;
                case 4: stream.Write("    "); return;
                case 5: stream.Write("     "); return;
                case 6: stream.Write("      "); return;
                case 7: stream.Write("       "); return;
                case 8: stream.Write("        "); return;
                case 9: stream.Write("         "); return;
                case 10: stream.Write("          "); return;
            }

            for (var i = 0; i < depth; i++)
            {
                stream.Write(' ');
            }
        }

        static readonly Hashtable GetSemiStaticInlineSerializerForCache = new Hashtable();
        static readonly MethodInfo GetSemiStaticInlineSerializerFor = typeof(DynamicSerializer).GetMethod("_GetSemiStaticInlineSerializerFor", BindingFlags.NonPublic | BindingFlags.Static);
        static Action<TextWriter, ForType, int> _GetSemiStaticInlineSerializerFor<ForType>(MemberInfo dynamicMember, Options opts)
        {
            var type = typeof(ForType);

            Type treatEnumerationAs = null;
            if (dynamicMember != null)
            {
                var attr = dynamicMember.GetCustomAttribute<JilDirectiveAttribute>();
                if (attr != null && attr.TreatEnumerationAs != null)
                {
                    treatEnumerationAs = attr.TreatEnumerationAs;
                }
            }

            var key = Tuple.Create(treatEnumerationAs, typeof(ForType), opts);

            var ret = (Action<TextWriter, ForType, int>)GetSemiStaticInlineSerializerForCache[key];
            if (ret != null) return ret;

            var cacheType = OptionsLookup.GetTypeCacheFor(opts);
            var builder = InlineSerializerHelper.BuildWithDynamism.MakeGenericMethod(type);

            lock (GetSemiStaticInlineSerializerForCache)
            {
                ret = (Action<TextWriter, ForType, int>)GetSemiStaticInlineSerializerForCache[key];
                if (ret != null) return ret;

                GetSemiStaticInlineSerializerForCache[key] = ret = (Action<TextWriter, ForType, int>)builder.Invoke(null, new object[] { dynamicMember, cacheType, opts.ShouldPrettyPrint, opts.ShouldExcludeNulls, opts.IsJSONP, opts.UseDateTimeFormat, opts.ShouldIncludeInherited, opts.UseUnspecifiedDateTimeKindBehavior, opts.SerializationNameFormat });
            }

            return ret;
        }

        static Hashtable GetSemiStaticSerializerForCache = new Hashtable();
        static Action<MemberInfo, TextWriter, object, int> GetSemiStaticSerializerFor(Type type, Options opts)
        {
            var key = Tuple.Create(type, opts);
            var ret = (Action<MemberInfo, TextWriter, object, int>)GetSemiStaticSerializerForCache[key];
            if (ret != null) return ret;

            var getSemiStaticSerializer = GetSemiStaticInlineSerializerFor.MakeGenericMethod(type);
            var invoke = typeof(Action<,,>).MakeGenericType(typeof(TextWriter), type, typeof(int)).GetMethod("Invoke");

            var emit = Emit.NewDynamicMethod(typeof(void), new[] { typeof(MemberInfo), typeof(TextWriter), typeof(object), typeof(int) }, doVerify: Utils.DoVerify);

            var optsField = OptionsLookup.GetOptionsFieldFor(opts);

            emit.LoadArgument(0);                                   // MemberInfo
            emit.LoadField(optsField);                              // MemberInfo Options
            emit.Call(getSemiStaticSerializer);                     // Action<TextWriter, Type, int>
            emit.LoadArgument(1);                                   // Action<TextWriter, Type, int> TextWriter
            emit.LoadArgument(2);                                   // Action<TextWriter, Type, int> TextWriter object

            if (type.IsValueType())
            {
                emit.UnboxAny(type);                                // Action<TextWriter, Type, int> TextWriter type
            }
            else
            {
                emit.CastClass(type);                               // Action<TextWriter, Type, int> TextWriter type
            }

            emit.LoadArgument(3);                                   // Action<TextWriter, Type, int> TextWriter type int
            emit.Call(invoke);                                      // --empty--
            emit.Return();                                          // --empty--

            lock (GetSemiStaticSerializerForCache)
            {
                ret = (Action<MemberInfo, TextWriter, object, int>)GetSemiStaticSerializerForCache[key];
                if (ret != null) return ret;

                GetSemiStaticSerializerForCache[key] = ret = emit.CreateDelegate<Action<MemberInfo, TextWriter, object, int>>(optimizationOptions: Utils.DelegateOptimizationOptions);
            }

            return ret;
        }

        static void SerializeSemiStatically(MemberInfo dynamicMember, TextWriter stream, object val, Options opts, int depth)
        {
            var serializer = GetSemiStaticSerializerFor(val.GetType(), opts);

            serializer(dynamicMember, stream, val, depth);
        }

        static void SerializeList(TextWriter stream, IEnumerable e, Options opts, int depth)
        {
            var comma = ",";
            if (opts.ShouldPrettyPrint)
            {
                comma = ", ";
            }

            bool isFirst = true;

            stream.Write("[");
            foreach (var i in e)
            {
                if (!isFirst)
                {
                    stream.Write(comma);
                }
                isFirst = false;

                Serialize(stream, i, opts, depth);
            }
            stream.Write("]");
        }

        static readonly ConvertBinder BoolConvertBinder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(bool), typeof(DynamicSerializer));
        static bool CanBeBool(object dyn, out bool bit)
        {
            var easyDyn = dyn as DynamicObject;
            if (easyDyn != null)
            {
                object ret;
                if (easyDyn.TryConvert(BoolConvertBinder, out ret))
                {
                    bit = (bool)ret;
                    return true;
                }

                bit = false;
                return false;
            }

            var jilDyn = dyn as Jil.DeserializeDynamic.JsonObject;
            if (jilDyn != null)
            {
                return jilDyn.TryCastBool(out bit);
            }

            return CanBeBoolDynamic(dyn, out bit);
        }

        static bool CanBeBoolDynamic(dynamic dyn, out bool bit)
        {
            try
            {
                // note we actually have to do a cast here, since `is` and `as` bypass the DLR
                bit = (bool)dyn;
                return true;
            }
            catch { }

            bit = false;
            return false;
        }

        static readonly ConvertBinder LongConvertBinder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(long), typeof(DynamicSerializer));
        static readonly ConvertBinder ULongConvertBinder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(ulong), typeof(DynamicSerializer));
        static bool CanBeInteger(object dyn, out ulong integer, out bool negative)
        {
            var easyDyn = dyn as DynamicObject;
            if (easyDyn != null)
            {
                object ret;
                if (easyDyn.TryConvert(LongConvertBinder, out ret))
                {
                    var asLong = (long)ret;
                    if (asLong < 0)
                    {
                        asLong = -asLong;
                        negative = true;
                    }
                    else
                    {
                        negative = false;
                    }

                    integer = (ulong)asLong;

                    return true;
                }

                if (easyDyn.TryConvert(ULongConvertBinder, out ret))
                {
                    negative = false;
                    integer = (ulong)ret;
                    return true;
                }

                integer = 0;
                negative = false;
                return false;
            }

            var jilDyn = dyn as Jil.DeserializeDynamic.JsonObject;
            if (jilDyn != null)
            {
                return jilDyn.TryCastInteger(out integer, out negative);
            }

            return CanBeIntegerDynamic(dyn, out integer, out negative);
        }

        static bool CanBeIntegerDynamic(dynamic dyn, out ulong integer, out bool negative)
        {
            try
            {
                // note we actually have to do a cast here, since `is` and `as` bypass the DLR
                var asLong = (long)dyn;

                if (asLong < 0)
                {
                    asLong = -asLong;
                    negative = true;
                }
                else
                {
                    integer = (ulong)asLong;
                    negative = false;
                }

                integer = (ulong)asLong;

                return true;
            }
            catch { }

            try
            {
                // note we actually have to do a cast here, since `is` and `as` bypass the DLR
                integer = (ulong)dyn;
                negative = false;

                return true;
            }
            catch { }

            integer = 0;
            negative = false;
            return false;
        }

        static readonly ConvertBinder FloatConvertBinder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(float), typeof(DynamicSerializer));
        static readonly ConvertBinder DoubleConvertBinder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(double), typeof(DynamicSerializer));
        static readonly ConvertBinder DecimalConvertBinder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(decimal), typeof(DynamicSerializer));
        static bool CanBeFloatingPoint(object dyn, out double floatingPoint)
        {
            var easyDyn = dyn as DynamicObject;
            if (easyDyn != null)
            {
                object ret;
                if (easyDyn.TryConvert(DoubleConvertBinder, out ret))
                {
                    floatingPoint = (double)ret;
                    return true;
                }

                if (easyDyn.TryConvert(FloatConvertBinder, out ret))
                {
                    floatingPoint = (float)ret;
                    return true;
                }

                if (easyDyn.TryConvert(DecimalConvertBinder, out ret))
                {
                    floatingPoint = (double)(decimal)ret;
                    return true;
                }

                floatingPoint = 0;
                return false;
            }

            var jilDyn = dyn as Jil.DeserializeDynamic.JsonObject;
            if (jilDyn != null)
            {
                return jilDyn.TryCastFloatingPoint(out floatingPoint);
            }

            return CanBeFloatingPointDynamic(dyn, out floatingPoint);
        }

        static bool CanBeFloatingPointDynamic(dynamic dyn, out double floatingPoint)
        {
            try
            {
                // note we actually have to do a cast here, since `is` and `as` bypass the DLR
                floatingPoint = (double)dyn;
                return true;
            }
            catch { }

            try
            {
                // note we actually have to do a cast here, since `is` and `as` bypass the DLR
                floatingPoint = (float)dyn;
                return true;
            }
            catch { }

            try
            {
                // note we actually have to do a cast here, since `is` and `as` bypass the DLR
                floatingPoint = (double)(decimal)dyn;
                return true;
            }
            catch { }

            floatingPoint = 0;
            return false;
        }

        static readonly ConvertBinder DateTimeConvertBinder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(DateTime), typeof(DynamicSerializer));
        static bool CanBeDateTime(object dyn, out DateTime dt)
        {
            var easyDyn = dyn as DynamicObject;
            if (easyDyn != null)
            {
                object ret;
                if (easyDyn.TryConvert(DateTimeConvertBinder, out ret))
                {
                    dt = (DateTime)ret;
                    return true;
                }

                dt = DateTime.MinValue;
                return false;
            }

            var jilDyn = dyn as Jil.DeserializeDynamic.JsonObject;
            if (jilDyn != null)
            {
                if (jilDyn.IsAmbiguousAsDateTime())
                {
                    dt = DateTime.MinValue;
                    return false;
                }

                return jilDyn.TryCastDateTime(out dt);
            }

            return CanBeDateTimeDynamic(dyn, out dt);
        }

        static bool CanBeDateTimeDynamic(dynamic dyn, out DateTime dt)
        {
            try
            {
                // note we actually have to do a cast here, since `is` and `as` bypass the DLR
                dt = (DateTime)dyn;
                return true;
            }
            catch { }

            dt = DateTime.MinValue;
            return false;
        }

        static readonly ConvertBinder DateTimeOffsetConvertBinder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(DateTimeOffset), typeof(DynamicSerializer));
        static bool CanBeDateTimeOffset(object dyn, out DateTimeOffset dt)
        {
            var easyDyn = dyn as DynamicObject;
            if (easyDyn != null)
            {
                object ret;
                if (easyDyn.TryConvert(DateTimeOffsetConvertBinder, out ret))
                {
                    dt = (DateTimeOffset)ret;
                    return true;
                }

                dt = DateTimeOffset.MinValue;
                return false;
            }

            var jilDyn = dyn as Jil.DeserializeDynamic.JsonObject;
            if (jilDyn != null)
            {
                if (jilDyn.IsAmbiguousAsDateTime())
                {
                    dt = DateTimeOffset.MinValue;
                    return false;
                }

                return jilDyn.TryCastDateTimeOffset(out dt);
            }

            return CanBeDateTimeOffsetDynamic(dyn, out dt);
        }

        static bool CanBeDateTimeOffsetDynamic(dynamic dyn, out DateTimeOffset dto)
        {
            try
            {
                // note we actually have to do a cast here, since `is` and `as` bypass the DLR
                dto = (DateTimeOffset)dyn;
                return true;
            }
            catch { }

            dto = DateTimeOffset.MinValue;
            return false;
        }

        static readonly ConvertBinder TimeSpanConvertBinder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(TimeSpan), typeof(DynamicSerializer));
        static bool CanBeTimeSpan(object dyn, out TimeSpan ts)
        {
            var easyDyn = dyn as DynamicObject;
            if (easyDyn != null)
            {
                object ret;
                if (easyDyn.TryConvert(TimeSpanConvertBinder, out ret))
                {
                    ts = (TimeSpan)ret;
                    return true;
                }

                ts = TimeSpan.MinValue;
                return false;
            }

            var jilDyn = dyn as Jil.DeserializeDynamic.JsonObject;
            if (jilDyn != null)
            {
                return jilDyn.TryCastTimeSpan(out ts);
            }

            return CanBeTimeSpanDynamic(dyn, out ts);
        }

        static bool CanBeTimeSpanDynamic(dynamic dyn, out TimeSpan ts)
        {
            try
            {
                // note we actually have to do a cast here, since `is` and `as` bypass the DLR
                ts = (TimeSpan)dyn;
                return true;
            }
            catch { }

            ts = TimeSpan.MinValue;
            return false;
        }

        static readonly ConvertBinder GuidConvertBinder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(Guid), typeof(DynamicSerializer));
        static bool CanBeGuid(object dyn, out Guid guid)
        {
            var easyDyn = dyn as DynamicObject;
            if (easyDyn != null)
            {
                object ret;
                if (easyDyn.TryConvert(GuidConvertBinder, out ret))
                {
                    guid = (Guid)ret;
                    return true;
                }

                guid = Guid.Empty;
                return false;
            }

            var jilDyn = dyn as Jil.DeserializeDynamic.JsonObject;
            if (jilDyn != null)
            {
                return jilDyn.TryCastGuid(out guid);
            }

            return CanBeGuidDynamic(dyn, out guid);
        }

        static bool CanBeGuidDynamic(dynamic dyn, out Guid guid)
        {
            try
            {
                // note we actually have to do a cast here, since `is` and `as` bypass the DLR
                guid = (Guid)dyn;
                return true;
            }
            catch { }

            guid = Guid.Empty;
            return false;
        }

        static readonly ConvertBinder StringConvertBinder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(string), typeof(DynamicSerializer));
        static readonly ConvertBinder CharConvertBinder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(char), typeof(DynamicSerializer));
        static bool CanBeString(object dyn, out string str)
        {
            var easyDyn = dyn as DynamicObject;
            if (easyDyn != null)
            {
                object ret;
                if (easyDyn.TryConvert(StringConvertBinder, out ret))
                {
                    str = (string)ret;
                    return true;
                }

                if (easyDyn.TryConvert(CharConvertBinder, out ret))
                {
                    str = ((char)ret).ToString();
                    return true;
                }

                str = null;
                return false;
            }

            var jilDyn = dyn as Jil.DeserializeDynamic.JsonObject;
            if(jilDyn != null)
            {
                return jilDyn.TryCastString(out str);
            }

            return CanBeStringDynamic(dyn, out str);
        }

        static bool CanBeStringDynamic(dynamic dyn, out string str)
        {
            try
            {
                // note we actually have to do a cast here, since `is` and `as` bypass the DLR
                str = (string)dyn;
                return true;
            }
            catch { }

            try
            {
                // note we actually have to do a cast here, since `is` and `as` bypass the DLR
                var c = (char)dyn;
                str = c.ToString();
                return true;
            }
            catch { }

            str = null;
            return false;
        }

        static readonly ConvertBinder IEnumerableConvertBinder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(IEnumerable), typeof(DynamicSerializer));
        static readonly ConvertBinder IDictionaryStringObjectConvertBinder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(IDictionary<string, object>), typeof(DynamicSerializer));
        static readonly ConvertBinder IDictionaryConvertBinder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(IDictionary), typeof(DynamicSerializer));
        static bool CanBeListAndNotDictionary(object dyn, out IEnumerable enumerable)
        {
            var easyDyn = dyn as DynamicObject;
            if (easyDyn != null)
            {
                object ret;
                if (easyDyn.TryConvert(IEnumerableConvertBinder, out ret))
                {
                    enumerable = (IEnumerable)ret;
                }
                else
                {
                    enumerable = null;
                    return false;
                }

                if (easyDyn.TryConvert(IDictionaryStringObjectConvertBinder, out ret)) return false;
                if (easyDyn.TryConvert(IDictionaryConvertBinder, out ret)) return false;

                return true;
            }

            var jilDyn = dyn as Jil.DeserializeDynamic.JsonObject;
            if (jilDyn != null)
            {
                if (jilDyn.TryConvertEnumerable(out enumerable) && !jilDyn.IsDictionary()) return true;

                return false;
            }

            return CanBeListAndNotDictionaryDynamic(dyn, out enumerable);
        }

        static bool CanBeListAndNotDictionaryDynamic(dynamic dyn, out IEnumerable enumerable)
        {
            try
            {
                // note we actually have to do a cast here, since `is` and `as` bypass the DLR
                enumerable = (IEnumerable)dyn;

                // if either of these succeed, we're not really a "list"
                try
                {
                    // note we actually have to do a cast here, since `is` and `as` bypass the DLR
                    var asDict1 = (IDictionary<string, object>)dyn;
                    return false;
                }
                catch { }
                try
                {
                    // note we actually have to do a cast here, since `is` and `as` bypass the DLR
                    var asDict2 = (IDictionary)dyn;
                    return false;
                }
                catch { }
                try
                {
                    var otherDictType = ((object)dyn).GetType().GetDictionaryInterface();
                    // aha, you implement IDictionary<T, V>, but do so in a way we can't cast to!
                    if (otherDictType != null)
                    {
                        var key = otherDictType.GetGenericArguments()[0];
                        // ok, your key is something we can work with, so indicate that this thing could be a Dictionary
                        if (key.IsStringyType() || key.IsEnum()) return false;
                    }
                }
                catch { }

                return true;
            }
            catch { }

            enumerable = null;
            return false;
        }

        public static readonly MethodInfo SerializeMtd = typeof(DynamicSerializer).GetMethod("Serialize");
        public static void Serialize(TextWriter stream, object obj, Options opts, int depth)
        {
            SerializeInternal(null, stream, obj, opts, depth);
        }

        public static readonly MethodInfo SerializeInternalMtd = typeof(DynamicSerializer).GetMethod("SerializeInternal", BindingFlags.NonPublic | BindingFlags.Static);
        private static void SerializeInternal(MemberInfo dynamicMember, TextWriter stream, object obj, Options opts, int depth) 
        { 
            if (obj == null)
            {
                stream.Write("null");
                return;
            }

            // if the names are camel case, we have to munge the members and _that_
            //    means we have to treat ExpandoObject and IDictionary<string, object> differently
            var canTreatExpandoAsDictionary = opts.SerializationNameFormat != SerializationNameFormat.CamelCase;

            var dynObject = obj as IDynamicMetaObjectProvider;
            // we can treat ExpandoObject as a static IDictionary<string, object> and 
            //   serialize much more quickly (no try/catch control flow)
            if (dynObject != null && !(canTreatExpandoAsDictionary && dynObject is ExpandoObject))
            {
                bool bit;
                if(CanBeBool(dynObject, out bit))
                {
                    Serialize(stream, bit, opts, depth);
                    return;
                }

                ulong integer;
                bool negative;
                if(CanBeInteger(dynObject, out integer, out negative))
                {
                    if (negative)
                    {
                        stream.Write('-');
                    }

                    Serialize(stream, integer, opts, depth);
                    return;
                }

                double floatingPoint;
                if (CanBeFloatingPoint(dynObject, out floatingPoint))
                {
                    Serialize(stream, floatingPoint, opts, depth);
                    return;
                }

                DateTimeOffset dto;
                if(CanBeDateTimeOffset(dynObject, out dto))
                {
                    Serialize(stream, dto, opts, depth);
                    return;
                }

                DateTime dt;
                if(CanBeDateTime(dynObject, out dt))
                {
                    Serialize(stream, dt, opts, depth);
                    return;
                }

                TimeSpan ts;
                if(CanBeTimeSpan(dynObject, out ts))
                {
                    Serialize(stream, ts, opts, depth);
                    return;
                }

                Guid guid;
                if (CanBeGuid(dynObject, out guid))
                {
                    Serialize(stream, guid, opts, depth);
                    return;
                }

                string str;
                if (CanBeString(dynObject, out str))
                {
                    Serialize(stream, str, opts, depth);
                    return;
                }

                IEnumerable list;
                if(CanBeListAndNotDictionary(dynObject, out list))
                {
                    SerializeList(stream, list, opts, depth);
                    return;
                }

                SerializeDynamicObject(dynObject, stream, opts, depth);
                return;
            }

            SerializeSemiStatically(dynamicMember, stream, obj, opts, depth);
        }
    }
}
