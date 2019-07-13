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
        /// <param name="throttleKey">(Optional) The throttle cache key.</param>
        public static void Log(this Exception exception, string throttleKey = null)
        {
            if (!ShouldLog(throttleKey)) return;

            if (exception is DeserializationException deserializationException)
            {
                exception.AddLoggedData("Snippet-After", deserializationException.SnippetAfterError)
                  .AddLoggedData("Position", deserializationException.Position.ToString())
                  .AddLoggedData("Ended-Unexpectedly", deserializationException.EndedUnexpectedly.ToString());
            }

            exception.LogNoContext();
            RecordLogged(throttleKey);
        }

        /// <summary>
        /// Record that an exception was logged in local cache for the specified length of time.
        /// </summary>
        /// <param name="key">The throttle cache key.</param>
        /// <param name="relogDelay">The duration of time to wait before logging again (default: 30 minutes).</param>
        private static void RecordLogged(string key, TimeSpan? relogDelay = null)
        {
            relogDelay = relogDelay ?? 30.Minutes();
            if (key.IsNullOrEmpty() || !relogDelay.HasValue) return;
            Current.LocalCache.Set("ExceptionLogRetry-" + key, true, relogDelay.Value);
        }

        /// <summary>
        /// See if an exception with the given key should be logged, based on if it was logged recently.
        /// </summary>
        /// <param name="key">The throttle cache key.</param>
        private static bool ShouldLog(string key)
        {
            if (key.IsNullOrEmpty()) return true;
            return !Current.LocalCache.Get<bool?>("ExceptionLogRetry-" + key).GetValueOrDefault();
        }
    }
}
