using Jil.Common;
using Sigil.NonGeneric;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Deserialize
{
    static class DeserializeIndirect
    {
        static Hashtable DeserializeFromStreamIndirectCache = new Hashtable();
        static Hashtable DeserializeFromStringIndirectCache = new Hashtable();

        static MethodInfo JSONDeserializeFromStream = typeof(JSON).GetMethod("Deserialize", new[] { typeof(TextReader), typeof(Options) });
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object DeserializeFromStream(TextReader reader, Type type, Options options)
        {
            var cached = (Func<TextReader, Options, object>)DeserializeFromStreamIndirectCache[type];
            if (cached == null)
            {
                lock(DeserializeFromStreamIndirectCache)
                {
                    cached = (Func<TextReader, Options, object>)DeserializeFromStreamIndirectCache[type];
                    if (cached == null)
                    {
                        var emit = Emit.NewDynamicMethod(typeof(object), new[] { typeof(TextReader), typeof(Options) }, doVerify: Utils.DoVerify);
                        var mtd = JSONDeserializeFromStream.MakeGenericMethod(type);

                        emit.LoadArgument(0);   // TextReader
                        emit.LoadArgument(1);   // TextReader Options
                        emit.Call(mtd);         // type
                        if (type.IsValueType())
                        {
                            emit.Box(type);     // object
                        }
                        emit.Return();

                        DeserializeFromStreamIndirectCache[type] = cached = emit.CreateDelegate<Func<TextReader, Options, object>>(Utils.DelegateOptimizationOptions);
                    }
                }
            }

            return cached(reader, options);
        }

        static MethodInfo JSONDeserializeFromString = typeof(JSON).GetMethod("Deserialize", new[] { typeof(string), typeof(Options) });
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object DeserializeFromString(string reader, Type type, Options options)
        {
            var cached = (Func<string, Options, object>)DeserializeFromStringIndirectCache[type];
            if (cached == null)
            {
                lock (DeserializeFromStringIndirectCache)
                {
                    cached = (Func<string, Options, object>)DeserializeFromStringIndirectCache[type];
                    if (cached == null)
                    {
                        var emit = Emit.NewDynamicMethod(typeof(object), new[] { typeof(string), typeof(Options) }, doVerify: Utils.DoVerify);
                        var mtd = JSONDeserializeFromString.MakeGenericMethod(type);

                        emit.LoadArgument(0);   // TextReader
                        emit.LoadArgument(1);   // TextReader Options
                        emit.Call(mtd);         // type
                        if (type.IsValueType())
                        {
                            emit.Box(type);     // object
                        }
                        emit.Return();

                        DeserializeFromStringIndirectCache[type] = cached = emit.CreateDelegate<Func<string, Options, object>>(Utils.DelegateOptimizationOptions);
                    }
                }
            }

            return cached(reader, options);
        }
    }
}
