using System;
using Jil;
using StackExchange.Exceptional;

namespace Opserver
{
    public static partial class ExtensionMethods
    {
        /// <summary>
        /// Manually write an exception to our standard exception log.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to log.</param>
        public static void Log(this Exception exception)
        {
            if (exception is DeserializationException deserializationException)
            {
                exception.AddLoggedData("Snippet-After", deserializationException.SnippetAfterError)
                  .AddLoggedData("Position", deserializationException.Position.ToString())
                  .AddLoggedData("Ended-Unexpectedly", deserializationException.EndedUnexpectedly.ToString());
            }

            exception.LogNoContext();
        }
    }
}
