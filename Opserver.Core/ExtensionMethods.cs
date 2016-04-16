using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Helpers;
using StackExchange.Profiling;
using StackExchange.Redis;
using TeamCitySharp.DomainEntities;

namespace StackExchange.Opserver
{
    /// <summary>
    /// Provides a centralized place for common functionality exposed via extension methods.
    /// </summary>
    public static partial class ExtensionMethods
    {
        public static string ExceptionLogPrefix = "ErrorLog-";

        /// <summary>
        /// Answers true if this String is either null or empty.
        /// </summary>
        /// <remarks>I'm so tired of typing String.IsNullOrEmpty(s)</remarks>
        public static bool IsNullOrEmpty(this string s) => string.IsNullOrEmpty(s);

        /// <summary>
        /// Answers true if this String is neither null or empty.
        /// </summary>
        /// <remarks>I'm also tired of typing !String.IsNullOrEmpty(s)</remarks>
        public static bool HasValue(this string s) => !string.IsNullOrEmpty(s);

        /// <summary>
        /// Returns the toReturn parameter when this string is null/empty.
        /// </summary>
        public static string IsNullOrEmptyReturn(this string s, string toReturn) =>
            s.HasValue()
                ? s
                : (toReturn.HasValue() ? toReturn : "");

        /// <summary>
        /// If this string ends in "toTrim", this will trim it once off the end
        /// </summary>
        public static string TrimEnd(this string s, string toTrim) =>
            s == null || toTrim == null || !s.EndsWith(toTrim)
                ? s
                : s.Substring(0, s.Length - toTrim.Length);


        /// <summary>
        /// returns Url Encoded string
        /// </summary>
        public static string UrlEncode(this string s) => s.HasValue() ? WebUtility.UrlEncode(s) : s;

        /// <summary>
        /// returns Html Encoded string
        /// </summary>
        public static string HtmlEncode(this string s) => s.HasValue() ? WebUtility.HtmlEncode(s) : s;

        /// <summary>
        /// Gets a readable type description for dashboards, e.g. "Dictionary&lt;string,string&gt;"
        /// </summary>
        public static string ReadableTypeDescription(this Type t) =>
            t.IsGenericType
                ? $"{t.Name.Split('`')[0]}<{string.Join(",", t.GetGenericArguments().Select(a => a.Name))}>"
                : t.Name;

        /// <summary>
        /// A brain dead pluralizer. 1.Pluralize("time") => "1 time"
        /// </summary>
        public static string Pluralize(this int number, string item, bool includeNumber = true)
        {
            var numString = includeNumber ? number.ToComma() + " " : "";
            return number == 1
                ? numString + item
                : numString + (item.EndsWith("y")
                    ? item.Remove(item.Length - 1) + "ies"
                    : item.EndsWith("s")
                        ? item.Remove(item.Length - 1) + "es"
                        : item.EndsWith("ex")? item+"es"
                        : item + "s");
        }

        /// <summary>
        /// A brain dead pluralizer. 1.Pluralize("time") => "1 time"
        /// </summary>
        public static string Pluralize(this long number, string item, bool includeNumber = true)
        {
            var numString = includeNumber ? number.ToComma() + " " : "";
            return number == 1
                       ? numString + item
                       : numString + (item.EndsWith("y") ? item.Remove(item.Length - 1) + "ies" : item + "s");
        }

        /// <summary>
        /// A plurailizer that accepts the count, single and plural variants of the text
        /// </summary>
        public static string Pluralize(this int number, string single, string plural, bool includeNumber = true)
        {
            var numString = includeNumber ? number.ToComma() + " " : "";
            return number == 1
                ? numString + single
                : numString + plural;
        }

        /// <summary>
        /// A plurailizer that accepts the count, single and plural variants of the text
        /// </summary>
        public static string Pluralize(this long number, string single, string plural, bool includeNumber = true)
        {
            var numString = includeNumber ? number.ToComma() + " " : "";
            return number == 1
                ? numString + single
                : numString + plural;
        }

        /// <summary>
        /// Returns the pluralized version of 'noun' when required by 'number'.
        /// </summary>
        public static string Pluralize(this string noun, int number, string pluralForm = null) =>
            number == 1
                ? noun
                : pluralForm.IsNullOrEmptyReturn((noun ?? "") + "s");

        /// <summary>
        /// force string to be maxlen or smaller
        /// </summary>
        public static string Truncate(this string s, int maxLength) =>
            s.IsNullOrEmpty()
                ? s
                : (s.Length > maxLength ? s.Remove(maxLength) : s);

        public static string TruncateWithEllipsis(this string s, int maxLength) =>
            s.IsNullOrEmpty() || s.Length <= maxLength
                ? s
                : $"{Truncate(s, Math.Max(maxLength, 3) - 3)}...";

        public static string CleanCRLF(this string s) =>
            string.IsNullOrWhiteSpace(s)
                ? s
                : s.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");

        public static string NormalizeForCache(this string s) => s?.ToLower();

        public static string NormalizeHostOrFQDN(this string s, bool defaultToHttps = false)
        {
            if (!s.HasValue()) return s;
            if (!s.StartsWith("http://") && !s.StartsWith("https://")) return $"{(defaultToHttps ? "https" : "http")}://{s}/";
            return s.EndsWith("/") ? s : $"{s}/";
        }

        public static bool HasData(this Cache cache) => cache != null && cache.ContainsData;

        public static T SafeData<T>(this Cache<T> cache, bool emptyIfMissing = false) where T : class, new() =>
            cache?.Data ?? (emptyIfMissing ? new T() : null);

        public static IEnumerable<T> WithIssues<T>(this IEnumerable<T> items) where T : IMonitorStatus =>
            items.Where(i => i.MonitorStatus != MonitorStatus.Good);
        
        public static string GetReasonSummary(this IEnumerable<IMonitorStatus> items) =>
            string.Join(", ", items.WithIssues().Select(i => i.MonitorStatusReason));

        public static MonitorStatus GetWorstStatus(this IEnumerable<IMonitorStatus> ims, string cacheKey = null, int durationSeconds = 5)
        {
            if (ims == null)
                return MonitorStatus.Unknown;
            MonitorStatus? result = null;
            if (cacheKey.HasValue())
                result = Current.LocalCache.Get<MonitorStatus?>(cacheKey);
            if (result == null)
            {
                result = GetWorstStatus(ims.Select(i => i.MonitorStatus));
                if (cacheKey.HasValue())
                    Current.LocalCache.Set(cacheKey, result, durationSeconds);
            }
            return result.Value;
        }

        public static MonitorStatus GetWorstStatus(this IEnumerable<MonitorStatus> ims)
        {
            return ims.OrderByDescending(i => i).FirstOrDefault();
        }

        public static IOrderedEnumerable<T> OrderByWorst<T>(this IEnumerable<T> ims) where T : IMonitorStatus
        {
            return OrderByWorst(ims, i => i.MonitorStatus);
        }

        public static IOrderedEnumerable<T> OrderByWorst<T>(this IEnumerable<T> ims, Func<T,MonitorStatus> getter)
        {
            return ims.OrderByDescending(getter);
        }

        public static IOrderedEnumerable<T> ThenByWorst<T>(this IOrderedEnumerable<T> ims) where T : IMonitorStatus
        {
            return ThenByWorst(ims, i => i.MonitorStatus);
        }

        public static IOrderedEnumerable<T> ThenByWorst<T>(this IOrderedEnumerable<T> ims, Func<T, MonitorStatus> getter)
        {
            return ims.ThenByDescending(getter);
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        /// <summary>
        /// Returns a unix Epoch time given a Date
        /// </summary>
        public static long ToEpochTime(this DateTime dt, bool toMilliseconds = false)
        {
            var seconds = (long) (dt - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            return toMilliseconds ? seconds * 1000 : seconds;
        }

        /// <summary>
        /// Converts to Date given an Epoch time
        /// </summary>
        public static DateTime ToDateTime(this long epoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(epoch);
        }

        /// <summary>
        /// Returns a humanized string indicating how long ago something happened, eg "3 days ago".
        /// For future dates, returns when this DateTime will occur from DateTime.UtcNow.
        /// </summary>
        public static string ToRelativeTime(this DateTime dt, bool includeTime = true, bool asPlusMinus = false, DateTime? compareTo = null, bool includeSign = true)
        {
            var comp = (compareTo ?? DateTime.UtcNow);
            if (asPlusMinus)
            {
                return dt <= comp
                    ? ToRelativeTimeSimple(comp - dt, includeSign ? "-" : "")
                    : ToRelativeTimeSimple(dt - comp, includeSign ? "+" : "");
            }
            return dt <= comp
                ? ToRelativeTimePast(dt, comp, includeTime)
                : ToRelativeTimeFuture(dt, comp, includeTime);
        }

        private static string ToRelativeTimePast(DateTime dt, DateTime utcNow, bool includeTime = true)
        {
            TimeSpan ts = utcNow - dt;
            double delta = ts.TotalSeconds;

            if (delta < 1)
            {
                return "just now";
            }
            if (delta < 60)
            {
                return ts.Seconds == 1 ? "1 sec ago" : ts.Seconds.ToString() + " secs ago";
            }
            if (delta < 3600) // 60 mins * 60 sec
            {
                return ts.Minutes == 1 ? "1 min ago" : ts.Minutes.ToString() + " mins ago";
            }
            if (delta < 86400)  // 24 hrs * 60 mins * 60 sec
            {
                return ts.Hours == 1 ? "1 hour ago" : ts.Hours.ToString() + " hours ago";
            }

            var days = ts.Days;
            if (days == 1)
            {
                return "yesterday";
            }
            if (days <= 2)
            {
                return days.ToString() + " days ago";
            }
            if (utcNow.Year == dt.Year)
            {
                return dt.ToString(includeTime ? "MMM %d 'at' %H:mmm" : "MMM %d");
            }
            return dt.ToString(includeTime ? @"MMM %d \'yy 'at' %H:mmm" : @"MMM %d \'yy");
        }

        private static string ToRelativeTimeFuture(DateTime dt, DateTime utcNow, bool includeTime = true)
        {
            TimeSpan ts = dt - utcNow;
            double delta = ts.TotalSeconds;

            if (delta < 1)
            {
                return "just now";
            }
            if (delta < 60)
            {
                return ts.Seconds == 1 ? "in 1 second" : "in " + ts.Seconds.ToString() + " seconds";
            }
            if (delta < 3600) // 60 mins * 60 sec
            {
                return ts.Minutes == 1 ? "in 1 minute" : "in " + ts.Minutes.ToString() + " minutes";
            }
            if (delta < 86400) // 24 hrs * 60 mins * 60 sec
            {
                return ts.Hours == 1 ? "in 1 hour" : "in " + ts.Hours.ToString() + " hours";
            }

            // use our own rounding so we can round the correct direction for future
            var days = (int)Math.Round(ts.TotalDays, 0);
            if (days == 1)
            {
                return "tomorrow";
            }
            if (days <= 10)
            {
                return "in " + days.ToString() + " day" + (days > 1 ? "s" : "");
            }
            // if the date is in the future enough to be in a different year, display the year
            if (utcNow.Year == dt.Year)
            {
                return "on " + dt.ToString(includeTime ? "MMM %d 'at' %H:mmm" : "MMM %d");
            }
            return "on " + dt.ToString(includeTime ? @"MMM %d \'yy 'at' %H:mmm" : @"MMM %d \'yy");
        }
        
        private static string ToRelativeTimeSimple(TimeSpan ts, string sign)
        {
            var delta = ts.TotalSeconds;
            if (delta < 1)
                return "< 1 sec";
            if (delta < 60)
                return sign + ts.Seconds.ToString() + " sec" + (ts.Seconds == 1 ? "" : "s");
            if (delta < 3600) // 60 mins * 60 sec
                return sign + ts.Minutes.ToString() + " min" + (ts.Minutes == 1 ? "" : "s");
            if (delta < 86400) // 24 hrs * 60 mins * 60 sec
                return sign + ts.Hours.ToString() + " hour" + (ts.Hours == 1 ? "" : "s");
            return sign + ts.Days.ToString() + " days";
        }

        /// <summary>
        /// Returns a string with all the DBML-mapped property names and their values. Each tuple will be separated by 'joinSeparator'.
        /// </summary>
        public static string GetPropertyNamesAndValues(this object o, string joinSeparator = "\n")
        {
            if (o == null)
                return "";

            var props = o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).AsEnumerable();

            var strings = props.Select(p => p.Name + ":" + p.GetValue(o, null));
            return string.Join(joinSeparator, strings);
        }

        public static string GetDescription<T>(this T? enumerationValue) where T : struct
        {
            return enumerationValue.HasValue ? enumerationValue.Value.GetDescription() : string.Empty;
        }

        /// <summary>
        /// Gets the Description attribute text or the .ToString() of an enum member
        /// </summary>
        public static string GetDescription<T>(this T enumerationValue) where T : struct
        {
            var type = enumerationValue.GetType();
            if (!type.IsEnum) throw new ArgumentException("EnumerationValue must be of Enum type", nameof(enumerationValue));
            var memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo.Length > 0)
            {
                var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs.Length > 0)
                    return ((DescriptionAttribute)attrs[0]).Description;
            }
            return enumerationValue.ToString();
        }
        
        /// <summary>
        /// Converts a raw long into a readable size
        /// </summary>
        public static string ToHumanReadableSize(this long size)
        {
            return string.Format(new FileSizeFormatProvider(), "{0:fs}", size);
        }

        public static string ToComma(this int? number, string valueIfZero = null)
        {
            return number.HasValue ? ToComma(number.Value, valueIfZero) : "";
        }

        public static string ToComma(this int number, string valueIfZero = null)
        {
            if (number == 0 && valueIfZero != null) return valueIfZero;
            return number.ToString("n0");
        }

        public static string ToComma(this long? number, string valueIfZero = null)
        {
            return number.HasValue ? ToComma(number.Value, valueIfZero) : "";
        }

        public static string ToComma(this long number, string valueIfZero = null)
        {
            if (number == 0 && valueIfZero != null) return valueIfZero;
            return number.ToString("n0");
        }

        public static string ToTimeStringMini(this TimeSpan span, int maxElements = 2)
        {
            var sb = StringBuilderCache.Get();
            var elems = 0;
            Action<string, int> add = (s, i) =>
                {
                    if (elems < maxElements && i > 0)
                    {
                        sb.AppendFormat("{0:0}{1} ", i, s);
                        elems++;
                    }
                };
            add("d", span.Days);
            add("h", span.Hours);
            add("m", span.Minutes);
            add("s", span.Seconds);
            add("ms", span.Milliseconds);

            if (sb.Length == 0) sb.Append("0");

            return sb.ToStringRecycle().Trim();
        }
        
        /// <summary>
        /// Adds a key/value pair for logging to an exception, one that'll appear in exceptional
        /// </summary>
        public static T AddLoggedData<T>(this T ex, string key, string value) where T : Exception
        {
            ex.Data[ExceptionLogPrefix + key] = value;
            return ex;
        }

        /// <summary>
        /// Does a Step with the location of caller as the label.
        /// </summary>
        public static IDisposable StepHere(
            this MiniProfiler profiler,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            return profiler?.Step($"{memberName} - {Path.GetFileName(sourceFilePath)}:{sourceLineNumber.ToString()}");
        }

        private static readonly ConcurrentDictionary<string, object> _getSetNullLocks = new ConcurrentDictionary<string, object>();

        internal class GetSetWrapper<T>
        {
            public DateTime StaleAfter { get; set; }
            public T Data { get; set; }
        }

        // return true if this caller won the race to load whatever would go at key
        private static bool GotCompeteLock(LocalCache cache, string key)
        {
            while (true)
            {
                var competeKey = key + "-cload";
                if (cache.SetNXSync(competeKey, DateTime.UtcNow))
                {
                    // Got it!
                    return true;
                }

                var x = cache.Get<DateTime>(competeKey);
                // Did somebody abandoned the lock?
                if (DateTime.UtcNow - x > TimeSpan.FromMinutes(5))
                {
                    // Yep, clear it and try again
                    cache.Remove(competeKey);
                    continue;
                }
                // Lost the lock competition
                return false;
            }
        }

        // called by a winner of CompeteToLoad, to make it so the next person to call CompeteToLoad will get true
        private static void ReleaseCompeteLock(LocalCache cache, string key)
        {
            cache.Remove(key + "-cload");
        }

        private static int totalGetSetSync, totalGetSetAsyncSuccess, totalGetSetAsyncError;
        /// <summary>
        /// Indicates how many sync (first), async-success (second) and async-error (third) GetSet operations have been completed
        /// </summary>
        public static Tuple<int, int, int> GetGetSetStatistics()
        {
            return Tuple.Create(Interlocked.CompareExchange(ref totalGetSetSync, 0, 0),
                Interlocked.CompareExchange(ref totalGetSetAsyncSuccess, 0, 0),
                Interlocked.CompareExchange(ref totalGetSetAsyncError, 0, 0));
        }

        /// <summary>
        /// 
        /// lookup refreshes the data if necessay, passing the old data if we have it.
        /// 
        /// durationSecs is the "time before stale" for the data
        /// serveStaleSecs is the maximum amount of time to serve data once it becomes stale
        /// 
        /// Note that one unlucky caller when the data is stale will block to fill the cache,
        /// everybody else will get stale data though.
        /// </summary>
        public static T GetSet<T>(this LocalCache cache, string key, Func<T, MicroContext, T> lookup, int durationSecs, int serveStaleDataSecs)
            where T : class
        {
            var possiblyStale = cache.Get<GetSetWrapper<T>>(key);
            var localLockName = key;
            var nullLoadLock = _getSetNullLocks.AddOrUpdate(localLockName, k => new object(), (k, old) => old);
            if (possiblyStale == null)
            {
                // We can't prevent multiple web server's from running this (well, we can but its probably overkill) but we can
                //   at least stop the query from running multiple times on *this* web server
                lock (nullLoadLock)
                {
                    possiblyStale = cache.Get<GetSetWrapper<T>>(key);

                    if (possiblyStale == null)
                    {
                        T data;
                        using (var ctx = new MicroContext())
                        {
                            data = lookup(null, ctx);
                        }
                        possiblyStale = new GetSetWrapper<T>
                        {
                            Data = data,
                            StaleAfter = DateTime.UtcNow + TimeSpan.FromSeconds(durationSecs)
                        };

                        cache.Set(key, possiblyStale, durationSecs + serveStaleDataSecs);
                        Interlocked.Increment(ref totalGetSetSync);
                    }
                }
            }

            if (possiblyStale.StaleAfter > DateTime.UtcNow) return possiblyStale.Data;

            bool gotCompeteLock = false;
            if (Monitor.TryEnter(nullLoadLock, 0))
            {   // it isn't actively being refreshed; we'll check for a mutex on the cache
                try
                {
                    gotCompeteLock = GotCompeteLock(cache, key);
                }
                finally
                {
                    Monitor.Exit(nullLoadLock);
                }
            }

            if (gotCompeteLock)
            {
                var old = possiblyStale.Data;
                var task = new Task(delegate
                {
                    lock (nullLoadLock) // holding this lock allows us to locally short-circuit all the other threads that come asking
                    {
                        try
                        {
                            var updated = new GetSetWrapper<T>();
                            using (var ctx = new MicroContext())
                            {
                                updated.Data = lookup(old, ctx);
                                updated.StaleAfter = DateTime.UtcNow + TimeSpan.FromSeconds(durationSecs);
                            }
                            cache.Remove(key);
                            cache.Set(key, updated, durationSecs + serveStaleDataSecs);
                        }
                        finally
                        {
                            ReleaseCompeteLock(cache, key);
                        }
                    }
                });
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Interlocked.Increment(ref totalGetSetAsyncError);
                        Current.LogException(t.Exception);
                    }
                    else
                    {
                        Interlocked.Increment(ref totalGetSetAsyncSuccess);
                    }
                });
                task.Start();
            }

            return possiblyStale.Data;
        }

        // In case there is some context we want here later...
        public class MicroContext : IDisposable
        {
            void IDisposable.Dispose() { }
        }
    }

    public static class ThirdPartyExtensionMethods
    {
        public static string NiceName(this Build b)
        {
            var config = BuildStatus.GetConfig(b.BuildTypeId);
            return config != null ? config.Name : "Unknown build config";
        }
        public static string NiceProjectName(this Build b)
        {
            var config = BuildStatus.GetConfig(b.BuildTypeId);
            return config != null ? config.ProjectName : "Unknown build config";
        }

        private static readonly Regex _traceRegex = new Regex(@"(.*).... \((\d+) more bytes\)$", RegexOptions.Compiled);
        public static string TraceDescription(this CommandTrace trace, int? truncateTo = null)
        {
            if (truncateTo != null)
            {
                Match match;
                if (trace.Arguments.Length >= 4 && (match = _traceRegex.Match(trace.Arguments[3])).Success)
                {
                    var str = string.Concat(string.Join(" ", trace.Arguments.Take(2)), " ", match.Groups[1].Value);
                    var bytesTotal = int.Parse(match.Groups[2].Value) + match.Groups[1].Value.Length;
                    return $"{str.TruncateWithEllipsis(truncateTo.Value)} ({bytesTotal.Pluralize("byte")} total)";
                }
                return string.Join(" ", trace.Arguments).TruncateWithEllipsis(truncateTo.Value);
            }
            return string.Join(" ", trace.Arguments);
        }
    }

    //Credits to http://stackoverflow.com/questions/128618/c-file-size-format-provider/3968504#3968504
    public static class IntToBytesExtension
    {
        private const int _precision = 2;
        private static readonly IList<string> Units = new List<string> { "", "K", "M", "G", "T" };
        
        /// <summary>
        /// Formats the value as a filesize in bytes (KB, MB, etc.)
        /// </summary>
        /// <param name="bytes">This value.</param>
        /// <param name="unit">Unit to use in the fomat, defaults to B for bytes</param>
        /// <param name="precision">How much precision to show, defaults to 2</param>
        /// <param name="zero">String to show if the value is 0</param>
        /// <returns>Filesize and quantifier formatted as a string.</returns>
        public static string ToSize(this int bytes, string unit = "B", int precision = _precision, string zero = "n/a")
        {
            return ToSize((double)bytes, unit, precision, zero: zero);
        }

        /// <summary>
        /// Formats the value as a filesize in bytes (KB, MB, etc.)
        /// </summary>
        /// <param name="bytes">This value.</param>
        /// <param name="unit">Unit to use in the fomat, defaults to B for bytes</param>
        /// <param name="precision">How much precision to show, defaults to 2</param>
        /// <param name="zero">String to show if the value is 0</param>
        /// <returns>Filesize and quantifier formatted as a string.</returns>
        public static string ToSize(this long bytes, string unit = "B", int precision = _precision, string zero = "n/a")
        {
            return ToSize((double)bytes, unit, precision, zero: zero);
        }

        /// <summary>
        /// Formats the value as a filesize in bytes (KB, MB, etc.)
        /// </summary>
        /// <param name="bytes">This value.</param>
        /// <param name="unit">Unit to use in the fomat, defaults to B for bytes</param>
        /// <param name="precision">How much precision to show, defaults to 2</param>
        /// <param name="zero">String to show if the value is 0</param>
        /// <returns>Filesize and quantifier formatted as a string.</returns>
        public static string ToSize(this float bytes, string unit = "B", int precision = _precision, string zero = "n/a")
        {
            return ToSize((double)bytes, unit, precision, zero: zero);
        }

        /// <summary>
        /// Formats the value as a filesize in bytes (KB, MB, etc.)
        /// </summary>
        /// <param name="bytes">This value.</param>
        /// <param name="unit">Unit to use in the fomat, defaults to B for bytes</param>
        /// <param name="precision">How much precision to show, defaults to 2</param>
        /// <param name="kiloSize">1k size, usually 1024 or 1000 depending on context</param>
        /// <param name="zero">String to show if the value is 0</param>
        /// <returns>Filesize and quantifier formatted as a string.</returns>
        public static string ToSize(this double bytes, string unit = "B", int precision = _precision, int kiloSize = 1024, string zero = "n/a")
        {
            if (bytes < 1) return zero;
            var pow = Math.Floor((bytes > 0 ? Math.Log(bytes) : 0) / Math.Log(kiloSize));
            pow = Math.Min(pow, Units.Count - 1);
            var value = bytes / Math.Pow(kiloSize, pow);
            return value.ToString(pow == 0 ? "F0" : "F" + precision.ToString()) + " " + Units[(int)pow] + unit;
        }
    }
}
