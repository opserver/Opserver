using Jil.Deserialize;
using System;
using System.IO;
using System.Text;
using Jil.Common;

namespace Jil
{
    /// <summary>
    /// An exception thrown when Jil encounters an error when deserializing a stream.
    /// </summary>
    public class DeserializationException : Exception
    {
        long? _Position;
        /// <summary>
        /// The position in the TextReader where an error occurred, if it is available.
        /// 
        /// This is meant for debugging purposes only, as exactly when Jil decides to abandon deserialization
        /// and throw an exception is an implementation detail.
        /// </summary>
        public long? Position
        {
            get { return _Position; }
            private set
            {
                const string dataKey = Utils.ExceptionDataKeyPrefix + nameof(Position);
                Data[dataKey] = _Position = value;
            }
        }

        string _SnippetAfterError;
        /// <summary>
        /// A snippet of text immediately after an error occurred.
        /// 
        /// This is meant for debugging purposes only, as exactly when Jil decides to abandon deserialization
        /// and throw an exception is an implementation detail.
        /// </summary>
        public string SnippetAfterError
        {
            get { return _SnippetAfterError; }
            private set
            {
                const string dataKey = Utils.ExceptionDataKeyPrefix + nameof(SnippetAfterError);
                Data[dataKey] = _SnippetAfterError = value;
            }
        }

        bool _EndedUnexpectedly;
        /// <summary>
        /// Whether or not the TextReader ended sooner than Jil expected.
        /// 
        /// This is meant for debugging purposes only, as exactly when Jil decides to abandon deserialization
        /// and throw an exception is an implementation detail.
        /// </summary>
        public bool EndedUnexpectedly
        {
            get { return _EndedUnexpectedly; }
            private set
            {
                const string dataKey = Utils.ExceptionDataKeyPrefix + nameof(EndedUnexpectedly);
                Data[dataKey] = _EndedUnexpectedly = value;
            }
        }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public override string Message
        {
            // this is overridden just so I can make _really_ damn sure
            //   that Message is always stashed in Data
            get
            {
                return (string)Data[Utils.ExceptionMessageDataKey];
            }
        }

        internal DeserializationException(Exception inner, TextReader reader, bool endedEarly)
            : base(inner.Message, inner)
        {
            SetMessage(inner.Message);
            InspectReader(reader);
            EndedUnexpectedly = endedEarly;
        }

        internal DeserializationException(Exception inner, ref ThunkReader reader, bool endedEarly)
            : base(inner.Message, inner)
        {
            SetMessage(inner.Message);
            InspectReader(ref reader);
            EndedUnexpectedly = endedEarly;
        }

        internal DeserializationException(string msg, TextReader reader, bool endedEarly) 
            : base(msg) 
        {
            SetMessage(msg);
            InspectReader(reader);
            EndedUnexpectedly = endedEarly;
        }

        internal DeserializationException(string msg, ref ThunkReader reader, bool endedEarly)
            : base(msg)
        {
            SetMessage(msg);
            InspectReader(ref reader);
            EndedUnexpectedly = endedEarly;
        }

        internal DeserializationException(string msg, TextReader reader, Exception inner, bool endedEarly)
            : base(msg, inner)
        {
            SetMessage(msg);
            InspectReader(reader);
            EndedUnexpectedly = endedEarly;
        }

        internal DeserializationException(string msg, ref ThunkReader reader, Exception inner, bool endedEarly)
            : base(msg, inner)
        {
            SetMessage(msg);
            InspectReader(ref reader);
            EndedUnexpectedly = endedEarly;
        }

        internal DeserializationException(string msg, Exception inner, bool endedEarly) 
            : base(msg + ": " + inner.Message, inner) 
        {
            SetMessage(msg + ": " + inner.Message);
            EndedUnexpectedly = endedEarly;
        }

        internal DeserializationException(Exception inner, bool endedEarly)
            : base(inner.Message, inner)
        {
            SetMessage(inner.Message);
            EndedUnexpectedly = endedEarly;
        }

        void SetMessage(string msg)
        {
            Data[Utils.ExceptionMessageDataKey] = msg;
        }

        void InspectReader(ref ThunkReader reader)
        {
            try
            {
                Position = reader.Position;

                var sb = new StringBuilder();

                int c;
                while ((c = reader.Read()) != -1 && sb.Length < 50)
                {
                    sb.Append((char)c);
                }

                SnippetAfterError = sb.ToString();
            }
            catch (Exception) { /* best effort here, things are already jacked */ }
        }

        void InspectReader(TextReader reader)
        {
            try
            {
                var asStreamReader = reader as StreamReader;
                if (asStreamReader != null)
                {
                    if (asStreamReader.BaseStream.CanSeek)
                    {
                        Position = asStreamReader.BaseStream.Position;
                    }
                }

                var sb = new StringBuilder();

                int c;
                while((c = reader.Read()) != -1 && sb.Length < 50)
                {
                    sb.Append((char)c);
                }

                SnippetAfterError = sb.ToString();
            }
            catch (Exception) { /* best effort here, things are already jacked */ }
        }
    }
}
