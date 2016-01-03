using System;
using StackExchange.Exceptional;
using StackExchange.Opserver.SettingsProviders;

namespace StackExchange.Opserver
{
    internal static partial class Current
    {
        public static SettingsProvider Settings => SettingsProvider.Current;

        /// <summary>
        /// manually write an exception to our standard exception log
        /// </summary>
        public static void LogException(string message, Exception innerException, string key = null, int? reLogDelaySeconds = null)
        {
            var ex = new Exception(message, innerException);
            LogException(ex);
        }

        /// <summary>
        /// manually write an exception to our standard exception log
        /// </summary>
        public static void LogException(Exception ex, string key = null, int? reLogDelaySeconds = null)
        {
            if (!ShouldLog(key)) return;
            ErrorStore.LogExceptionWithoutContext(ex, appendFullStackTrace: true);
            RecordLogged(key);
        }

        /// <summary>
        /// record that an exception was logged in local cache for the specified length of time (default of 30 minutes)
        /// </summary>
        private static void RecordLogged(string key, int? reLogDelaySeconds = 30 * 60 * 60)
        {
            if (key.IsNullOrEmpty() || !reLogDelaySeconds.HasValue) return;
            LocalCache.Set("ExceptionLogRetry-" + key, true, reLogDelaySeconds.Value);
        }

        /// <summary>
        /// see if an exception with the given key should be logged, based on if it was logged recently
        /// </summary>
        private static bool ShouldLog(string key)
        {
            if (key.IsNullOrEmpty()) return true;
            return !LocalCache.Get<bool?>("ExceptionLogRetry-"+key).GetValueOrDefault();
        }
    }
}