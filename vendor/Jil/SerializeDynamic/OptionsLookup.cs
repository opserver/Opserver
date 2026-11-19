using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jil.SerializeDynamic
{
    class OptionsLookup
    {
        #region initialization
        static readonly Dictionary<Options, FieldInfo> FieldLookup = new Dictionary<Options, FieldInfo>();
        static readonly Dictionary<Options, Type> TypeCacheLookup = new Dictionary<Options, Type>();
        static OptionsLookup()
        {
            var precalced = typeof(Options).GetFields(BindingFlags.Static | BindingFlags.Public);

            foreach (var field in precalced)
            {
                var opts = (Options)field.GetValue(null);
                FieldLookup[opts] = field;
                TypeCacheLookup[opts] = CalculateTypeCacheFor(opts);
            }
        }

        static Type CalculateTypeCacheFor(Options opts)
        {
            switch(opts.UseDateTimeFormat)
            {
                case DateTimeFormat.ISO8601: return GetISO8601TypeCache(opts);
                case DateTimeFormat.MillisecondsSinceUnixEpoch: return GetMillisecondsTypeCache(opts);
                case DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch: return GetMicrosoftTypeCache(opts);
                case DateTimeFormat.SecondsSinceUnixEpoch: return GetSecondsTypeCache(opts);
                case DateTimeFormat.RFC1123: return GetRFC1123TypeCache(opts);
                default: throw new Exception("Unexpected DateTimeFormat: " + opts.UseDateTimeFormat);
            }
        }

        static Type SwitchOnNameFormat<Verbatim, CamelCase>(Options opts) 
            where Verbatim  : Jil.Serialize.ISerializeOptions
            where CamelCase : Jil.Serialize.ISerializeOptions
        {
            switch(opts.SerializationNameFormat)
            {
                case SerializationNameFormat.Verbatim: return typeof(Verbatim);
                case SerializationNameFormat.CamelCase: return typeof(CamelCase);
                default: throw new Exception("Unexpected SerializationNameFormat: " + opts.SerializationNameFormat);
            }
        }

        static Type GetMicrosoftTypeCache(Options opts)
        {
            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls && opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.MicrosoftStylePrettyPrintExcludeNullsJSONPInherited, Serialize.MicrosoftStylePrettyPrintExcludeNullsJSONPInheritedCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls && opts.IsJSONP)
            {
                return SwitchOnNameFormat<Serialize.MicrosoftStylePrettyPrintExcludeNullsJSONP, Serialize.MicrosoftStylePrettyPrintExcludeNullsJSONPCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.MicrosoftStylePrettyPrintExcludeNullsInherited, Serialize.MicrosoftStylePrettyPrintExcludeNullsInheritedCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.MicrosoftStylePrettyPrintJSONPInherited, Serialize.MicrosoftStylePrettyPrintJSONPInheritedCamelCase>(opts);
            }

            if (opts.ShouldExcludeNulls && opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.MicrosoftStyleExcludeNullsJSONPInherited, Serialize.MicrosoftStyleExcludeNullsJSONPInheritedCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls)
            {
                return SwitchOnNameFormat<Serialize.MicrosoftStylePrettyPrintExcludeNulls, Serialize.MicrosoftStylePrettyPrintExcludeNullsCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.IsJSONP)
            {
                return SwitchOnNameFormat<Serialize.MicrosoftStylePrettyPrintJSONP, Serialize.MicrosoftStylePrettyPrintJSONPCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.MicrosoftStylePrettyPrintInherited, Serialize.MicrosoftStylePrettyPrintInheritedCamelCase>(opts);
            }

            if (opts.ShouldExcludeNulls && opts.IsJSONP)
            {
                return SwitchOnNameFormat<Serialize.MicrosoftStyleExcludeNullsJSONP, Serialize.MicrosoftStyleExcludeNullsJSONPCamelCase>(opts);
            }

            if (opts.ShouldExcludeNulls && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.MicrosoftStyleExcludeNullsInherited, Serialize.MicrosoftStyleExcludeNullsInheritedCamelCase>(opts);
            }

            if (opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.MicrosoftStyleJSONPInherited, Serialize.MicrosoftStyleJSONPInheritedCamelCase>(opts);
            }

            return SwitchOnNameFormat<Serialize.MicrosoftStyle, Serialize.MicrosoftStyleCamelCase>(opts);
        }

        static Type GetSecondsTypeCache(Options opts)
        {
            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls && opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.SecondsPrettyPrintExcludeNullsJSONPInherited, Serialize.SecondsPrettyPrintExcludeNullsJSONPInheritedCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls && opts.IsJSONP)
            {
                return SwitchOnNameFormat<Serialize.SecondsPrettyPrintExcludeNullsJSONP, Serialize.SecondsPrettyPrintExcludeNullsJSONPCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.SecondsPrettyPrintExcludeNullsInherited, Serialize.SecondsPrettyPrintExcludeNullsInheritedCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.SecondsPrettyPrintJSONPInherited, Serialize.SecondsPrettyPrintJSONPInheritedCamelCase>(opts);
            }

            if (opts.ShouldExcludeNulls && opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.SecondsExcludeNullsJSONPInherited, Serialize.SecondsExcludeNullsJSONPInheritedCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls)
            {
                return SwitchOnNameFormat<Serialize.SecondsPrettyPrintExcludeNulls, Serialize.SecondsPrettyPrintExcludeNullsCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.IsJSONP)
            {
                return SwitchOnNameFormat<Serialize.SecondsPrettyPrintJSONP, Serialize.SecondsPrettyPrintJSONPCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.SecondsPrettyPrintInherited, Serialize.SecondsPrettyPrintInheritedCamelCase>(opts);
            }

            if (opts.ShouldExcludeNulls && opts.IsJSONP)
            {
                return SwitchOnNameFormat<Serialize.SecondsExcludeNullsJSONP, Serialize.SecondsExcludeNullsJSONPCamelCase>(opts);
            }

            if (opts.ShouldExcludeNulls && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.SecondsExcludeNullsInherited, Serialize.SecondsExcludeNullsInheritedCamelCase>(opts);
            }

            if (opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.SecondsJSONPInherited, Serialize.SecondsJSONPInheritedCamelCase>(opts);
            }

            return SwitchOnNameFormat<Serialize.Seconds, Serialize.SecondsCamelCase>(opts);
        }

        static Type GetRFC1123TypeCache(Options opts)
        {
            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls && opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.RFC1123PrettyPrintExcludeNullsJSONPInherited, Serialize.RFC1123PrettyPrintExcludeNullsJSONPInheritedCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls && opts.IsJSONP)
            {
                return SwitchOnNameFormat<Serialize.RFC1123PrettyPrintExcludeNullsJSONP, Serialize.RFC1123PrettyPrintExcludeNullsJSONPCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.RFC1123PrettyPrintExcludeNullsInherited, Serialize.RFC1123PrettyPrintExcludeNullsInheritedCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.RFC1123PrettyPrintJSONPInherited, Serialize.RFC1123PrettyPrintJSONPInheritedCamelCase>(opts);
            }

            if (opts.ShouldExcludeNulls && opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.RFC1123ExcludeNullsJSONPInherited, Serialize.RFC1123ExcludeNullsJSONPInheritedCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls)
            {
                return SwitchOnNameFormat<Serialize.RFC1123PrettyPrintExcludeNulls, Serialize.RFC1123PrettyPrintExcludeNullsCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.IsJSONP)
            {
                return SwitchOnNameFormat<Serialize.RFC1123PrettyPrintJSONP, Serialize.RFC1123PrettyPrintJSONPCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.RFC1123PrettyPrintInherited, Serialize.RFC1123PrettyPrintInheritedCamelCase>(opts);
            }

            if (opts.ShouldExcludeNulls && opts.IsJSONP)
            {
                return SwitchOnNameFormat<Serialize.RFC1123ExcludeNullsJSONP, Serialize.RFC1123ExcludeNullsJSONPCamelCase>(opts);
            }

            if (opts.ShouldExcludeNulls && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.RFC1123ExcludeNullsInherited, Serialize.RFC1123ExcludeNullsInheritedCamelCase>(opts);
            }

            if (opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.RFC1123JSONPInherited, Serialize.RFC1123JSONPInheritedCamelCase>(opts);
            }

            return SwitchOnNameFormat<Serialize.RFC1123, Serialize.RFC1123CamelCase>(opts);
        }

        static Type GetMillisecondsTypeCache(Options opts)
        {
            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls && opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.MillisecondsPrettyPrintExcludeNullsJSONPInherited, Serialize.MillisecondsPrettyPrintExcludeNullsJSONPInheritedCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls && opts.IsJSONP)
            {
                return SwitchOnNameFormat<Serialize.MillisecondsPrettyPrintExcludeNullsJSONP, Serialize.MillisecondsPrettyPrintExcludeNullsJSONPCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.MillisecondsPrettyPrintExcludeNullsInherited, Serialize.MillisecondsPrettyPrintExcludeNullsInheritedCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.MillisecondsPrettyPrintJSONPInherited, Serialize.MillisecondsPrettyPrintJSONPInheritedCamelCase>(opts);
            }

            if (opts.ShouldExcludeNulls && opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.MillisecondsExcludeNullsJSONPInherited, Serialize.MillisecondsExcludeNullsJSONPInheritedCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls)
            {
                return SwitchOnNameFormat<Serialize.MillisecondsPrettyPrintExcludeNulls, Serialize.MillisecondsPrettyPrintExcludeNullsCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.IsJSONP)
            {
                return SwitchOnNameFormat<Serialize.MillisecondsPrettyPrintJSONP, Serialize.MillisecondsPrettyPrintJSONPCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.MillisecondsPrettyPrintInherited, Serialize.MillisecondsPrettyPrintInheritedCamelCase>(opts);
            }

            if (opts.ShouldExcludeNulls && opts.IsJSONP)
            {
                return SwitchOnNameFormat<Serialize.MillisecondsExcludeNullsJSONP, Serialize.MillisecondsExcludeNullsJSONPCamelCase>(opts);
            }

            if (opts.ShouldExcludeNulls && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.MillisecondsExcludeNullsInherited, Serialize.MillisecondsExcludeNullsInheritedCamelCase>(opts);
            }

            if (opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.MillisecondsJSONPInherited, Serialize.MillisecondsJSONPInheritedCamelCase>(opts);
            }

            return SwitchOnNameFormat<Serialize.Milliseconds, Serialize.MillisecondsCamelCase>(opts);
        }

        static Type GetISO8601TypeCache(Options opts)
        {
            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls && opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.ISO8601PrettyPrintExcludeNullsJSONPInherited, Serialize.ISO8601PrettyPrintExcludeNullsJSONPInheritedCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls && opts.IsJSONP)
            {
                return SwitchOnNameFormat<Serialize.ISO8601PrettyPrintExcludeNullsJSONP, Serialize.ISO8601PrettyPrintExcludeNullsJSONPCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.ISO8601PrettyPrintExcludeNullsInherited, Serialize.ISO8601PrettyPrintExcludeNullsInheritedCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.ISO8601PrettyPrintJSONPInherited, Serialize.ISO8601PrettyPrintJSONPInheritedCamelCase>(opts);
            }

            if (opts.ShouldExcludeNulls && opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.ISO8601ExcludeNullsJSONPInherited, Serialize.ISO8601ExcludeNullsJSONPInheritedCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldExcludeNulls)
            {
                return SwitchOnNameFormat<Serialize.ISO8601PrettyPrintExcludeNulls, Serialize.ISO8601PrettyPrintExcludeNullsCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.IsJSONP)
            {
                return SwitchOnNameFormat<Serialize.ISO8601PrettyPrintJSONP, Serialize.ISO8601PrettyPrintJSONPCamelCase>(opts);
            }

            if (opts.ShouldPrettyPrint && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.ISO8601PrettyPrintInherited, Serialize.ISO8601PrettyPrintInheritedCamelCase>(opts);
            }

            if (opts.ShouldExcludeNulls && opts.IsJSONP)
            {
                return SwitchOnNameFormat<Serialize.ISO8601ExcludeNullsJSONP, Serialize.ISO8601ExcludeNullsJSONPCamelCase>(opts);
            }

            if (opts.ShouldExcludeNulls && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.ISO8601ExcludeNullsInherited, Serialize.ISO8601ExcludeNullsInheritedCamelCase>(opts);
            }

            if (opts.IsJSONP && opts.ShouldIncludeInherited)
            {
                return SwitchOnNameFormat<Serialize.ISO8601JSONPInherited, Serialize.ISO8601JSONPInheritedCamelCase>(opts);
            }

            return SwitchOnNameFormat<Serialize.ISO8601, Serialize.ISO8601CamelCase>(opts);
        }
        #endregion

        public static FieldInfo GetOptionsFieldFor(Options opts)
        {
            return FieldLookup[opts];
        }

        public static Type GetTypeCacheFor(Options opts)
        {
            return TypeCacheLookup[opts];
        }
    }
}
