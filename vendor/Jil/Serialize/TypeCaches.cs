using Sigil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Serialize
{
    interface ISerializeOptions
    {
        bool PrettyPrint { get; }
        bool ExcludeNulls { get; }
        DateTimeFormat DateFormat { get; }
        bool JSONP { get; }
        bool IncludeInherited { get; }
        UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get; }
        SerializationNameFormat SerializationNameFormat { get; }
    }

    static class TypeCache<TOptions, T>
        where TOptions : ISerializeOptions, new()
    {
        static readonly object ThunkInitLock = new object();
        static volatile bool ThunkBeingBuilt = false;
        public static volatile Action<TextWriter, T, int> Thunk;
        public static Exception ThunkExceptionDuringBuild;

        static readonly object StringThunkInitLock = new object();
        static volatile bool StringThunkBeingBuilt = false;
        public static volatile StringThunkDelegate<T> StringThunk;
        public static Exception StringThunkExceptionDuringBuild;

        public static Action<TextWriter, T, int> Get()
        {
            Load();
            return Thunk;
        }

        public static void Load()
        {
            if (Thunk != null) return;

            lock (ThunkInitLock)
            {
                if (Thunk != null || ThunkBeingBuilt) return;
                ThunkBeingBuilt = true;

                var opts = new TOptions();

                Thunk = InlineSerializerHelper.Build<T>(typeof(TOptions), pretty: opts.PrettyPrint, excludeNulls: opts.ExcludeNulls, dateFormat: opts.DateFormat, jsonp: opts.JSONP, includeInherited: opts.IncludeInherited, dateTimeBehavior: opts.DateTimeKindBehavior, serializationNameFormat: opts.SerializationNameFormat, exceptionDuringBuild: out ThunkExceptionDuringBuild);
            }
        }

        public static StringThunkDelegate<T> GetToString()
        {
            LoadToString();
            return StringThunk;
        }

        public static void LoadToString()
        {
            if (StringThunk != null) return;

            lock (StringThunkInitLock)
            {
                if (StringThunk != null || StringThunkBeingBuilt) return;
                StringThunkBeingBuilt = true;

                var opts = new TOptions();

                StringThunk = InlineSerializerHelper.BuildToString<T>(typeof(TOptions), pretty: opts.PrettyPrint, excludeNulls: opts.ExcludeNulls, dateFormat: opts.DateFormat, jsonp: opts.JSONP, includeInherited: opts.IncludeInherited, dateTimeBehavior: opts.DateTimeKindBehavior, serializationNameFormat: opts.SerializationNameFormat, exceptionDuringBuild: out StringThunkExceptionDuringBuild);
            }
        }
    }

    // Start OptionsGeneration.linq generated content
    class MicrosoftStyle : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrint : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleExcludeNulls : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintExcludeNulls : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrintJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrintInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrintUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrintCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStyleExcludeNullsJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleExcludeNullsInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleExcludeNullsUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleExcludeNullsCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStyleJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStyleInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStyleUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintExcludeNullsJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrintExcludeNullsInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrintExcludeNullsUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrintExcludeNullsCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrintJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrintJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrintInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStyleExcludeNullsJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleExcludeNullsJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleExcludeNullsJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStyleExcludeNullsInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleExcludeNullsInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStyleExcludeNullsUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStyleJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStyleJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStyleInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintExcludeNullsJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrintExcludeNullsJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrintExcludeNullsJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintExcludeNullsInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrintExcludeNullsInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintExcludeNullsUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrintJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStyleExcludeNullsJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleExcludeNullsJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStyleExcludeNullsJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStyleExcludeNullsInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStyleJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintExcludeNullsJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStylePrettyPrintExcludeNullsJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintExcludeNullsJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintExcludeNullsInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStyleExcludeNullsJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MicrosoftStylePrettyPrintExcludeNullsJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601 : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrint : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601ExcludeNulls : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601JSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601Inherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601Utc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601CamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintExcludeNulls : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrintJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrintInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrintUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrintCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601ExcludeNullsJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601ExcludeNullsInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601ExcludeNullsUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601ExcludeNullsCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601JSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601JSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601JSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601InheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601InheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601UtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintExcludeNullsJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrintExcludeNullsInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrintExcludeNullsUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrintExcludeNullsCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrintJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrintJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrintInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601ExcludeNullsJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601ExcludeNullsJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601ExcludeNullsJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601ExcludeNullsInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601ExcludeNullsInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601ExcludeNullsUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601JSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601JSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601JSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601InheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintExcludeNullsJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrintExcludeNullsJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrintExcludeNullsJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintExcludeNullsInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrintExcludeNullsInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintExcludeNullsUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrintJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601ExcludeNullsJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601ExcludeNullsJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601ExcludeNullsJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601ExcludeNullsInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601JSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintExcludeNullsJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601PrettyPrintExcludeNullsJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintExcludeNullsJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintExcludeNullsInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601ExcludeNullsJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601PrettyPrintExcludeNullsJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class Milliseconds : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrint : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsExcludeNulls : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintExcludeNulls : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrintJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrintInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrintUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrintCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsExcludeNullsJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsExcludeNullsInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsExcludeNullsUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsExcludeNullsCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintExcludeNullsJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrintExcludeNullsInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrintExcludeNullsUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrintExcludeNullsCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrintJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrintJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrintInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsExcludeNullsJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsExcludeNullsJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsExcludeNullsJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsExcludeNullsInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsExcludeNullsInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsExcludeNullsUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintExcludeNullsJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrintExcludeNullsJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrintExcludeNullsJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintExcludeNullsInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrintExcludeNullsInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintExcludeNullsUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrintJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsExcludeNullsJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsExcludeNullsJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsExcludeNullsJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsExcludeNullsInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintExcludeNullsJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondsPrettyPrintExcludeNullsJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintExcludeNullsJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintExcludeNullsInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsExcludeNullsJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondsPrettyPrintExcludeNullsJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123 : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrint : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123ExcludeNulls : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123JSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123Inherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123Utc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123CamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintExcludeNulls : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrintJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrintInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrintUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrintCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123ExcludeNullsJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123ExcludeNullsInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123ExcludeNullsUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123ExcludeNullsCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123JSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123JSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123JSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123InheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123InheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123UtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintExcludeNullsJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrintExcludeNullsInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrintExcludeNullsUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrintExcludeNullsCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrintJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrintJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrintInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123ExcludeNullsJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123ExcludeNullsJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123ExcludeNullsJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123ExcludeNullsInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123ExcludeNullsInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123ExcludeNullsUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123JSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123JSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123JSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123InheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintExcludeNullsJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrintExcludeNullsJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrintExcludeNullsJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintExcludeNullsInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrintExcludeNullsInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintExcludeNullsUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrintJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123ExcludeNullsJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123ExcludeNullsJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123ExcludeNullsJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123ExcludeNullsInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123JSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintExcludeNullsJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123PrettyPrintExcludeNullsJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintExcludeNullsJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintExcludeNullsInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123ExcludeNullsJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123PrettyPrintExcludeNullsJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class Seconds : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrint : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsExcludeNulls : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintExcludeNulls : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrintJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrintInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrintUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrintCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsExcludeNullsJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsExcludeNullsInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsExcludeNullsUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsExcludeNullsCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintExcludeNullsJSONP : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrintExcludeNullsInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrintExcludeNullsUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrintExcludeNullsCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrintJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrintJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrintInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsExcludeNullsJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsExcludeNullsJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsExcludeNullsJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsExcludeNullsInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsExcludeNullsInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsExcludeNullsUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintExcludeNullsJSONPInherited : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrintExcludeNullsJSONPUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrintExcludeNullsJSONPCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintExcludeNullsInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrintExcludeNullsInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintExcludeNullsUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrintJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsExcludeNullsJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsExcludeNullsJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsExcludeNullsJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsExcludeNullsInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintExcludeNullsJSONPInheritedUtc : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondsPrettyPrintExcludeNullsJSONPInheritedCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsLocal; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintExcludeNullsJSONPUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return false; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintExcludeNullsInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return false; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return false; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsExcludeNullsJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return false; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondsPrettyPrintExcludeNullsJSONPInheritedUtcCamelCase : ISerializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public bool PrettyPrint { get { return true; } }
        public bool ExcludeNulls { get { return true; } }
        public bool JSONP { get { return true; } }
        public bool IncludeInherited { get { return true; } }
        public UnspecifiedDateTimeKindBehavior DateTimeKindBehavior { get { return UnspecifiedDateTimeKindBehavior.IsUTC; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }
    // End OptionsGeneration.linq generated content
}