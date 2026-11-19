using Sigil;
using Sigil.NonGeneric;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Jil.Common
{
    class Utils
    {
#if DEBUG
        public const bool DoVerify = true;
#else
        public const bool DoVerify = false;
#endif
        public const OptimizationOptions DelegateOptimizationOptions = OptimizationOptions.All & ~OptimizationOptions.EnableBranchPatching;

        public const string ExceptionDataKeyPrefix = "Jil-";
        public const string ExceptionMessageDataKey = ExceptionDataKeyPrefix + nameof(Exception.Message);

        public const int MAX_IL_INSTRUCTION_LIMIT = 20_000;

        private static readonly Dictionary<int, OpCode> OneByteOps;
        private static readonly Dictionary<int, OpCode> TwoByteOps;

        static Utils()
        {
            var oneByte = new List<OpCode>();
            var twoByte = new List<OpCode>();

            foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var op = (OpCode)field.GetValue(null);

                if (op.Size == 1)
                {
                    oneByte.Add(op);
                    continue;
                }

                if (op.Size == 2)
                {
                    twoByte.Add(op);
                    continue;
                }

                throw new Exception("Unexpected op size for " + op);
            }

            OneByteOps = oneByte.ToDictionary(d => (int)d.Value, d => d);
            TwoByteOps = twoByte.ToDictionary(d => (int)(d.Value & 0xFF), d => d);
        }

        // The Experiments project was used to find these #s
        // Basically, based on some limited testing it appears that this order of
        //    member access is the "fastest" most often given the buckets I've chosen to use.
        // I make no claims that this is absolutely ideal, but it appears to be better than
        //   declaration order.
        internal static int[] MemberOrdering = new int[] { -1, 3, 4, -2 };

        public static List<List<MemberInfo>> IdealMemberOrderForWriting(Type forType, IEnumerable<Type> recursiveTypes, IEnumerable<List<MemberInfo>> members)
        {
            var fields = Utils.FieldOffsetsInMemory(forType);
            var props = Utils.PropertyFieldUsage(forType);

            var simpleTypes = members.Where(m => m.First().ReturnType().IsValueType() && !m.First().ReturnType().IsNullableType() && m.First().ReturnType().IsPrimitiveType()).ToList();
            var otherPrimitive = members.Where(m => (m.First().ReturnType().IsPrimitiveType() || m.First().ReturnType().IsNullableType()) && !simpleTypes.Contains(m)).ToList();
            var recursive = members.Where(m => recursiveTypes.Contains(m.First().ReturnType()) && !simpleTypes.Contains(m) && !otherPrimitive.Contains(m)).ToList();
            var everythingElse = members.Where(m => !simpleTypes.Contains(m) && !otherPrimitive.Contains(m) && !recursive.Contains(m)).ToList();

            Func<List<MemberInfo>, int> byAccessOrder =
                ms =>
                {
                    var m = ms.First();

                    var asField = m as FieldInfo;
                    if (asField != null)
                    {
                        int fieldAddr;
                        if (fields.TryGetValue(asField, out fieldAddr))
                        {
                            return fieldAddr;
                        }

                        return int.MaxValue;
                    }

                    var asProp = m as PropertyInfo;
                    if (asProp != null)
                    {
                        List<FieldInfo> usesFields;
                        if (!props.TryGetValue(asProp, out usesFields))
                        {
                            return int.MaxValue;
                        }

                        if (usesFields.Count == 0) return int.MaxValue;

                        return
                            usesFields.Select(
                                f =>
                                {
                                    int fieldAdd;
                                    if (fields.TryGetValue(f, out fieldAdd))
                                    {
                                        return fieldAdd;
                                    }

                                    return int.MaxValue;
                                }
                           ).Min();
                    }

                    return int.MaxValue;
                };

            Func<List<MemberInfo>, int> fieldsFirst =
                ms =>
                {
                    var m = ms.First();

                    return m is FieldInfo ? 0 : 1;
                };

            var ret = new List<List<MemberInfo>>();

            foreach (var ix in MemberOrdering)
            {
                var asc = ix > 0;
                var i = Math.Abs(ix);

                switch (i)
                {
                    case 1:
                        ret.AddRange(
                            asc ?
                                simpleTypes.OrderBy(byAccessOrder).ThenBy(fieldsFirst) :
                                simpleTypes.OrderByDescending(byAccessOrder).ThenByDescending(fieldsFirst)
                        );
                        break;

                    case 2:
                        ret.AddRange(
                            asc ?
                                otherPrimitive.OrderBy(byAccessOrder).ThenBy(fieldsFirst) :
                                otherPrimitive.OrderByDescending(byAccessOrder).ThenByDescending(fieldsFirst)
                        );
                        break;

                    case 3:
                        ret.AddRange(
                            asc ?
                                everythingElse.OrderBy(byAccessOrder).ThenBy(fieldsFirst) :
                                everythingElse.OrderByDescending(byAccessOrder).ThenByDescending(fieldsFirst)
                        );
                        break;

                    case 4:
                        ret.AddRange(
                            asc ?
                                recursive.OrderBy(byAccessOrder).ThenBy(fieldsFirst) :
                                recursive.OrderByDescending(byAccessOrder).ThenByDescending(fieldsFirst)
                        );
                        break;

                    default: throw new Exception();
                }
            }

            return ret;
        }

        public static Dictionary<PropertyInfo, List<FieldInfo>> PropertyFieldUsage(Type t)
        {
#if NETSTANDARD1_6
            return new Dictionary<PropertyInfo, List<FieldInfo>>();
#else
            var ret = new Dictionary<PropertyInfo, List<FieldInfo>>();

            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).Where(p => p.GetMethod != null && p.GetMethod.GetParameters().Count() == 0);

            var module = t.Module();

            foreach (var prop in props)
            {
                try
                {
                    var getMtd = prop.GetMethod;

                    var fieldHandles = _GetFieldHandles(getMtd);

                    var fieldInfos =
                        fieldHandles
                            .Select(
                                f =>
                                {
                                    var genArgs = t.GetGenericArguments();

                                    return module.ResolveField(f, genArgs, null);
                                }
                            ).ToList();

                    ret[prop] = fieldInfos;
                }
                catch { /* Anything that goes wrong in here, we can't really do anything about; just continue with less knowledge */ }
            }

            return ret;
#endif
        }
        
        public static List<Tuple<OpCode, int?, long?, double?, FieldInfo>> Decompile(MethodBase mtd)
        {
#if NETSTANDARD1_6
            return null;
#else
            var mtdBody = mtd.GetMethodBody();
            if (mtdBody == null) return null;
            var cil = mtdBody.GetILAsByteArray();
            if (cil == null) return null;

            var ret = new List<Tuple<OpCode, int?, long?, double?, FieldInfo>>();

            int i = 0;
            while (i < cil.Length)
            {
                int? fieldHandle;
                OpCode opcode;
                int? intOperand;
                long? longOperand;
                double? doubleOperand;
                i += _ReadOp(cil, i, out fieldHandle, out opcode, out intOperand, out longOperand, out doubleOperand);

                FieldInfo field = null;
                if (fieldHandle.HasValue)
                {
                    var genArguments = mtd.DeclaringType.GetGenericArguments();

                    field = mtd.Module.ResolveField(fieldHandle.Value, genArguments, null);
                }

                ret.Add(Tuple.Create(opcode, intOperand, longOperand, doubleOperand, field));
            }

            return ret;
#endif
        }

        private static List<int> _GetFieldHandles(MethodInfo mtd)
        {
#if NETSTANDARD1_6
            return new List<int>();
#else
            var mtdBody = mtd.GetMethodBody();
            var cil = mtdBody.GetILAsByteArray();

            var ret = new List<int>();

            int i = 0;
            while (i < cil.Length)
            {
                int? fieldHandle;
                OpCode ignoredOp;
                int? ignored1;
                long? ignored2;
                double? ignored3;
                i += _ReadOp(cil, i, out fieldHandle, out ignoredOp, out ignored1, out ignored2, out ignored3);

                if (fieldHandle.HasValue)
                {
                    ret.Add(fieldHandle.Value);
                }
            }

            return ret;
#endif
        }

        private static int _ReadOp(byte[] cil, int ix, out int? fieldHandle, out OpCode opcode, out int? intOperand, out long? longOperand, out double? doubleOperand)
        {
            const byte ContinueOpcode = 0xFE;

            int advance = 0;

            byte first = cil[ix];

            if (first == ContinueOpcode)
            {
                var next = cil[ix + 1];

                opcode = TwoByteOps[next];
                advance += 2;
            }
            else
            {
                opcode = OneByteOps[first];
                advance++;
            }

            fieldHandle = _ReadFieldOperands(opcode, cil, ix, ix + advance, ref advance, out intOperand, out longOperand, out doubleOperand);

            return advance;
        }

        private static int? _ReadFieldOperands(OpCode op, byte[] cil, int instrStart, int operandStart, ref int advance, out int? constantInt, out long? constantLong, out double? constantDouble)
        {
            Func<int, int> readInt = (at) => cil[at] | (cil[at + 1] << 8) | (cil[at + 2] << 16) | (cil[at + 3] << 24);
            Func<int, long> readLong = 
                (at) =>
                {
                    var a = (uint)(cil[at] | (cil[at + 1] << 8) | (cil[at + 2] << 16) | (cil[at + 3] << 24));
                    var b = (uint)(cil[at+4] | (cil[at + 5] << 8) | (cil[at + 6] << 16) | (cil[at + 7] << 24));

                    return (((long)b) << 32) | a;
                };
            Func<int, double> readDouble =
                (at) =>
                {
                    var arr = new byte[8];
                    arr[0] = cil[at];
                    arr[1] = cil[at + 1];
                    arr[2] = cil[at + 2];
                    arr[3] = cil[at + 3];
                    arr[4] = cil[at + 4];
                    arr[5] = cil[at + 5];
                    arr[6] = cil[at + 6];
                    arr[7] = cil[at + 7];

                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(arr);
                    }

                    return BitConverter.ToDouble(arr, 0);
                };
            Func<int, float> readFloat =
                (at) =>
                {
                    var arr = new byte[4];
                    arr[0] = cil[at];
                    arr[1] = cil[at + 1];
                    arr[2] = cil[at + 2];
                    arr[3] = cil[at + 3];

                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(arr);
                    }

                    return BitConverter.ToSingle(arr, 0);
                };

            constantInt = null;
            constantLong = null;
            constantDouble = null;

            switch (op.OperandType)
            {
                case OperandType.InlineBrTarget:
                    advance += 4;
                    return null;

                case OperandType.InlineSwitch:
                    advance += 4;
                    var len = readInt(operandStart);
                    for (var i = 0; i < len; i++)
                    {
                        advance += 4;
                    }
                    return null;

                case OperandType.ShortInlineBrTarget:
                    advance += 1;
                    return null;

                case OperandType.InlineField:
                    advance += 4;
                    var field = readInt(operandStart);
                    return field;

                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.InlineMethod:
                    advance += 4;
                    return null;

                case OperandType.InlineI:
                    advance += 4;
                    constantInt = readInt(operandStart);
                    return null;

                case OperandType.InlineI8:
                    advance += 8;
                    constantLong = readLong(operandStart);
                    return null;

                case OperandType.InlineNone:
                    return null;

                case OperandType.InlineR:
                    advance += 8;
                    constantDouble = readDouble(operandStart);
                    return null;

                case OperandType.InlineSig:
                    advance += 4;
                    return null;

                case OperandType.InlineString:
                    advance += 4;
                    constantInt = readInt(operandStart);
                    return null;

                case OperandType.InlineVar:
                    advance += 2;
                    constantInt = cil[operandStart];
                    return null;

                case OperandType.ShortInlineI:
                    advance += 1;
                    constantInt = cil[operandStart];
                    return null;

                case OperandType.ShortInlineR:
                    advance += 4;
                    constantDouble = readFloat(operandStart);
                    return null;

                case OperandType.ShortInlineVar:
                    advance += 1;
                    constantInt = cil[operandStart];
                    return null;

                default: throw new Exception("Unexpected operand type [" + op.OperandType + "]");
            }
        }

        public static Dictionary<FieldInfo, int> FieldOffsetsInMemory(Type t)
        {
            try
            {
                var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

                var emit = Emit<Func<object, ulong[]>>.NewDynamicMethod("_GetOffsetsInMemory" + t.FullName);
                var retLoc = emit.DeclareLocal<ulong[]>("ret");

                emit.LoadConstant(fields.Length);	// ulong
                emit.NewArray(typeof(ulong));		// ulong[]
                emit.StoreLocal(retLoc);			// --empty--

                for (var i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];

                    emit.LoadLocal(retLoc);			// ulong[]
                    emit.LoadConstant(i);			// ulong[] ulong

                    emit.LoadArgument(0);			// ulong[] ulong param#0
                    emit.CastClass(t);	            // ulong[] ulong param#0

                    emit.LoadFieldAddress(field);	// ulong[] ulong field&
                    emit.Convert<ulong>();			// ulong[] ulong ulong

                    emit.StoreElement<ulong>();		// --empty--
                }

                emit.LoadLocal(retLoc);			// ulong[]
                emit.Return();					// --empty--

                var getAddrs = emit.CreateDelegate(Utils.DelegateOptimizationOptions);

                var cons = t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(p => p.GetParameters().Count()).FirstOrDefault();
                var consParameters = cons != null ? cons.GetParameters().Select(p => p.ParameterType.DefaultValue()).ToArray() : null;

                object obj;
                if (cons != null)
                {
                    obj = cons.Invoke(consParameters);
                }
                else
                {
                    obj = Activator.CreateInstance(t);
                }

                var addrs = getAddrs(obj);

                if (addrs.Length == 0)
                {
                    return new Dictionary<FieldInfo, int>();
                }

                var min = addrs.Min();

                var ret = new Dictionary<FieldInfo, int>();

                for (var i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];

                    var addr = addrs[i];
                    var offset = addr - min;

                    ret[field] = (int)offset;
                }

                return ret;
            }
            catch
            {
                // A lot can go wrong during this, and the common response is just to bail
                // This catch is much simpler than trying (and probably failing) to enumerate
                //    all the exceptional cases
                return new Dictionary<FieldInfo, int>();
            }
        }

        /// <summary>
        /// This returns a map of "name of member" => [Type of member, index of argument to constructor].
        /// We need this for anonymous types because we can't set properties (they're read-only).
        /// </summary>
        public static Dictionary<string, Tuple<Type, int>> GetAnonymousNameToConstructorMap(Type objType)
        {
            var cons = objType.GetConstructors().Single();
            var consInstrs = Utils.Decompile(cons);

            if (consInstrs != null)
            {
                return GetAnonymousNameToConstructorMap_Decompile(objType, cons, consInstrs);
            }

            // If we're in a case where the decompiler isn't available (right now, that means you NETCORE)
            //   then we fall back to guessing.
            // It's possible to fail here, which sucks, but is better than just not deserializing
            //   anonymous types

            return GetAnonymousNameToConstructorMap_Guess(objType, cons);
        }

        /// <summary>
        /// This returns a map of "name of member" => [Type of member, index of argument to constructor].
        /// We need this for anonymous types because we can't set properties (they're read-only).
        /// 
        /// How this works is kind of fun.
        /// 
        /// By spec, the arguments to the constructor are in declaration order for an anonymous type.
        /// So: new { A, B, C } => new SomeType(A a, B b, C c)
        /// 
        /// However there is no way to get declaration order via reflection, it's just not data that's
        /// preserved.  Furthermore, the names of backing fields for those read-only properties are not
        /// defined by the spec.
        /// 
        /// IF we have decompliation facilities, then we can do this "properly" (see GetAnonymousNameToConstructorMap_Decompile).
        /// But if we don't, we can still take a decent guess.
        /// 
        /// First, group properties by type.  If there are no duplicate types, then there's a one-to-one mapping between
        /// each parameter to the constructor and each property.
        /// 
        /// For groups with multiple properties, compare the names of the property and the constructor parameter.  Take
        /// exact matches, assuming there aren't any collisions (don't think that's possible, but just in case...).
        /// 
        /// This sort of exact matching isn't supported by the spec at all, but works for current implementations so
        /// as long as there's a "oh shit, invariant broken"-bail I'm OK with it.
        /// </summary>
        static Dictionary<string, Tuple<Type, int>> GetAnonymousNameToConstructorMap_Guess(Type objType, ConstructorInfo cons)
        {
            var props = objType.GetProperties();
            var conParams = cons.GetParameters();

            var remainingProps = new List<PropertyInfo>();
            remainingProps.AddRange(props);

            var remainingParams = new List<ParameterInfo>();
            remainingParams.AddRange(conParams);

            var propertyToParam = new Dictionary<PropertyInfo, ParameterInfo>();

            foreach (var group in remainingProps.GroupBy(p => p.PropertyType).ToList())
            {
                if (group.Count() == 1)
                {
                    var prop = group.ElementAt(0);
                    var param = conParams.Single(p => p.ParameterType == prop.PropertyType);

                    propertyToParam[prop] = param;

                    remainingProps.Remove(prop);
                }
            }

            var keepTrying = remainingProps.Count > 0;

            while (keepTrying)
            {
                keepTrying = false;

                foreach (var group in remainingProps.GroupBy(p => p.PropertyType).ToList())
                {
                    var propType = group.Key;

                    var candidateParams = remainingParams.Where(p => p.ParameterType == propType).ToList();

                    foreach (var prop in group)
                    {
                        var exactMatch = candidateParams.Where(p => p.Name == prop.Name).ToList();
                        if (exactMatch.Count == 1)
                        {
                            var matchingParam = exactMatch[0];
                            propertyToParam[prop] = matchingParam;

                            remainingProps.Remove(prop);

                            remainingParams.Remove(matchingParam);
                            candidateParams.Remove(matchingParam);

                            keepTrying = true;
                        }
                    }
                }
            }

            if (remainingProps.Count > 0)
            {
                throw new ConstructionException("Due to a limitation in .netcore - could not guess property <-> parameter mapping necessary to deserialize an anonymous type");
            }

            var ret = new Dictionary<string, Tuple<Type, int>>();

            foreach (var prop in props)
            {
                var pairedParam = propertyToParam[prop];
                var paramIndex = Array.IndexOf(conParams, pairedParam);
                ret[prop.Name] = Tuple.Create(prop.PropertyType, -1);
            }

            return ret;
        }

        /// <summary>
        /// This returns a map of "name of member" => [Type of member, index of argument to constructor].
        /// We need this for anonymous types because we can't set properties (they're read-only).
        /// 
        /// How this works is kind of fun.
        /// 
        /// By spec, the arguments to the constructor are in declaration order for an anonymous type.
        /// So: new { A, B, C } => new SomeType(A a, B b, C c)
        /// 
        /// However there is no way to get declaration order via reflection, it's just not data that's
        /// preserved.  Furthermore, the names of backing fields for those read-only properties are not
        /// defined by the spec.
        /// 
        /// So I got clever.
        /// 
        /// This method decompiles the constructor to find out which fields are set by which arguments (by index).
        /// It then decompiles all properties to find out which fields back which properties.
        /// Then it finally works backwards from each property, taking the property's name type and using it's backing
        /// field to lookup which index to pass it in as when constructing the anonymous object.
        /// </summary>
        static Dictionary<string, Tuple<Type, int>> GetAnonymousNameToConstructorMap_Decompile(Type objType, ConstructorInfo cons, List<Tuple<OpCode, int?, long?, double?, FieldInfo>> consInstrs)
        {
            var fieldToArgumentIndex = new Dictionary<FieldInfo, int>();

            var fields = objType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var storeInstr = consInstrs.FindIndex(d => d.Item1 == OpCodes.Stfld && d.Item5 == field);
                var preceedingLdArg = consInstrs.Take(storeInstr).Reverse().First(f => f.Item1.IsLoadArgumentOpCode());

                int paramIx;
                switch (preceedingLdArg.Item1.Value)
                {
                    // Ldarg_0 will be `this`, so we'll never see it

                    // Ldarg_1
                    case 0x03: paramIx = 1; break;
                    // Ldarg_2
                    case 0x04: paramIx = 2; break;
                    // Ldarg_3
                    case 0x05: paramIx = 3; break;

                    // Ldarg, Ldarg_S
                    default: paramIx = preceedingLdArg.Item2.Value; break;
                }

                fieldToArgumentIndex[field] = paramIx;
            }

            var propertyToBackingField = new Dictionary<PropertyInfo, FieldInfo>();
            foreach (var prop in objType.GetProperties())
            {
                var propInstrs = Utils.Decompile(prop.GetMethod);
                var backingField = propInstrs.Single(p => p.Item1 == OpCodes.Ldfld);

                propertyToBackingField[prop] = backingField.Item5;
            }

            var nameToTypeAndConsIndex =
                propertyToBackingField
                    .ToDictionary(
                        d => d.Key.Name,
                        d => Tuple.Create(d.Key.PropertyType, fieldToArgumentIndex[d.Value] - 1)    // -1 here because `this` adds 1 in the IL
                    );

            return nameToTypeAndConsIndex;
        }

        static MethodInfo DynamicProjectTo = typeof(Utils).GetMethod("_DynamicProjectTo", BindingFlags.NonPublic | BindingFlags.Static);
        static IEnumerable<T> _DynamicProjectTo<T>(IEnumerable<object> e)
        {
            return e.Cast<T>();
        }

        static System.Collections.Hashtable DynamicCastCache = new System.Collections.Hashtable();
        public static object DynamicProject(IEnumerable<object> e, Type castElementsTo)
        {
            var cached = (Func<IEnumerable<object>, object>)DynamicCastCache[castElementsTo];
            if (cached == null)
            {
                var mtd = DynamicProjectTo.MakeGenericMethod(castElementsTo);
                var emit = Sigil.Emit<Func<IEnumerable<object>, object>>.NewDynamicMethod();
                emit.LoadArgument(0);
                emit.Call(mtd);
                emit.Return();
                cached = emit.CreateDelegate(Utils.DelegateOptimizationOptions);

                lock (DynamicCastCache)
                {
                    DynamicCastCache[castElementsTo] = cached;
                }
            }

            return cached(e);
        }

        static MethodInfo ProjectToStringDictionary = typeof(Utils).GetMethod("_ProjectToStringDictionary", BindingFlags.Static | BindingFlags.NonPublic);
        static object _ProjectToStringDictionary<T>(IDictionary<object, object> real)
        {
            var ret = new Dictionary<string, T>();
            foreach (var kv in real)
            {
                ret[(string)kv.Key] = (T)kv.Value;
            }

            return ret;
        }

        static System.Collections.Hashtable StringDictionaryCache = new System.Collections.Hashtable();
        public static object ProjectStringDictionary(IDictionary<object, object> inner, Type valType)
        {
            var cached = (Func<IDictionary<object, object>, object>)StringDictionaryCache[valType];
            if (cached == null)
            {
                var mtd = ProjectToStringDictionary.MakeGenericMethod(valType);
                var emit = Sigil.Emit<Func<IDictionary<object, object>, object>>.NewDynamicMethod();
                emit.LoadArgument(0);
                emit.Call(mtd);
                emit.Return();

                cached = emit.CreateDelegate(Utils.DelegateOptimizationOptions);
                lock (StringDictionaryCache)
                {
                    StringDictionaryCache[valType] = cached;
                }
            }

            return cached(inner);
        }

        static MethodInfo ProjectToEnumDictionary = typeof(Utils).GetMethod("_ProjectToEnumDictionary", BindingFlags.Static | BindingFlags.NonPublic);
        static object _ProjectToEnumDictionary<TEnum, TValue>(IDictionary<object, object> real)
            where TEnum : struct
        {
            var ret = new Dictionary<TEnum, TValue>();
            foreach (var kv in real)
            {
                ret[(TEnum)kv.Key] = (TValue)kv.Value;
            }

            return ret;
        }

        static System.Collections.Hashtable EnumDictionaryCache = new System.Collections.Hashtable();
        public static object ProjectEnumDictionary(IDictionary<object, object> inner, Type keyType, Type valType)
        {
            var cached = (Func<IDictionary<object, object>, object>)EnumDictionaryCache[valType];
            if (cached == null)
            {
                var mtd = ProjectToEnumDictionary.MakeGenericMethod(keyType, valType);
                var emit = Sigil.Emit<Func<IDictionary<object, object>, object>>.NewDynamicMethod();
                emit.LoadArgument(0);
                emit.Call(mtd);
                emit.Return();

                cached = emit.CreateDelegate(Utils.DelegateOptimizationOptions);
                lock (EnumDictionaryCache)
                {
                    EnumDictionaryCache[valType] = cached;
                }
            }

            return cached(inner);
        }

        class Node<T>
        { 
            public T Value { get; set; }

            List<Node<T>> Childern = new List<Node<T>>();

            public Node(T value)
            {
                Value = value;
            }

            public void AddChild(Node<T> child)
            {
                Childern.Add(child);
            }

            bool CanReachImpl(T lookingFor, HashSet<T> alreadySeen)
            {
                if (lookingFor.Equals(Value)) return true;

                foreach (var child in Childern)
                {
                    // loop, but not the one we're looking for...
                    if (alreadySeen.Contains(child.Value)) continue;

                    var copy = new HashSet<T>(alreadySeen);
                    copy.Add(Value);

                    if (child.CanReachImpl(lookingFor, copy)) return true;
                }

                return false;
            }

            public bool CanReach(Node<T> otherNode)
            {
                return CanReachImpl(otherNode.Value, new HashSet<T>());
            }
        }

        public static HashSet<Type> FindRecursiveTypes(Type rootType)
        {
            var lookup = new Dictionary<Type, Node<Type>>();

            var pending = new Stack<Node<Type>>();
            var ret = new HashSet<Type>();

            Action<Type, Node<Type>> addIfReachable =
                (seenType, curNode) =>
                {
                    if (seenType.IsNullableType())
                    {
                        seenType = Nullable.GetUnderlyingType(seenType);
                    }
                    else 
                    {
                        if (seenType.IsListType())
                        {
                            var listI = seenType.GetListInterface();
                            seenType = listI.GetGenericArguments()[0];
                        }
                        else
                        {
                            if (seenType.IsDictionaryType())
                            {
                                var dictI = seenType.GetDictionaryInterface();
                                seenType = dictI.GetGenericArguments()[1];
                            }
                            else
                            {
                                if (seenType.IsEnumerableType())
                                {
                                    var enumI = seenType.GetEnumerableInterface();
                                    seenType = enumI.GetGenericArguments()[0];
                                }
                            }
                        }
                    }

                    Node<Type> olderNode;
                    if (lookup.TryGetValue(seenType, out olderNode))
                    {
                        if (olderNode.CanReach(curNode))
                        {
                            ret.Add(seenType);
                        }
                    }
                    else
                    {
                        olderNode = new Node<Type>(seenType);

                        if (curNode != null)
                        {
                            curNode.AddChild(olderNode);
                        }
                        
                        lookup[seenType] = olderNode;
                        pending.Push(olderNode);
                    }
                };

            addIfReachable(rootType, null);

            while (pending.Count > 0)
            {
                var curNode = pending.Pop();
                var curType = curNode.Value;

                // these can't have members, bail
                if (curType.IsPrimitiveType() || curType.IsEnum()) continue;

                if (curType.IsNullableType())
                {
                    var underlyingType = Nullable.GetUnderlyingType(curType);
                    addIfReachable(underlyingType, curNode);

                    continue;
                }

                if (curType.IsListType())
                {
                    var listI = curType.GetListInterface();
                    var valType = listI.GetGenericArguments()[0];
                    addIfReachable(valType, curNode);
                    continue;
                }

                if (curType.IsDictionaryType())
                {
                    var dictI = curType.GetDictionaryInterface();
                    var valType = dictI.GetGenericArguments()[1];
                    addIfReachable(valType, curNode);
                    continue;
                }

                if (curType.IsEnumerableType())
                {
                    var enumI = curType.GetEnumerableInterface();
                    var valType = enumI.GetGenericArguments()[0];
                    addIfReachable(valType, curNode);
                    continue;
                }

                foreach (var field in curType.GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (field.ShouldUseMember())
                    {
                        addIfReachable(field.FieldType, curNode);
                    }
                }

                foreach (var prop in curType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetMethod != null))
                {
                    if (prop.ShouldUseMember())
                    {
                        addIfReachable(prop.PropertyType, curNode);
                    }
                }
            }

            return ret;
        }

        public static List<Type> FindReusedTypes(Type rootType)
        {
            var pending = new Stack<Type>();
            pending.Push(rootType);

            var counts = new Dictionary<Type, int>();

            Action<Type> pushIfNew =
                type =>
                {
                    if (!counts.ContainsKey(type) || counts[type] < 2)
                    {
                        pending.Push(type);
                    }
                };

            while (pending.Count > 0)
            {
                var curType = pending.Pop();

                // these can't have members, bail
                if (curType.IsPrimitiveType() || curType.IsEnum()) continue;

                if (!counts.ContainsKey(curType)) counts[curType] = 0;
                counts[curType]++;

                if (curType.IsNullableType())
                {
                    var underlyingType = Nullable.GetUnderlyingType(curType);

                    pushIfNew(underlyingType);

                    continue;
                }

                if (curType.IsListType())
                {
                    var listI = curType.GetListInterface();
                    var valType = listI.GetGenericArguments()[0];
                    pushIfNew(valType);
                    continue;
                }

                if (curType.IsDictionaryType())
                {
                    var dictI = curType.GetDictionaryInterface();
                    var valType = dictI.GetGenericArguments()[1];
                    pushIfNew(valType);
                    continue;
                }

                if (curType.IsEnumerableType())
                {
                    var enumI = curType.GetEnumerableInterface();
                    var valType = enumI.GetGenericArguments()[0];
                    pushIfNew(valType);
                    continue;
                }

                foreach (var field in curType.GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (field.ShouldUseMember())
                    {
                        pushIfNew(field.FieldType);
                    }
                }

                foreach (var prop in curType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetMethod != null))
                {
                    if (prop.ShouldUseMember())
                    {
                        pushIfNew(prop.PropertyType);
                    }
                }
            }

            var reused = counts.Where(kv => kv.Value > 1).Select(kv => kv.Key).ToList();

            return 
                reused
                    .Where(
                        r => !(r.IsPrimitiveType() ||   // all the types we handle extra specially
                               r.IsEnum() || 
                               r.IsNullableType() ||
                               r.IsListType() ||
                               r.IsDictionaryType() ||
                               r.IsReadOnlyListType() ||
                               r.IsReadOnlyDictionaryType() ||
                               r.IsEnumerableType() ||
                               r.IsCollectionType())
                    )
                    .ToList();
        }

        public static string SafeConvertFromUtf32(int utf32)
        {
            if (utf32 > 0x10ffff || (utf32 >= 0x00d800 && utf32 <= 0x00dfff))
            {
                return ((char)utf32).ToString();
            }

            return char.ConvertFromUtf32(utf32);
        }

        public static bool LoadConstantOfType<T>(Emit<T> emit, object val, Type type)
        {
            if (type == typeof(byte))
            {
                try
                {
                    var v = (byte)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (byte?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            if (type == typeof(sbyte))
            {
                try
                {
                    var v = (sbyte)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (sbyte?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            if (type == typeof(short))
            {
                try
                {
                    var v = (short)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (short?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            if (type == typeof(ushort))
            {
                try
                {
                    var v = (ushort)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (ushort?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            if (type == typeof(int))
            {
                try
                {
                    var v = (int)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (int?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            if (type == typeof(uint))
            {
                try
                {
                    var v = (uint)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (uint?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            if (type == typeof(long))
            {
                try
                {
                    var v = (long)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (long?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            if (type == typeof(ulong))
            {
                try
                {
                    var v = (ulong)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (ulong?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            return false;
        }

        public static bool LoadConstantOfType(Emit emit, object val, Type type)
        {
            if (type == typeof(byte))
            {
                try
                {
                    var v = (byte)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (byte?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            if (type == typeof(sbyte))
            {
                try
                {
                    var v = (sbyte)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (sbyte?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            if (type == typeof(short))
            {
                try
                {
                    var v = (short)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (short?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            if (type == typeof(ushort))
            {
                try
                {
                    var v = (ushort)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (ushort?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            if (type == typeof(int))
            {
                try
                {
                    var v = (int)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (int?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            if (type == typeof(uint))
            {
                try
                {
                    var v = (uint)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (uint?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            if (type == typeof(long))
            {
                try
                {
                    var v = (long)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (long?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            if (type == typeof(ulong))
            {
                try
                {
                    var v = (ulong)val;
                    emit.LoadConstant(v);
                    return true;
                }
                catch { }

                try
                {
                    var v = (ulong?)val;
                    emit.LoadConstant(v.Value);
                    return true;
                }
                catch { }

                return false;
            }

            return false;
        }

        private static long[] PowersOf10 = new[] 
        {
            1L,
            10L, 
            100L, 
            1000L, 
            10000L, 
            100000L,
            1000000L,
            10000000L,
            100000000L
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Pow10(int power) 
        {
            if (power < PowersOf10.Length)
                return PowersOf10[power];
            return (long)Math.Pow(10, power);
        }

        public static bool CheckUnionLegality(DateTimeFormat dateFormat, string memberName, IEnumerable<MemberInfo> possible, out Dictionary<char, MemberInfo> discriminantChars, out MemberInfo destinationType, out Dictionary<UnionCharsets, MemberInfo> charsetToMember, out bool allowsNull, out string errorMessage)
        {
            charsetToMember = new Dictionary<UnionCharsets, MemberInfo>();
            allowsNull = false;
            destinationType = null;

            var charsToMembers = new Dictionary<char, List<MemberInfo>>();

            foreach (var member in possible)
            {
                var attr = member.GetCustomAttribute<JilDirectiveAttribute>();
                if (attr == null || !attr.IsUnion)
                {
                    discriminantChars = null;
                    errorMessage = "Member [" + member.Name + "] isn't marked as part of a union, but other members share the same Name [" + memberName + "]";
                    return false;
                }

                if (attr.IsUnionType)
                {
                    if (member.ReturnType() != typeof(Type))
                    {
                        discriminantChars = null;
                        errorMessage = "Member [" + member.Name + "] has IsUnionType set, but is not of type System.Type";
                        return false;
                    }

                    var asProp = member as PropertyInfo;
                    if (asProp != null && asProp.SetMethod == null)
                    {
                        discriminantChars = null;
                        errorMessage = "Member [" + member.Name + "] has IsUnionType set, but has no set method";
                        return false;
                    }

                    if (destinationType != null)
                    {
                        discriminantChars = null;
                        errorMessage = "Member [" + member.Name + "] has IsUnionType set, but IsUnionType is also set for [" + destinationType.Name + "]";
                        return false;
                    }

                    destinationType = member;
                    continue;
                }

                var memberType = member.ReturnType();
                if (!memberType.IsValueType() || memberType.IsNullableType())
                {
                    allowsNull = true;
                }

                UnionCharsets perMember;
                var dis = GetDescriminantCharacters(memberType, dateFormat, out perMember);
                foreach (var e in Enum.GetValues(typeof(UnionCharsets)).Cast<UnionCharsets>().Where(x => perMember.HasFlag(x)))
                {
                    charsetToMember[e] = member;
                }

                charsetToMember[perMember] = member;

                foreach (var c in dis)
                {
                    List<MemberInfo> mems;
                    if (!charsToMembers.TryGetValue(c, out mems))
                    {
                        charsToMembers[c] = mems = new List<MemberInfo>();
                    }

                    mems.Add(member);
                }
            }

            var ambiguousCharsToMembers = charsToMembers.Where(kv => kv.Value.Count > 1).ToDictionary(kv => kv.Key, kv => new MemberGroup(kv.Value));
            if(ambiguousCharsToMembers.Any())
            {
                var conflicts = 
                    ambiguousCharsToMembers
                        .GroupBy(kv => kv.Value)
                        .Select(
                            g => 
                                Tuple.Create(
                                    g.Select(kv => kv.Key).Distinct().OrderBy(_ => _).ToList(), 
                                    g.Key.Members
                                )
                        )
                        .ToList();

                errorMessage =
                    string.Join("; ",
                        conflicts
                            .Select(
                                t =>
                                    "The members  [" +
                                        string.Join(", ", t.Item2.Select(m => m.Name)) +
                                    "] cannot be distiguished in a union because they can each start with these characters [" +
                                        string.Join(", ", t.Item1) +
                                    "]"
                            )
                            .OrderBy(_ => _)
                    );

                discriminantChars = null;
                return false;
            }

            discriminantChars = charsToMembers.ToDictionary(kv => kv.Key, kv => kv.Value.Single());
            errorMessage = null;
            return true;
        }

        class MemberGroup : IEquatable<MemberGroup>
        {
            public List<MemberInfo> Members {get; private set;}

            public MemberGroup(List<MemberInfo> members)
            {
                Members = members;
            }

            public void Add(MemberInfo member)
            {
                Members.Add(member);
            }

            public override bool Equals(object other)
            {
                var asMemberGroup = other as MemberGroup;
                if(asMemberGroup == null) return false;

                return Equals(asMemberGroup);
            }

            public override int GetHashCode()
            {
                return Members.Select(m => m.GetHashCode()).OrderBy(_ => _).Aggregate((cur, val) => cur ^ val);
            }

            public bool Equals(MemberGroup other)
            {
                if (other.Members.Count != this.Members.Count) return false;

                return this.Members.All(m => other.Members.Contains(m));
            }
        }

        static IEnumerable<char> GetDescriminantCharacters(Type memType, DateTimeFormat dateFormat, out UnionCharsets charsets)
        {
            charsets = UnionCharsets.None;

            IEnumerable<char> ret = Enumerable.Empty<char>();

            if (memType.IsNullableType())
            {
                memType = Nullable.GetUnderlyingType(memType);
            }

            if (memType.IsNumberType())
            {
                if (memType.IsSigned() || memType.IsFloatingPointNumberType())
                {
                    charsets |= UnionCharsets.Signed;
                    ret = ret.Concat(UnionCharsetArrays.UnionSignedSet);
                }

                charsets |= UnionCharsets.Number;
                ret = ret.Concat(UnionCharsetArrays.UnionNumberSet);

                return ret;
            }

            if (memType.IsStringyType() || memType == typeof(Guid))
            {
                charsets |= UnionCharsets.Stringy;
                ret = ret.Concat(UnionCharsetArrays.UnionStringySet);

                return ret;
            }

            if (memType == typeof(bool))
            {
                charsets |= UnionCharsets.Bool;
                ret = ret.Concat(UnionCharsetArrays.UnionBoolSet);

                return ret;
            }

            if (memType == typeof(DateTime) || memType == typeof(DateTimeOffset) || memType == typeof(TimeSpan))
            {
                switch (dateFormat)
                {
                    case DateTimeFormat.RFC1123:
                    case DateTimeFormat.ISO8601:
                    case DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch:
                        charsets |= UnionCharsets.Stringy;
                        ret = ret.Concat(UnionCharsetArrays.UnionStringySet);
                        break;

                    case DateTimeFormat.MillisecondsSinceUnixEpoch:
                    case DateTimeFormat.SecondsSinceUnixEpoch:
                        charsets |= UnionCharsets.Number;
                        ret = ret.Concat(UnionCharsetArrays.UnionNumberSet);
                        break;

                    default: throw new Exception("Unexpected DateTimeFormat: " + dateFormat);
                }

                return ret;
            }

            if (memType.IsEnum())
            {
                var attr = memType.GetCustomAttribute<JilDirectiveAttribute>();
                if (attr != null && attr.TreatEnumerationAs != null)
                {
                    charsets |= UnionCharsets.Number;
                    ret = ret.Concat(UnionCharsetArrays.UnionNumberSet);
                    return ret;
                }

                charsets |= UnionCharsets.Stringy;
                ret = ret.Concat(UnionCharsetArrays.UnionStringySet);
                return ret;
            }

            if (memType.IsDictionaryType() || memType.IsGenericReadOnlyDictionary())
            {
                charsets |= UnionCharsets.Object;
                ret = ret.Concat(UnionCharsetArrays.UnionObjectSet);
                return ret;
            }

            if (memType.IsListType() || memType.IsGenericEnumerable() || memType.IsGenericReadOnlyList())
            {
                charsets |= UnionCharsets.Listy;
                ret = ret.Concat(UnionCharsetArrays.UnionListySet);
                return ret;
            }

            charsets |= UnionCharsets.Object;
            ret = ret.Concat(UnionCharsetArrays.UnionObjectSet);
            return ret;
        }

        /// <summary>
        /// Functional-style helper method for creating and initializing a new array.
        /// </summary>
        /// <typeparam name="T">The element type of the array.</typeparam>
        /// <param name="count">The length of the array to create.</param>
        /// <param name="generator">
        /// The function used to initialize each element of the array.
        /// The integer applied to the function represents the index of the element being initialized.
        /// </param>
        /// <returns>An array with element type <typeparamref>T</typeparamref> and length <paramref>count</paramref>.</returns>
        internal static T[] CreateArray<T>(int count, Func<int, T> generator)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            else if (generator == null)
            {
                throw new ArgumentNullException("generator");
            }
            //Contract.EndContractBlock();

            var arr = new T[count];
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = generator(i);
            }
            return arr;
        }
    }
}
