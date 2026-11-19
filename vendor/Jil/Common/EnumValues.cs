using Sigil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jil.Common
{
    static class EnumValues<TEnum>
        where TEnum : struct
    {
        static readonly Dictionary<string, TEnum> PreCalculated;

        static EnumValues()
        {
            PreCalculated = new Dictionary<string, TEnum>(StringComparer.OrdinalIgnoreCase);

            foreach (var val in Enum.GetValues(typeof(TEnum)))
            {
                var name = typeof(TEnum).GetEnumValueName(val);
                PreCalculated[name] = (TEnum)val;
            }
        }

        public static bool TryParse(string str, out TEnum ret)
        {
            return PreCalculated.TryGetValue(str, out ret);
        }
    }

    static class EnumValues
    {
        delegate bool TryParseProxy(string str, out object val);

        static readonly Hashtable Cache = new Hashtable();

        static int NextName = 0;
        static string GetNextName()
        {
            var r = Interlocked.Increment(ref NextName);

            return "ev_n" + r;
        }

        public static bool TryParse(Type enumType, string str, out object ret)
        {
            var invoke = (TryParseProxy)Cache[enumType];
            if (invoke != null) return invoke(str, out ret);

            var emit = Emit<TryParseProxy>.NewDynamicMethod(doVerify: Utils.DoVerify);
            var specific = typeof(EnumValues<>).MakeGenericType(enumType).GetMethod("TryParse", BindingFlags.Public | BindingFlags.Static);

            using (var loc = emit.DeclareLocal(enumType, GetNextName()))
            {
                emit.LoadArgument(0);           // string
                emit.LoadLocalAddress(loc);     // string enum&
                emit.Call(specific);            // bool
                emit.LoadArgument(1);           // bool object&
                emit.LoadLocal(loc);            // bool object& enum
                emit.Box(enumType);             // bool object& object
                emit.StoreIndirect<object>();   // bool
                emit.Return();                  // --empty--
            }

            var parse = emit.CreateDelegate(Utils.DelegateOptimizationOptions);

            lock (Cache)
            {
                if (Cache.ContainsKey(enumType))
                {
                    parse = (TryParseProxy)Cache[enumType];
                }
                else
                {
                    Cache[enumType] = parse;
                }
            }

            return parse(str, out ret);
        }
    }
}
