using System;
using StackExchange.Exceptional;

namespace Opserver
{
    public static partial class ExtensionMethods
    {
        /// <summary>
        /// Manually write an exception to our standard exception log.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to log.</param>
        public static void Log(this Exception exception) => exception.LogNoContext();
    }
}
