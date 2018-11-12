using System;
using System.Runtime.Serialization;

namespace StackExchange.Opserver.Helpers
{
    public class OpserverConfigException : Exception
    {
        public OpserverConfigException() { }
        public OpserverConfigException(string message) : base(message) { }
        public OpserverConfigException(string message, Exception innerException) : base(message, innerException) { }
        protected OpserverConfigException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
