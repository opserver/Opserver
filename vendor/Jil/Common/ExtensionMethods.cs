using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Common
{
    static class ExtensionMethods
    {
        public static ConstructorInfo GetPublicOrPrivateConstructor(this Type onType, params Type[] parameterTypes)
        {
            return onType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, parameterTypes);
        }

        public static ConstructorInfo GetConstructor(this Type type, BindingFlags flags, Type[] parameterTypes)
        {
            var info = type.GetTypeInfo();
            var cons = info.GetConstructors(flags);
            foreach (var con in cons)
            {
                var ps = con.GetParameters();
                if (ps.Length != parameterTypes.Length) continue;

                var allMatch = true;

                for (var i = 0; i < ps.Length; i++)
                {
                    var p = ps[i].ParameterType;
                    var pt = parameterTypes[i];
                    if (pt != p)
                    {
                        allMatch = false;
                    }
                }

                if (allMatch) return con;
            }

            return null;
        }

        public static MethodInfo GetMethod(this Type type, string name, BindingFlags flags, Type[] parameterTypes)
        {
            var info = type.GetTypeInfo();
            var mtds = info.GetMethods(flags).Where(m => m.Name == name);

            foreach (var mtd in mtds)
            {
                var ps = mtd.GetParameters();
                if (ps.Length != parameterTypes.Length) continue;

                var allMatch = true;
                for (var i = 0; i < ps.Length; i++)
                {
                    var p = ps[i].ParameterType;
                    var pt = parameterTypes[i];
                    if (p != pt)
                    {
                        allMatch = false;
                    }
                }

                if (allMatch)
                {
                    return mtd;
                }
            }

            return null;
        }

        public static Module Module(this Type type)
        {
            var info = type.GetTypeInfo();
            return info.Module;
        }

        public static Type GetNestedType(this Type type, string name)
        {
            var info = type.GetTypeInfo();
            var ret = info.GetNestedType(name);

            return ret;
        }

        public static bool IsValueType(this Type type)
        {
            var info = type.GetTypeInfo();

            return info.IsValueType;
        }

        public static string ResolveString(this Module module, int handle)
        {
#if NETSTANDARD1_6
            throw new NotImplementedException("Cannot resolve string references in IL on dotNetCore");
#else
            return module.ResolveString(handle);
#endif
        }

        public static Type BaseType(this Type type)
        {
            var info = type.GetTypeInfo();

            return info.BaseType;
        }

        public static bool IsGenericType(this Type type)
        {
            var info = type.GetTypeInfo();

            return info.IsGenericType;
        }

        public static bool ShouldConvertEnum(this MemberInfo member, Type enumType)
        {
            Type ignored;
            return member.ShouldConvertEnum(enumType, out ignored);
        }

        public static bool ShouldConvertEnum(this MemberInfo member, Type enumType, out Type toType)
        {
            var attr = member.GetCustomAttribute<JilDirectiveAttribute>();
            if (attr == null || attr.TreatEnumerationAs == null)
            {
                toType = null;
                return false;
            }

            var primitiveType = Enum.GetUnderlyingType(enumType);
            var convert = attr.TreatEnumerationAs;

            bool underlyingSigned, targetSigned;
            int underlyingSize, targetSize;

            if (!GetEnumUnderlyingPrimitiveInfo(primitiveType, out underlyingSigned, out underlyingSize) || !GetEnumUnderlyingPrimitiveInfo(primitiveType, out targetSigned, out targetSize))
            {
                throw new ConstructionException("Cannot map enum [" + enumType + "] with underlying type [" + primitiveType + "] to [" + convert + "], convert is not an acceptable integer primitive type");
            }

            // possible cases
            // both are signed and target is equal or large to underlying => OK
            // underlying is unsigned, and target is unsigned and equal or larger => OK
            // underlying is unsigned and target is signed and larger => OK
            // underlying is signed, target is unsigned => NOPE
            // underlying is unsigned, target is signed and equal or smaller => NOPE

            if (underlyingSigned && !targetSigned)
            {
                throw new ConstructionException("Cannot map enum [" + enumType + "] with underlying type [" + primitiveType + "] to [" + convert + "], there is a signed/unsigned mismatch");
            }

            if (!underlyingSigned && (targetSigned && targetSize <= underlyingSize))
            {
                throw new ConstructionException("Cannot map enum [" + enumType + "] with underlying type [" + primitiveType + "] to [" + convert + "], target type is not large enough");
            }

            toType = convert;
            return true;
        }

        static bool GetEnumUnderlyingPrimitiveInfo(Type primitiveType, out bool signed, out int numBytes)
        {
            if (primitiveType == typeof(byte))
            {
                signed = false;
                numBytes = 1;
                return true;
            }

            if (primitiveType == typeof(sbyte))
            {
                signed = true;
                numBytes = 1;
                return true;
            }

            if (primitiveType == typeof(short))
            {
                signed = true;
                numBytes = 2;
                return true;
            }

            if (primitiveType == typeof(ushort))
            {
                signed = false;
                numBytes = 2;
                return true;
            }

            if (primitiveType == typeof(int))
            {
                signed = true;
                numBytes = 4;
                return true;
            }

            if (primitiveType == typeof(uint))
            {
                signed = false;
                numBytes = 4;
                return true;
            }

            if (primitiveType == typeof(long))
            {
                signed = true;
                numBytes = 8;
                return true;
            }

            if (primitiveType == typeof(ulong))
            {
                signed = false;
                numBytes = 8;
                return true;
            }

            signed = false;
            numBytes = -1;

            return false;
        }

        public static TextReader MakeSupportPeek(this TextReader inner)
        {
            var asStringReader = inner as StringReader;
            if (asStringReader != null) return asStringReader;

            var asStreamReader = inner as StreamReader;
            if(asStreamReader != null && asStreamReader.BaseStream.CanSeek) return asStreamReader;

            return new PeekSupportingTextReader(inner);
        }

        public static string GetEnumValueName(this Type enumType, object enumVal)
        {
            var field = enumType.GetFields().Single(f => f.Name == Enum.GetName(enumType, enumVal));

            var enumMember = field.GetCustomAttribute<System.Runtime.Serialization.EnumMemberAttribute>();
            if (enumMember == null || enumMember.Value == null)
            {
                return Enum.GetName(enumType, enumVal);
            }

            return enumMember.Value;
        }

        public static bool IsFlagsEnum(this Type enumType)
        {
            return enumType.GetCustomAttribute<FlagsAttribute>() != null;
        }

        public static T GetCustomAttribute<T>(this Type type)
            where T : Attribute
        {
            var info = type.GetTypeInfo();
            var data = info.CustomAttributes.Where(a => a.AttributeType == typeof(T)).SingleOrDefault();

            if (data == null) return null;

            var cons = data.Constructor;
            var args = data.ConstructorArguments.Select(a => a.Value).ToArray();
            var ret = cons.Invoke(args);

            return (T)ret;
        }

        public static bool IsInterface(this Type type)
        {
            var info = type.GetTypeInfo();

            return info.IsInterface;
        }

        public static bool IsSealed(this Type type)
        {
            var info = type.GetTypeInfo();

            return info.IsSealed;
        }

        public static bool IsPublic(this Type type)
        {
            var info = type.GetTypeInfo();

            return info.IsPublic;
        }

        public static bool IsGenericContainer(this Type forType, Type containerType)
        {
            return forType.IsInterface() && forType.IsGenericType() && forType.GetGenericTypeDefinition() == containerType;
        }

        public static bool IsGenericDictionary(this Type forType)
        {
            return IsGenericContainer(forType, typeof(IDictionary<,>));
        }

        public static bool IsGenericEnumerable(this Type forType)
        {
            return IsGenericContainer(forType, typeof(IEnumerable<>));
        }

        public static bool IsGenericReadOnlyList(this Type forType)
        {
            return IsGenericContainer(forType, typeof(IReadOnlyList<>));
        }

        public static bool IsGenericReadOnlyDictionary(this Type forType)
        {
            return IsGenericContainer(forType, typeof(IReadOnlyDictionary<,>));
        }

        public static string GetSerializationName(this MemberInfo member, SerializationNameFormat serializationNameFormat)
        {
            var jilDirectiveAttr = member.GetCustomAttribute<JilDirectiveAttribute>();
            if (jilDirectiveAttr != null && jilDirectiveAttr.Name != null) return jilDirectiveAttr.Name;

            var dataMemberAttr = member.GetCustomAttribute<System.Runtime.Serialization.DataMemberAttribute>();
            if (dataMemberAttr != null && dataMemberAttr.Name != null) return dataMemberAttr.Name;

            switch (serializationNameFormat)
            {
                case SerializationNameFormat.CamelCase:
                    return member.Name.ToCamelCase();
                default:
                    return member.Name;
            }
        }

        public static string ToCamelCase(this string s)
        {
            if (string.IsNullOrEmpty(s) || !char.IsUpper(s[0])) return s;

            var cArr = s.ToCharArray();
            for (var i = 0; i < cArr.Length; i++)
            {
                if (i > 0 && i + 1 < cArr.Length && !char.IsUpper(cArr[i + 1])) break;
                cArr[i] = char.ToLowerInvariant(cArr[i]);
            }

            return new string(cArr);
        }

        public static bool IsLoadArgumentOpCode(this OpCode op)
        {
            return
                op == OpCodes.Ldarg ||
                op == OpCodes.Ldarg_0 ||
                op == OpCodes.Ldarg_1 ||
                op == OpCodes.Ldarg_2 ||
                op == OpCodes.Ldarg_3 ||
                op == OpCodes.Ldarg_S;
        }

        /// <summary>
        /// HACK: This is a best effort attempt to divine if a type is anonymous based on the language spec.
        /// 
        /// Reference section 7.6.10.6 of the C# language spec as of 2012/11/19
        /// 
        /// It checks:
        ///     - is a class
        ///     - descends directly from object
        ///     - has [CompilerGenerated]
        ///     - has a single constructor
        ///     - that constructor takes exactly the same parameters as its public properties
        ///     - all public properties are not writable
        ///     - has a private field for every public property
        ///     - overrides Equals(object)
        ///     - overrides GetHashCode()
        /// </summary>
        public static bool IsAnonymouseClass(this Type type) // don't fix the typo, it's fitting.
        {
            if (type.IsValueType()) return false;
            if (type.BaseType() != typeof(object)) return false;

            var compilerGenerated = type.GetTypeInfo().CustomAttributes.Any(a => a.AttributeType == typeof(CompilerGeneratedAttribute));
            if (!compilerGenerated) return false;

            var allCons = type.GetConstructors();
            if (allCons.Length != 1) return false;

            var cons = allCons[0];
            if (!cons.IsPublic) return false;

            var props = type.GetProperties();
            if (props.Any(p => p.CanWrite)) return false;

            var propTypes = props.Select(t => t.PropertyType).ToList();

            foreach (var param in cons.GetParameters())
            {
                if (!propTypes.Contains(param.ParameterType)) return false;

                propTypes.Remove(param.ParameterType);
            }

            if (propTypes.Count != 0) return false;

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            if (fields.Any(f => !f.IsPrivate)) return false;

            propTypes = props.Select(t => t.PropertyType).ToList();
            foreach (var field in fields)
            {
                if (!propTypes.Contains(field.FieldType)) return false;

                propTypes.Remove(field.FieldType);
            }

            if (propTypes.Count != 0) return false;

            var equals = type.GetMethod("Equals", new Type[] { typeof(object) });
            var hashCode = type.GetMethod("GetHashCode", new Type[0]);

            if (!equals.IsOverride() || !hashCode.IsOverride()) return false;

            return true;
        }

        public static bool IsOverride(this MethodInfo method)
        {
            return method.GetBaseDefinition() != method;
        }

        public static bool IsUserDefinedType(this Type type)
        {
            return !type.IsListType() && !type.IsDictionaryType() && !type.IsEnum() && !type.IsPrimitiveType();
        }

        public static bool IsEnum(this Type type)
        {
            var info = type.GetTypeInfo();

            return info.IsEnum;
        }

        public static bool IsConstant(this MemberInfo member)
        {
            var asField = member as FieldInfo;
            if (asField != null) return asField.IsConstant();

            var asProp = member as PropertyInfo;
            if (asProp != null) return asProp.IsConstant();

            throw new Exception("Expected member to be a FieldInfo or PropertyInfo, found: " + member);
        }

        public static bool IsConstant(this FieldInfo field)
        {
            try
            {
                return field.IsLiteral && field.ReturnType().IsPropagatableType() && field.GetRawConstantValue() != null;
            }
            catch (Exception)
            {
                // Something went sideways, bail
                return false;
            }
        }

        static readonly IEnumerable<OpCode> ReadOnlyOpCodes =
            new[]
            {   
                OpCodes.Ldc_I4,     // Constant integer numbers
                OpCodes.Ldc_I4_0,
                OpCodes.Ldc_I4_1,
                OpCodes.Ldc_I4_2,
                OpCodes.Ldc_I4_3,
                OpCodes.Ldc_I4_4,
                OpCodes.Ldc_I4_5,
                OpCodes.Ldc_I4_6,
                OpCodes.Ldc_I4_7,
                OpCodes.Ldc_I4_8,
                OpCodes.Ldc_I4_M1,
                OpCodes.Ldc_I4_S,
                OpCodes.Ldc_I8,

                OpCodes.Ldc_R4,     // Constant floating point numbers
                OpCodes.Ldc_R8,

                OpCodes.Ldstr,      // Constant strings

                OpCodes.Conv_I,         // Conversion operators
                OpCodes.Conv_I1,
                OpCodes.Conv_I2,
                OpCodes.Conv_I4,
                OpCodes.Conv_I8,
                OpCodes.Conv_Ovf_I,
                OpCodes.Conv_Ovf_I_Un,
                OpCodes.Conv_Ovf_I1,
                OpCodes.Conv_Ovf_I1_Un,
                OpCodes.Conv_Ovf_I2,
                OpCodes.Conv_Ovf_I2_Un,
                OpCodes.Conv_Ovf_I4,
                OpCodes.Conv_Ovf_I4_Un,
                OpCodes.Conv_Ovf_I8,
                OpCodes.Conv_Ovf_I8_Un,
                OpCodes.Conv_R4,
                OpCodes.Conv_R8,

                OpCodes.Ldnull      // always null
            };

        static readonly IEnumerable<OpCode> ConstantLoadOpCodes = 
            new[]
            {
                OpCodes.Ldc_I4_0,
                OpCodes.Ldc_I4_1,
                OpCodes.Ldc_I4_2,
                OpCodes.Ldc_I4_3,
                OpCodes.Ldc_I4_4,
                OpCodes.Ldc_I4_5,
                OpCodes.Ldc_I4_6,
                OpCodes.Ldc_I4_7,
                OpCodes.Ldc_I4_8,
                OpCodes.Ldc_I4_M1,

                OpCodes.Ldnull
            };

        public static bool IsConstant(this PropertyInfo prop)
        {
            var getMtd = prop.GetMethod;

            // virtual methods can't be proven constant, who knows what overrides are out there
            if (getMtd == null || getMtd.IsVirtual) return false;

            var instrs = Utils.Decompile(getMtd);
            if (instrs == null) return false;

            // anything control flow-y, call-y, load-y, etc. means the property isn't constant
            var hasNonConstantInstructions = instrs.Any(a => !ReadOnlyOpCodes.Contains(a.Item1) && a.Item1 != OpCodes.Ret);
            if (hasNonConstantInstructions) return false;

            // if *two* constants are in play (somehow) we can't tell which one to propagate so break it
            var numberOfConstants = instrs.Count(a => ConstantLoadOpCodes.Contains(a.Item1) || a.Item2.HasValue || a.Item3.HasValue || a.Item4.HasValue);
            if (numberOfConstants != 1) return false;

            return true;
        }

        public static string GetConstantJSONStringEquivalent(this MemberInfo member, bool jsonp)
        {
            var asField = member as FieldInfo;
            if (asField != null) return asField.GetConstantJSONStringEquivalent(jsonp);

            var asProp = member as PropertyInfo;
            if (asProp != null) return asProp.GetConstantJSONStringEquivalent(jsonp);

            throw new Exception("Expected member to be a FieldInfo or PropetyInfo, found: " + member);
        }

        public static string GetConstantJSONStringEquivalent(this PropertyInfo prop, bool jsonp)
        {
#if NETSTANDARD1_6
            throw new NotImplementedException("It's not possible to determine a constant value for a property in dotnet core.");
#else

            var instrs = Utils.Decompile(prop.GetMethod);

            var constInstr = instrs.Single(o => ConstantLoadOpCodes.Contains(o.Item1) || o.Item2.HasValue || o.Item3.HasValue || o.Item4.HasValue);

            object equivObj = null;

            if (ConstantLoadOpCodes.Contains(constInstr.Item1))
            {
                if (constInstr.Item1 == OpCodes.Ldnull)
                {
                    equivObj = null;
                }
                else
                {
                    if (constInstr.Item1 == OpCodes.Ldc_I4_0) equivObj = 0;
                    if (constInstr.Item1 == OpCodes.Ldc_I4_1) equivObj = 1;
                    if (constInstr.Item1 == OpCodes.Ldc_I4_2) equivObj = 2;
                    if (constInstr.Item1 == OpCodes.Ldc_I4_3) equivObj = 3;
                    if (constInstr.Item1 == OpCodes.Ldc_I4_4) equivObj = 4;
                    if (constInstr.Item1 == OpCodes.Ldc_I4_5) equivObj = 5;
                    if (constInstr.Item1 == OpCodes.Ldc_I4_6) equivObj = 6;
                    if (constInstr.Item1 == OpCodes.Ldc_I4_7) equivObj = 7;
                    if (constInstr.Item1 == OpCodes.Ldc_I4_8) equivObj = 8;
                    if (constInstr.Item1 == OpCodes.Ldc_I4_M1) equivObj = -1;

                    if (equivObj == null) throw new Exception("Couldn't determine constant being loaded");
                }
            }
            else
            {
                if (constInstr.Item1 == OpCodes.Ldstr)
                {
                    var handle = constInstr.Item2.Value;
                    equivObj = prop.Module.ResolveString(handle);
                }

                if (constInstr.Item1 == OpCodes.Ldc_R8)
                {
                    equivObj = constInstr.Item4.Value;
                }

                if (constInstr.Item1 == OpCodes.Ldc_R4)
                {
                    equivObj = constInstr.Item4.Value;
                }

                if (constInstr.Item1 == OpCodes.Ldc_I4)
                {
                    equivObj = constInstr.Item2.Value;
                }

                if (constInstr.Item1 == OpCodes.Ldc_I4_S)
                {
                    equivObj = (sbyte)constInstr.Item2.Value;
                }

                if (constInstr.Item1 == OpCodes.Ldc_I8)
                {
                    equivObj = constInstr.Item3.Value;
                }
            }

            if (equivObj != null && equivObj.GetType() != prop.ReturnType())
            {
                equivObj = ConvertType(equivObj, equivObj.GetType(), prop.ReturnType());
            }

            return GetConstantJSONStringEquivalent(prop.ReturnType(), equivObj, jsonp);
#endif
        }

        private static object ConvertType(object val, Type fromType, Type toType)
        {
            if (toType.IsEnum())
            {
                toType = Enum.GetUnderlyingType(toType);
            }

            if (toType == typeof(sbyte))
            {
                if (fromType.IsSigned())
                {
                    var l = (long)Convert.ChangeType(val, typeof(long));

                    return (sbyte)l;
                }

                var ul = (ulong)Convert.ChangeType(val, typeof(ulong));

                return (sbyte)ul;
            }

            if (toType == typeof(ushort))
            {
                if (fromType.IsSigned())
                {
                    var l = (long)Convert.ChangeType(val, typeof(long));

                    return (ushort)l;
                }

                var ul = (ulong)Convert.ChangeType(val, typeof(ulong));

                return (ushort)ul;
            }

            if (toType == typeof(uint))
            {
                if (fromType.IsSigned())
                {
                    var l = (long)Convert.ChangeType(val, typeof(long));

                    return (uint)l;
                }

                var ul = (ulong)Convert.ChangeType(val, typeof(ulong));

                return (uint)ul;
            }

            if (toType == typeof(ulong))
            {
                if (fromType.IsSigned())
                {
                    long l = (long)Convert.ChangeType(val, typeof(long));

                    return (ulong)l;
                }

                return Convert.ChangeType(val, typeof(ulong));
            }

            return Convert.ChangeType(val, toType);
        }

        public static bool IsUnsigned(this Type t)
        {
            return !t.IsSigned();
        }

        public static bool IsSigned(this Type t)
        {
            return
                t == typeof(sbyte) ||
                t == typeof(short) ||
                t == typeof(int) ||
                t == typeof(long);
        }

        public static string GetConstantJSONStringEquivalent(this FieldInfo field, bool jsonp)
        {
            var obj = field.GetRawConstantValue();

            return GetConstantJSONStringEquivalent(field.ReturnType(), obj, jsonp);
        }

        private static string GetConstantJSONStringEquivalent(Type type, object obj, bool jsonp)
        {
            if (obj == null) return "null";

            var asStr = obj as string;
            if (asStr != null)
            {
                return "\"" + asStr.JsonEscape(jsonp) + "\"";
            }

            var asChar = obj is char ? (char?)(char)obj : (char?)null;
            if (asChar != null)
            {
                return "\"" + asChar.Value.JsonEscape(jsonp) + "\"";
            }

            var asBool = obj is bool ? (bool?)(bool)obj : (bool?)null;
            if (asBool != null)
            {
                return asBool.Value ? "true" : "false";
            }

            if (type.IsEnum())
            {
                return "\"" + type.GetEnumValueName(obj).JsonEscape(jsonp) + "\"";
            }
            
            var formattable = obj as IFormattable;
            if (formattable != null)
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            else
                return obj.ToString();
        }

        public static bool IsPropagatableType(this Type t)
        {
            return
                t == typeof(string) ||
                t == typeof(char) ||
                t == typeof(float) ||
                t == typeof(double) ||
                t == typeof(byte) ||
                t == typeof(sbyte) ||
                t == typeof(short) ||
                t == typeof(ushort) ||
                t == typeof(int) ||
                t == typeof(uint) ||
                t == typeof(long) ||
                t == typeof(ulong) ||
                t == typeof(bool) ||
                t.IsEnum();
        }

        public static bool ShouldUseMember(this MemberInfo memberInfo)
        {
            var jilDirectiveAttributes = memberInfo.GetCustomAttributes<JilDirectiveAttribute>();
            if (jilDirectiveAttributes.Count() > 0) return !jilDirectiveAttributes.Any(d => d.Ignore);

            var ignoreDataMemberAttributes = memberInfo.GetCustomAttributes<IgnoreDataMemberAttribute>();
            return ignoreDataMemberAttributes.Count() == 0;
        }

        public static MethodInfo ShouldSerializeMethod(this PropertyInfo prop, Type serializingType)
        {
            var mtdName = "ShouldSerialize" + prop.Name;

            var ret =
                serializingType
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.Name == mtdName && m.ReturnType == typeof(bool) && m.GetParameters().Length == 0)
                    .SingleOrDefault();

            return ret;
        }

        public static Type ReturnType(this MemberInfo m)
        {
            var asField = m as FieldInfo;
            var asProp = m as PropertyInfo;

            return
                asField != null ? asField.FieldType : asProp.PropertyType;
        }

        public static bool IsNullableType(this Type t)
        {
            var underlying = GetUnderlyingType(t);

            return underlying != null;
        }

        public static Type GetUnderlyingType(this Type t)
        {
            return Nullable.GetUnderlyingType(t);
        }

        public static bool IsContainerType(this Type t, Type containerType)
        {
            try
            {
                return
                    (t.IsGenericType() && t.GetGenericTypeDefinition() == containerType) ||
                    t.GetInterfaces().Any(i => i.IsGenericType() && i.GetGenericTypeDefinition() == containerType);
            }
            catch (Exception) { return false; }
        }

        public static bool IsListType(this Type t)
        {
            return t.IsContainerType(typeof(IList<>));
        }

        public static bool IsCollectionType(this Type t)
        {
            return t.IsContainerType(typeof(ICollection<>));
        }

        public static bool IsEnumerableType(this Type t)
        {
            return t.IsContainerType(typeof(IEnumerable<>));
        }

        public static bool IsReadOnlyListType(this Type t)
        {
            return t.IsContainerType(typeof(IReadOnlyList<>));
        }

        public static bool IsReadOnlyDictionaryType(this Type t)
        {
            return t.IsContainerType(typeof(IReadOnlyDictionary<,>));
        }

        public static Type GetListInterface(this Type t)
        {
            return t.GetContainerInterface(typeof(IList<>));
        }

        public static Type GetSetInterface(this Type t)
        {
            return t.GetContainerInterface(typeof(ISet<>));
        }

        public static Type GetReadOnlyListInterface(this Type t)
        {
            return t.GetContainerInterface(typeof(IReadOnlyList<>));
        }

        public static Type GetEnumerableInterface(this Type t)
        {
            return t.GetContainerInterface(typeof(IEnumerable<>));
        }

        public static Type GetCollectionInterface(this Type t)
        {
            return t.GetContainerInterface(typeof(ICollection<>));
        }

        public static Type GetReadOnlyCollectionInterface(this Type t)
        {
            return t.GetContainerInterface(typeof(IReadOnlyCollection<>));
        }

        public static bool IsDictionaryType(this Type t)
        {
            return t.IsContainerType(typeof(IDictionary<,>));
        }

        public static bool IsSetType(this Type t)
        {
            return t.IsContainerType(typeof(ISet<>));
        }

        public static Type GetContainerInterface(this Type t, Type containerType)
        {
            return
                (t.IsGenericType() && t.GetGenericTypeDefinition() == containerType)
                    ? t
                    : t.GetInterfaces().First(i => i.IsGenericType() && i.GetGenericTypeDefinition() == containerType);
        }

        public static Type GetDictionaryInterface(this Type t)
        {
            return t.GetContainerInterface(typeof(IDictionary<,>));
        }

        public static Type GetReadOnlyDictionaryInterface(this Type t)
        {
            return t.GetContainerInterface(typeof(IReadOnlyDictionary<,>));
        }

        public static bool IsExactlyDictionaryType(this Type t)
        {
            if (!t.IsGenericType()) return false;

            var generic = t.GetGenericTypeDefinition();

            return generic == typeof(Dictionary<,>);
        }

        public static bool IsSimpleInterface(this Type t)
        {
            // not an interface? bail
            if (!t.IsInterface()) return false;

            // not public? bail
            if (!t.IsPublic()) return false;

            var members = t.GetAllInterfaceMembers();

            var mtds = members.OfType<MethodInfo>().ToList();
            var props = members.OfType<PropertyInfo>().ToList();

            // something weird here, bail
            if (mtds.Count + props.Count != members.Count) return false;

            // any methods that aren't property accessors? bail
            if (mtds.Any(m => !props.Any(p => p.GetMethod == m || p.SetMethod == m))) return false;
            
            // define a property that takes parameters? bail
            if (props.Any(p => p.GetIndexParameters().Length != 0)) return false;

            return true;
        }

        public static List<MemberInfo> GetAllInterfaceMembers(this Type t)
        {
            if (!t.IsInterface()) throw new Exception("Expected interface, found: " + t);

            var pending = new Stack<Type>();
            pending.Push(t);

            var ret = new List<MemberInfo>();

            while (pending.Count > 0)
            {
                var current = pending.Pop();

                ret.AddRange(current.GetMembers());

                if (current.BaseType() != null)
                {
                    pending.Push(current.BaseType());
                }

                current.GetInterfaces().ForEach(i => pending.Push(i));
            }

            return ret;
        }

        public static void ForEach<T>(this IEnumerable<T> e, Action<T> func)
        {
            foreach (var x in e)
            {
                func(x);
            }
        }

        public static bool IsPrimitiveType(this Type t)
        {
            return
                t == typeof(string) ||
                t == typeof(char) ||
                t == typeof(float) ||
                t == typeof(double) ||
                t == typeof(decimal) ||
                t == typeof(byte) ||
                t == typeof(sbyte) ||
                t == typeof(short) ||
                t == typeof(ushort) ||
                t == typeof(int) ||
                t == typeof(uint) ||
                t == typeof(long) ||
                t == typeof(ulong) ||
                t == typeof(bool) ||
                t == typeof(DateTime) ||
                t == typeof(DateTimeOffset) ||
                t == typeof(Guid) ||
                t == typeof(TimeSpan);
        }

        public static bool IsPrimitiveWrapper(this Type type)
        {
            return type.GetCustomAttribute<JilPrimitiveWrapper>() != null;
        }

        public static MemberInfo GetPrimitiveWrapperPropertyOrField(this Type primitiveWrapperType)
        {
            var members = primitiveWrapperType.GetMembers(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var candidates =
                members.Where(
                    m =>
                    {
                        var f = m as FieldInfo;
                        if (f != null)
                            return f.GetCustomAttribute<CompilerGeneratedAttribute>() == null && f.FieldType.IsPrimitiveType();
                        var p = m as PropertyInfo;
                        if (p != null)
                            return p.PropertyType.IsPrimitiveType();
                        return false;
                    }
                );

            var candidateCount = candidates.Count();

            if(candidateCount != 1)
            {
                throw new ConstructionException("Primitive wrappers can only have 1 declared primitive member, found " + candidateCount + " for " + primitiveWrapperType.Name);
            }

            return candidates.Single();
        }

        public static bool IsStringyType(this MemberInfo member)
        {
            var asField = member as FieldInfo;
            if (asField != null)
            {
                return asField.FieldType.IsStringyType();
            }

            var asProperty = member as PropertyInfo;
            if (asProperty != null)
            {
                return asProperty.PropertyType.IsStringyType();
            }

            throw new InvalidOperationException();
        }

        public static bool IsStringyType(this Type t)
        {
            return
                t == typeof(string) ||
                t == typeof(char);
        }

        // From: http://www.ietf.org/rfc/rfc4627.txt?number=4627
        public static string JsonEscape(this string str, bool jsonp)
        {
            var ret = "";
            foreach (var c in str)
            {
                ret += c.JsonEscape(jsonp);
            }

            return ret;
        }

        public static void JsonEscapeFast(this string str, bool jsonp, System.IO.TextWriter output)
        {
            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];

                if (jsonp)
                {
                    if (c == '\u2028')
                    {
                        output.Write(@"\u2028");
                        continue;
                    }

                    if (c == '\u2029')
                    {
                        output.Write(@"\u2029");
                        continue;
                    }
                }

                if (c == '\\')
                {
                    output.Write(@"\\");
                    continue;
                }

                if (c == '"')
                {
                    output.Write(@"\""");
                    continue;
                }

                switch (c)
                {
                    case '\u0000': output.Write(@"\u0000"); continue;
                    case '\u0001': output.Write(@"\u0001"); continue;
                    case '\u0002': output.Write(@"\u0002"); continue;
                    case '\u0003': output.Write(@"\u0003"); continue;
                    case '\u0004': output.Write(@"\u0004"); continue;
                    case '\u0005': output.Write(@"\u0005"); continue;
                    case '\u0006': output.Write(@"\u0006"); continue;
                    case '\u0007': output.Write(@"\u0007"); continue;
                    case '\u0008': output.Write(@"\u0008"); continue;
                    case '\u0009': output.Write(@"\t"); continue;
                    case '\u000A': output.Write(@"\n"); continue;
                    case '\u000B': output.Write(@"\v"); continue;
                    case '\u000C': output.Write(@"\f"); continue;
                    case '\u000D': output.Write(@"\r"); continue;
                    case '\u000E': output.Write(@"\u000E"); continue;
                    case '\u000F': output.Write(@"\u000F"); continue;
                    case '\u0010': output.Write(@"\u0010"); continue;
                    case '\u0011': output.Write(@"\u0011"); continue;
                    case '\u0012': output.Write(@"\u0012"); continue;
                    case '\u0013': output.Write(@"\u0013"); continue;
                    case '\u0014': output.Write(@"\u0014"); continue;
                    case '\u0015': output.Write(@"\u0015"); continue;
                    case '\u0016': output.Write(@"\u0016"); continue;
                    case '\u0017': output.Write(@"\u0017"); continue;
                    case '\u0018': output.Write(@"\u0018"); continue;
                    case '\u0019': output.Write(@"\u0019"); continue;
                    case '\u001A': output.Write(@"\u001A"); continue;
                    case '\u001B': output.Write(@"\u001B"); continue;
                    case '\u001C': output.Write(@"\u001C"); continue;
                    case '\u001D': output.Write(@"\u001D"); continue;
                    case '\u001E': output.Write(@"\u001E"); continue;
                    case '\u001F': output.Write(@"\u001F"); continue;
                }

                output.Write(c);
            }
        }

        public static string JsonEscape(this char c, bool jsonp)
        {
            switch (c)
            {
                case '\\': return @"\\";
                case '"': return @"\""";
                case '\u0000': return @"\u0000";
                case '\u0001': return @"\u0001";
                case '\u0002': return @"\u0002";
                case '\u0003': return @"\u0003";
                case '\u0004': return @"\u0004";
                case '\u0005': return @"\u0005";
                case '\u0006': return @"\u0006";
                case '\u0007': return @"\u0007";
                case '\u0008': return @"\u0008";
                case '\u0009': return @"\t";
                case '\u000A': return @"\n";
                case '\u000B': return @"\v";
                case '\u000C': return @"\f";
                case '\u000D': return @"\r";
                case '\u000E': return @"\u000E";
                case '\u000F': return @"\u000F";
                case '\u0010': return @"\u0010";
                case '\u0011': return @"\u0011";
                case '\u0012': return @"\u0012";
                case '\u0013': return @"\u0013";
                case '\u0014': return @"\u0014";
                case '\u0015': return @"\u0015";
                case '\u0016': return @"\u0016";
                case '\u0017': return @"\u0017";
                case '\u0018': return @"\u0018";
                case '\u0019': return @"\u0019";
                case '\u001A': return @"\u001A";
                case '\u001B': return @"\u001B";
                case '\u001C': return @"\u001C";
                case '\u001D': return @"\u001D";
                case '\u001E': return @"\u001E";
                case '\u001F': return @"\u001F";

                case '\u2028':
                    if (jsonp)
                    {
                        return @"\u2028";
                    }
                    else
                    {
                        goto default;
                    }

                case '\u2029':
                    if (jsonp)
                    {
                        return @"\u2029";
                    }
                    else
                    {
                        goto default;
                    }

                default: return c.ToString();
            }
        }

        public static object DefaultValue(this Type t)
        {
            if (!t.IsValueType()) return null;

            return Activator.CreateInstance(t);
        }

        public static bool IsNumberType(this Type t)
        {
            return t.IsIntegerNumberType() || t.IsFloatingPointNumberType();
        }

        public static bool IsIntegerNumberType(this Type t)
        {
            return
                t == typeof(byte) ||
                t == typeof(sbyte) ||
                t == typeof(short) ||
                t == typeof(ushort) ||
                t == typeof(int) ||
                t == typeof(uint) ||
                t == typeof(long) ||
                t == typeof(ulong);
        }

        public static bool IsFloatingPointNumberType(this Type t)
        {
            return
                t == typeof(float) ||
                t == typeof(double) ||
                t == typeof(decimal);
        }

        public static List<Type> InvolvedTypes(this Type t)
        {
            var ret = new List<Type>();

            var pending = new Stack<Type>();
            pending.Push(t);

            while (pending.Count > 0)
            {
                var cur = pending.Pop();
                if (!ret.Contains(cur))
                {
                    ret.Add(cur);
                }
                else
                {
                    continue;
                }

                if (cur.IsNullableType())
                {
                    var inner = Nullable.GetUnderlyingType(cur);
                    pending.Push(inner);
                    continue;
                }

                if (cur.IsListType())
                {
                    var elem = cur.GetListInterface().GetGenericArguments()[0];
                    pending.Push(elem);
                    continue;
                }

                if (cur.IsDictionaryType())
                {
                    var key = cur.GetDictionaryInterface().GetGenericArguments()[0];
                    var val = cur.GetDictionaryInterface().GetGenericArguments()[1];

                    pending.Push(key);
                    pending.Push(val);

                    continue;
                }

                if (cur.IsEnumerableType())
                {
                    var val = cur.GetEnumerableInterface().GetGenericArguments()[0];
                    pending.Push(val);

                    continue;
                }

                cur.GetFields().ForEach(f => pending.Push(f.FieldType));
                cur.GetProperties().Where(p => p.GetMethod != null && p.GetMethod.GetParameters().Length == 0).ForEach(p => pending.Push(p.PropertyType));
            }

            return ret;
        }

        public static HashSet<Type> FindRecursiveOrReusedTypes(this Type forType)
        {
            var recursive = Utils.FindRecursiveTypes(forType);
            var reusedTypes = Utils.FindReusedTypes(forType);

            return new HashSet<Type>(recursive.Concat(reusedTypes));
        }

        public static HashSet<Type> FindChildTypes(this Type forType)
        {
            var ret = new HashSet<Type>();
            var seen = new HashSet<Type>();
            var pending = new Stack<Type>();

            pending.Push(forType);

            while (pending.Any())
            {
                var cur = pending.Pop();
                seen.Add(cur);

                if (seen.Contains(cur))
                {
                    // already visited
                    continue;
                }

                if (cur.IsPrimitiveType())
                {
                    // no members
                    continue;
                }

                if (cur.IsNullableType())
                {
                    // nullables shouldn't be serialized directly

                    var underlyingType = Nullable.GetUnderlyingType(cur);
                    if (!seen.Contains(underlyingType))
                    {
                        pending.Push(underlyingType);
                    }

                    continue;
                }

                if (cur.IsListType())
                {
                    ret.Add(cur);

                    var listI = cur.GetListInterface();
                    var valType = listI.GetGenericArguments()[0];
                    if (!seen.Contains(valType))
                    {
                        pending.Push(valType);
                    }

                    continue;
                }

                if (cur.IsDictionaryType())
                {
                    ret.Add(cur);

                    var dictI = cur.GetDictionaryInterface();
                    var valType = dictI.GetGenericArguments()[1];
                    if (!seen.Contains(valType))
                    {
                        pending.Push(valType);
                    }

                    continue;
                }

                if (cur.IsEnumerableType())
                {
                    ret.Add(cur);


                    var enumI = cur.GetEnumerableInterface();
                    var valType = enumI.GetGenericArguments()[0];
                    if (!seen.Contains(valType))
                    {
                        pending.Push(valType);
                    }

                    continue;
                }

                foreach (var field in cur.GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (!field.ShouldUseMember()) continue;

                    if (!seen.Contains(field.FieldType))
                    {
                        pending.Push(field.FieldType);
                    }
                }

                foreach (var prop in cur.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetMethod != null))
                {
                    if (!prop.ShouldUseMember()) continue;

                    if (!seen.Contains(prop.PropertyType))
                    {
                        pending.Push(prop.PropertyType);
                    }
                }
            }

            ret.Remove(forType);

            return ret;
        }
    }
}
