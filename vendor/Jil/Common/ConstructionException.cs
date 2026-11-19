using System;

namespace Jil.Common
{
    // Just a common type to act as a "couldn't build this junk!" catch-all
    sealed class ConstructionException : Exception
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

        public ConstructionException(string msg) : base(msg)
        {
            Data[Utils.ExceptionMessageDataKey] = msg;
        }
    }
}
