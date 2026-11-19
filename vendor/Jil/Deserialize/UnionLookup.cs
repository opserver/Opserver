using Jil.Common;
using Sigil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Deserialize
{
    abstract class UnionLookupConfigBase
    {
        public abstract UnionCharsets Charsets { get; }
        public abstract bool AllowsNull { get; }
    }

    /// <summary>
    /// Based on the given generic parameter, this classes memebers end up as follows.
    /// 
    /// MinimumChar is the smallest legal char allowed for the lookup, when constructing a switch subtract this from the
    ///    character to be looked up.
    /// 
    /// Lookup is an array of indexes where you lookup by character (after subtracting MinimumCharacter), and get an index into
    ///     CharsetOrder.  Whatever exists at CharsetOrder is the type corresponding to the looked up character.  If the index
    ///     fetched from Lookup is out of range, that means their is no mapped type.
    /// </summary>
    static class UnionLookup<Config> where Config : UnionLookupConfigBase, new()
    {
        public readonly static int MinimumChar;
        public readonly static int[] Lookup;
        public readonly static UnionCharsets[] CharsetOrder;

        static UnionLookup()
        {
            var config = new Config();
            var charsets = config.Charsets;

            var allChars = Enumerable.Empty<char>();
            var allSets = new List<UnionCharsets>();

            if (config.AllowsNull)
            {
                allChars = allChars.Concat(UnionCharsetArrays.UnionNull);
                allSets.Add(UnionCharsets.Null);
            }

            if (charsets.HasFlag(UnionCharsets.Signed))
            {
                allChars = allChars.Concat(UnionCharsetArrays.UnionSignedSet);
                allSets.Add(UnionCharsets.Signed);
            }
            if (charsets.HasFlag(UnionCharsets.Number))
            {
                allChars = allChars.Concat(UnionCharsetArrays.UnionNumberSet);
                allSets.Add(UnionCharsets.Number);
            }
            if (charsets.HasFlag(UnionCharsets.Stringy))
            {
                allChars = allChars.Concat(UnionCharsetArrays.UnionStringySet);
                allSets.Add(UnionCharsets.Stringy);
            }
            if (charsets.HasFlag(UnionCharsets.Bool))
            {
                allChars = allChars.Concat(UnionCharsetArrays.UnionBoolSet);
                allSets.Add(UnionCharsets.Bool);
            }
            if (charsets.HasFlag(UnionCharsets.Object))
            {
                allChars = allChars.Concat(UnionCharsetArrays.UnionObjectSet);
                allSets.Add(UnionCharsets.Object);
            }
            if (charsets.HasFlag(UnionCharsets.Listy))
            {
                allChars = allChars.Concat(UnionCharsetArrays.UnionListySet);
                allSets.Add(UnionCharsets.Listy);
            }

            var inOrder = allChars.OrderBy(_ => _).ToList();

            MinimumChar = inOrder.First();
            CharsetOrder = allSets.ToArray();

            var maxChar = inOrder.Last();

            var flightSize = maxChar - MinimumChar + 1;
            
            Lookup = Enumerable.Repeat(-1, flightSize).ToArray();

            foreach (var c in allChars)
            {
                var lIx = c - MinimumChar;
                var set = UnionCharsets.None;
                if (UnionCharsetArrays.UnionSignedSet.Contains(c)) set = UnionCharsets.Signed;
                if (UnionCharsetArrays.UnionNumberSet.Contains(c)) set = UnionCharsets.Number;
                if (UnionCharsetArrays.UnionStringySet.Contains(c)) set = UnionCharsets.Stringy;
                if (UnionCharsetArrays.UnionBoolSet.Contains(c)) set = UnionCharsets.Bool;
                if (UnionCharsetArrays.UnionObjectSet.Contains(c)) set = UnionCharsets.Object;
                if (UnionCharsetArrays.UnionListySet.Contains(c)) set = UnionCharsets.Listy;
                if (UnionCharsetArrays.UnionNull.Contains(c)) set = UnionCharsets.Null;
                
                var ix = allSets.IndexOf(set);
                if (ix == -1) throw new Exception("Unexpected UnionCharsetArrays [" + set + "]");

                Lookup[lIx] = ix;
            }
        }
    }

    internal static class UnionConfigLookup
    {
        static readonly Hashtable NullCache = new Hashtable();
        static readonly Hashtable NonNullCache = new Hashtable();

        static readonly ModuleBuilder ModBuilder;
        static readonly AssemblyBuilder AsmBuilder;

        static UnionConfigLookup()
        {
            AsmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("JilUnionConfigLookupTypes"), AssemblyBuilderAccess.Run);
            ModBuilder = AsmBuilder.DefineDynamicModule("UnionConfigLookupTypes");
        }

        public static Type Get(UnionCharsets ucs, bool allowsNull)
        {
            var cache = allowsNull ? NullCache : NonNullCache;

            var cached = (Type)cache[ucs];
            if (cached != null) return cached;

            lock (cache)
            {
                cached = (Type)cache[ucs];
                if (cached != null) return cached;

                var newType = ModBuilder.DefineType(ucs.ToString()+"_"+allowsNull, TypeAttributes.NotPublic | TypeAttributes.Class, typeof(UnionLookupConfigBase));
                var field = newType.DefineField("_Charsets", typeof(UnionCharsets), FieldAttributes.Private | FieldAttributes.InitOnly);
                var prop = newType.DefineProperty("Charsets", PropertyAttributes.None, typeof(UnionCharsets), Type.EmptyTypes);

                var gcEmit = Emit<Func<UnionCharsets>>.BuildInstanceMethod(newType, "getCharsets", MethodAttributes.Public | MethodAttributes.Virtual, doVerify: Utils.DoVerify);
                gcEmit.LoadConstant((int)ucs);
                gcEmit.Convert(Enum.GetUnderlyingType(typeof(UnionCharsets)));
                gcEmit.Return();

                var gcMtd = gcEmit.CreateMethod(Utils.DelegateOptimizationOptions);
                var gcShouldOverride = typeof(UnionLookupConfigBase).GetProperty("Charsets").GetMethod;
                newType.DefineMethodOverride(gcMtd, gcShouldOverride);

                var aeEmit = Emit<Func<bool>>.BuildInstanceMethod(newType, "getAllowsNull", MethodAttributes.Public | MethodAttributes.Virtual, doVerify: Utils.DoVerify);
                aeEmit.LoadConstant(allowsNull ? 1 : 0);
                aeEmit.Convert<byte>();
                aeEmit.Return();

                var aeMtd = aeEmit.CreateMethod(Utils.DelegateOptimizationOptions);
                var aeShouldOverride = typeof(UnionLookupConfigBase).GetProperty("AllowsNull").GetMethod;
                newType.DefineMethodOverride(aeMtd, aeShouldOverride);

                var typeInfo = newType.CreateTypeInfo();
                var type = typeInfo.AsType();

                cache[ucs] = cached = type;

                return cached;
            }
        }
    }
}
