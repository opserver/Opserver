using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Deserialize
{
    interface IDeserializeOptions
    {
        DateTimeFormat DateFormat { get; }
        SerializationNameFormat SerializationNameFormat { get; }
    }

    static class TypeCache<TOptions, T>
        where TOptions : IDeserializeOptions, new()
    {
        static readonly object ThunkInitLock = new object();
        static volatile bool ThunkBeingBuilt = false;
        public static volatile Func<TextReader, int, T> Thunk;
        public static Exception ExceptionDuringBuildFromStream;

        static readonly object StringThunkInitLock = new object();
        static volatile bool StringThunkBeingBuilt = false;
        public static volatile StringThunkDelegate<T> StringThunk;
        public static Exception ExceptionDuringBuildFromString;

        public static Func<TextReader, int, T> Get()
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

                var options = new TOptions();

                Thunk = InlineDeserializerHelper.BuildFromStream<T>(typeof(TOptions), options.DateFormat, options.SerializationNameFormat, exceptionDuringBuild: out ExceptionDuringBuildFromStream);
            }
        }

        public static StringThunkDelegate<T> GetFromString()
        {
            LoadFromString();
            return StringThunk;
        }

        public static void LoadFromString()
        {
            if (StringThunk != null) return;

            lock (StringThunkInitLock)
            {
                if (StringThunk != null || StringThunkBeingBuilt) return;
                StringThunkBeingBuilt = true;

                var options = new TOptions();

                StringThunk = InlineDeserializerHelper.BuildFromString<T>(typeof(TOptions), options.DateFormat, options.SerializationNameFormat, exceptionDuringBuild: out ExceptionDuringBuildFromString);
            }
        }
    }

    class MicrosoftStyle : IDeserializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public SerializationNameFormat SerializationNameFormat { get {  return SerializationNameFormat.Verbatim; } }
    }

    class MillisecondStyle : IDeserializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class SecondStyle : IDeserializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class ISO8601Style : IDeserializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class RFC1123Style : IDeserializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.Verbatim; } }
    }

    class MicrosoftStyleCamelCase : IDeserializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class MillisecondStyleCamelCase : IDeserializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.MillisecondsSinceUnixEpoch; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class SecondStyleCamelCase : IDeserializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.SecondsSinceUnixEpoch; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class ISO8601StyleCamelCase : IDeserializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.ISO8601; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }

    class RFC1123StyleCamelCase : IDeserializeOptions
    {
        public DateTimeFormat DateFormat { get { return DateTimeFormat.RFC1123; } }
        public SerializationNameFormat SerializationNameFormat { get { return SerializationNameFormat.CamelCase; } }
    }
}