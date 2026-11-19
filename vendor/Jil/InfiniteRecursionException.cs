using Jil.Common;
using System;

namespace Jil
{
    /// <summary>
    /// An exception throw when Jil suspects it's in an infinite recursive case.
    /// 
    /// Note that this is detected heuristically, exactly how many recursions must occur
    /// before it is thrown depends on the configuration and version of Jil, as well as the object being serialized.
    /// </summary>
    public class InfiniteRecursionException : Exception
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

        internal InfiniteRecursionException() : base("Jil detected and infinite recursion and bailed")
        {
            Data[Utils.ExceptionMessageDataKey] = "Jil detected and infinite recursion and bailed";
        }
    }
}
