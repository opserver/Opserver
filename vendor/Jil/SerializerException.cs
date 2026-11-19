using Jil.Common;
using System;
using System.Reflection;

namespace Jil
{
    /// <summary>
    /// An exception thrown when Jil encounters an error while serializing an object.
    /// </summary>
    public class SerializerException : Exception
    {
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

        internal SerializerException(string message, Exception innerException) :
            base(message + ": " + innerException.Message, innerException)
        {
            Data[Utils.ExceptionMessageDataKey] = message + ": " + innerException.Message;
        }

        internal static readonly ConstructorInfo Constructor_String = typeof(SerializerException).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new [] { typeof(string) });
        internal SerializerException(string message) : base(message)
        {
            Data[Utils.ExceptionMessageDataKey] = message;
        }
    }
}
