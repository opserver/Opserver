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
        
        /// <summary>
        /// Caches a backoff for the given command
        /// </summary>
        public static void SetBackOff(string key, int durationSeconds)
        {
            LocalCache.Set("BackOff-" + key, true, durationSeconds);
        }

        /// <summary>
        /// Whether this given key should backoff and not proceed in its action
        /// </summary>
        public static bool ShouldBackOff(string key)
        {
            return LocalCache.Get<bool?>("BackOff-" + key).GetValueOrDefault();
        }

        public static int DefaultExceptionBackOffSeconds = 5;

        /// <summary>
        /// Executes a command with a backoff.  If a previous command errored or set a backoff on execution, a default(T) will be returned.
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="key">The backoff cache key to use</param>
        /// <param name="toRun">The degate to execute</param>
        /// <param name="exceptionBackOffSeconds">How long to backoff if an exception arises</param>
        /// <param name="defaultResult">The return to provide if told to BACK THE HELL OFF</param>
        public static T ExecWithBackoff<T>(string key, Func<BackOffArgs, T> toRun, int? exceptionBackOffSeconds = null, T defaultResult = default(T))
        {
            if (ShouldBackOff(key))
                return defaultResult;
            var b = new BackOffArgs();
            try
            {
                var result = toRun(b);
                if (b.BackOffSeconds.HasValue)
                    SetBackOff(key, b.BackOffSeconds.GetValueOrDefault());
                return result;
            }
            catch (Exception)
            {
                SetBackOff(key, exceptionBackOffSeconds.GetValueOrDefault(DefaultExceptionBackOffSeconds));
                return defaultResult;
            }
        }

        public class BackOffArgs
        {
            public int? BackOffSeconds { get; set; }
        }
    }
}