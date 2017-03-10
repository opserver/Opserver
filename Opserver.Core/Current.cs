using System;
using StackExchange.Exceptional;
using StackExchange.Opserver.SettingsProviders;
using Jil;

namespace StackExchange.Opserver
{
    internal static partial class Current
    {
        public static SettingsProvider Settings => SettingsProvider.Current;

        /// <summary>
        /// Manually write an exception to our standard exception log.
        /// </summary>
        /// <param name="message">The exception message to log.</param>
        /// <param name="innerException">The exception to wrap in an outer exception with <paramref name="message"/>.</param>
        public static void LogException(string message, Exception innerException)
        {
            var ex = new Exception(message, innerException);
            LogException(ex);
        }

        /// <summary>
        /// Manually write an exception to our standard exception log.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to log.</param>
        /// <param name="key">(Optional) The throttle cache key.</param>
        public static void LogException(Exception exception, string key = null)
        {
            if (!ShouldLog(key)) return;

            var deserializationException = exception as DeserializationException;
            if (deserializationException != null)
            {
                exception.AddLoggedData("Snippet-After", deserializationException.SnippetAfterError)
                  .AddLoggedData("Position", deserializationException.Position.ToString())
                  .AddLoggedData("Ended-Unexpectedly", deserializationException.EndedUnexpectedly.ToString());
            }

            ErrorStore.LogExceptionWithoutContext(exception, appendFullStackTrace: true);
            RecordLogged(key);
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
            LocalCache.Set("ExceptionLogRetry-" + key, true, relogDelay.Value);
        }

        /// <summary>
        /// See if an exception with the given key should be logged, based on if it was logged recently.
        /// </summary>
        /// <param name="key">The throttle cache key.</param>
        private static bool ShouldLog(string key)
        {
            if (key.IsNullOrEmpty()) return true;
            return !LocalCache.Get<bool?>("ExceptionLogRetry-"+key).GetValueOrDefault();
        }
    }
}