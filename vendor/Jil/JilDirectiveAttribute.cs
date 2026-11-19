using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jil
{
    /// <summary>
    /// Alternative to using [DataMember] and [IgnoreDataMember], for 
    /// when their use isn't possible.
    /// 
    /// When applied to a property or field, allows configuration
    /// of the name (de)serialized, whether to (de)serialize at all,
    /// and the primitive type to treat an enum type as.
    /// 
    /// Takes precedence over [DataMember] and [IgnoreDataMember].
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class JilDirectiveAttribute : Attribute
    {
        /// <summary>
        /// If true, the decorated member will not be serialized or deserialized.
        /// </summary>
        public bool Ignore { get; set; }
        /// <summary>
        /// If non-null, the decorated member's name in serialization will match
        /// the value of Name.
        /// 
        /// When deserializing, Name is used to map to a member.  This mapping is
        /// case sensitive.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// If true and the member annotated is an enum, will cause Jil to convert
        /// the enum to the appropriate primitive type before serializing; and expect
        /// that primitive type when deserializing, converting back to the enum when
        /// constructing the final object.
        /// </summary>
        public Type TreatEnumerationAs { get; set; }
        /// <summary>
        /// If true then multiple members (each of a different type) can have the same Name, forming a discriminant union.
        /// This can be used to handle JSON which puts different types of values under the same key.
        /// 
        /// When deserializing if Jil encounters a value under the name of a union, it will set whichever member has a matching type.
        /// When serializing, Jil will check each member under the name of the union and write whichever one has a non-default value.
        /// </summary>
        public bool IsUnion { get; set; }
        /// <summary>
        /// If true, and the annotated member is a Type, and the annotated member is part of a union then:
        ///   - the annotated member will be set to whichever Type was deserialized for the indicated union
        ///   - if no value was provided, the annotated member will be null
        ///   
        /// There can be only one member of a union which has IsUnionType set.
        /// </summary>
        public bool IsUnionType { get; set; }

        /// <summary>
        /// Create a new JilDirectiveAttribute
        /// </summary>
        public JilDirectiveAttribute() { }

        /// <summary>
        /// Create a new JilDirectiveAttribute, with a name override.
        /// </summary>
        public JilDirectiveAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Create a new JilDirectiveAttribute, optionally ignoring the decorated member.
        /// </summary>
        public JilDirectiveAttribute(bool ignore)
        {
            Ignore = ignore;
        }

        /// <summary>
        /// Create a new JilDirectiveAttribute, treating the decorate member of an enum type
        /// as the given primitive type (ie. byte, sbyte, short, ushort, int, uint, long, or ulong).
        /// </summary>
        public JilDirectiveAttribute(Type serializeEnumAs)
        {
            TreatEnumerationAs = serializeEnumAs;
        }
    }

    /// <summary>
    /// Marks the given class as a simple wrapper around primitive type.
    /// The marked class should either:
    /// - have a non default ctor, with this only value.
    /// - have only one property or field, which will be used to set and read primitive value from.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class JilPrimitiveWrapper : Attribute { }
}