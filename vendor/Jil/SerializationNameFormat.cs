using System;

namespace Jil
{
    /// <summary>
    /// Specifies how to serialize names when not specified explicitly via an attribute.
    /// 
    /// If an attrbiute is defined specifying the name (such as [JilDirective(Name="prop-name")] or [DataMember(Name="prop-name")]), 
    /// that attribute will be used. If there is no attribute, the specified SerializationNameFormat will be used.
    /// </summary>
    public enum SerializationNameFormat : byte
    {
        /// <summary>
        /// Names for classes and properties will be seraialized exactly as they appear or as attributes define them.
        /// 
        /// This is the default.
        /// </summary>
        Verbatim = 0,
        /// <summary>
        /// Names for classes and properties (unless specified exactly via attribute) will be serialized to CamelCase.
        /// 
        /// Example:
        ///     "MyClass" => "myClass"
        /// 
        /// If an attrbiute is defined specifying the name (such as [JilDirective(Name="prop-name")] or [DataMember(Name="prop-name")]), 
        /// that attribute will be used. If there is no attribute, the specified SerializationNameFormat will be used.
        /// </summary>
        CamelCase
    }

    internal static class SerializationNameFormatExtensions
    {
        internal static Type GetGenericTypeArgument(this SerializationNameFormat serializationNameFormat)
        {
            switch (serializationNameFormat)
            {
                case SerializationNameFormat.Verbatim:
                    return typeof (SerializationNameFormatVerbatim);
                case SerializationNameFormat.CamelCase:
                    return typeof(SerializationNameFormatCamelCase);
                default:
                    throw new ArgumentOutOfRangeException("serializationNameFormat", serializationNameFormat, null);
            }
        }
    }

    /// <summary>
    /// Class for generic caching differentiation
    /// </summary>
    internal class SerializationNameFormatVerbatim { }
    /// <summary>
    /// Class for generic caching differentiation
    /// </summary>
    internal class SerializationNameFormatCamelCase { }
}
