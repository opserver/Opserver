using Sigil;
using System;
using System.Collections;

namespace Jil.Common
{
    class FlagsEnumCombiner<TEnum>
        where TEnum : struct
    {
        public static readonly Func<TEnum, TEnum, TEnum> Combine;

        static FlagsEnumCombiner()
        {
            {
                var emit = Emit<Func<TEnum, TEnum, TEnum>>.NewDynamicMethod(doVerify: Utils.DoVerify);
                emit.LoadArgument(0);       // enum
                emit.LoadArgument(1);       // enum enum
                emit.Or();                  // enum
                emit.Return();              // --empty--

                Combine = emit.CreateDelegate(Utils.DelegateOptimizationOptions);
            }
        }
    }

    class FlagsEnumCombiner
    {
        delegate void CombineInPlaceDelegate(object newFlag, object accumFlag);
        static readonly Hashtable CombineInPlaceCache = new Hashtable();

        public static void CombineInPlace(Type enumType, object a, object b)
        {
            var cached = (CombineInPlaceDelegate)CombineInPlaceCache[enumType];
            if (cached != null)
            {
                cached(a, b);
                return;
            }

            var emit = Emit<CombineInPlaceDelegate>.NewDynamicMethod(doVerify: Utils.DoVerify);
            emit.LoadArgument(1);               // object
            emit.Unbox(enumType);               // enum&
            emit.LoadArgument(0);               // enum& object
            emit.UnboxAny(enumType);            // enum& enum
            emit.LoadArgument(1);               // enum& enum object
            emit.UnboxAny(enumType);            // enum& enum enum
            emit.Or();                          // enum& enum
            emit.StoreObject(enumType);         // --empty--
            emit.Return();

            var newDel = emit.CreateDelegate(Utils.DelegateOptimizationOptions);

            lock (CombineInPlaceCache)
            {
                cached = (CombineInPlaceDelegate)CombineInPlaceCache[enumType];
                if (cached == null)
                {
                    CombineInPlaceCache[enumType] = cached = newDel;
                }
            }

            cached(a, b);
        }
    }
}
