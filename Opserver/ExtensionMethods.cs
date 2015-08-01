using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Data.SQL;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Views.Shared;

namespace StackExchange.Opserver
{
    /// <summary>
    /// Provides a centralized place for common functionality exposed via extension methods.
    /// </summary>
    public static class WebExtensionMethods
    {
        /// <summary>
        /// returns Url Encoded string
        /// </summary>
        public static string UrlEncode(this string s)
        {
            return s.HasValue() ? HttpUtility.UrlEncode(s) : s;
        }

        /// <summary>
        /// Returns a url encoded string with any + converted to %20 for better query string transport.
        /// </summary>
        public static string QueryStringEncode(this string s)
        {
            return s.HasValue() ? HtmlUtilities.QueryStringEncode(s) : s;
        }

        /// <summary>
        /// returns Html Encoded string
        /// </summary>
        public static string HtmlEncode(this string s)
        {
            return s.HasValue() ? HttpUtility.HtmlEncode(s) : s;
        }

        /// <summary>
        /// Title cases a string given the current culture
        /// </summary>
        public static string ToTitleCase(this string s)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s);
        }

        /// <summary>
        /// Gets the count of lines in a string
        /// </summary>
        public static int LineCount(this string s)
        {
            return s.Count(c => c == '\n') + 1;
        }

        public static IHtmlString ToStatusSpan(this Data.HAProxy.Item item)
        {
            if (item.Status == "UP") return MvcHtmlString.Empty;

            switch (item.MonitorStatus)
            {
                case MonitorStatus.Good:
                    return $"({item.Status})".AsHtml();
                default:
                    return $"(<b>{item.Status}</b>)".AsHtml();
            }
        }

        /// <summary>
        /// Returns an icon span representation of this MonitorStatus
        /// </summary>
        public static IHtmlString IconSpan(this MonitorStatus status)
        {
            switch (status)
            {
                case MonitorStatus.Good:
                    return StatusIndicator.IconSpan(StatusIndicator.UpClass);
                case MonitorStatus.Warning:
                case MonitorStatus.Maintenance:
                    return StatusIndicator.IconSpan(StatusIndicator.WarningClass);
                case MonitorStatus.Critical:
                    return StatusIndicator.IconSpan(StatusIndicator.DownClass);
                default:
                    return StatusIndicator.IconSpan(StatusIndicator.UnknownClass);
            }
        }

        public static IHtmlString IconSpan(this IMonitorStatus status)
        {
            if (status == null)
                return StatusIndicator.IconSpan(StatusIndicator.UnknownClass);

            switch (status.MonitorStatus)
            {
                case MonitorStatus.Good:
                    return StatusIndicator.IconSpan(StatusIndicator.UpClass);
                case MonitorStatus.Warning:
                case MonitorStatus.Maintenance:
                    return StatusIndicator.IconSpan(StatusIndicator.WarningClass, status.MonitorStatusReason);
                case MonitorStatus.Critical:
                    return StatusIndicator.IconSpan(StatusIndicator.DownClass, status.MonitorStatusReason);
                default:
                    return StatusIndicator.IconSpan(StatusIndicator.UnknownClass, status.MonitorStatusReason);
            }
        }

        public static IHtmlString Span(this MonitorStatus status, string text, string tooltip = null)
        {
            switch (status)
            {
                case MonitorStatus.Good:
                    return StatusIndicator.UpCustomSpam(text, tooltip);
                case MonitorStatus.Warning:
                    return StatusIndicator.WarningCustomSpam(text, tooltip);
                case MonitorStatus.Critical:
                    return StatusIndicator.DownCustomSpam(text, tooltip);
                default:
                    return StatusIndicator.UnknownCustomSpam(text, tooltip);
            }
        }
        
        public static string Class(this MonitorStatus status)
        {
            switch (status)
            {
                case MonitorStatus.Good:
                    return StatusIndicator.UpClass;
                case MonitorStatus.Warning:
                    return StatusIndicator.WarningClass;
                case MonitorStatus.Critical:
                    return StatusIndicator.DownClass;
                default:
                    return StatusIndicator.UnknownClass;
            }
        }

        public static string RowClass(this IMonitorStatus status)
        {
            return RowClass(status.MonitorStatus);
        }

        public static string RowClass(this MonitorStatus status)
        {
            switch (status)
            {
                case MonitorStatus.Good:
                    return "good-row";
                case MonitorStatus.Warning:
                    return "warning-row";
                case MonitorStatus.Critical:
                    return "critical-row";
                default:
                    return "unknown-row";
            }
        }
        
        public static IHtmlString ToPollSpan(this Cache cache, bool mini = true, bool lastSuccess = false)
        {
            if (cache == null)
                return MonitorStatus.Warning.Span("Unknown", "No Data Available Yet");

            if (lastSuccess)
                return mini ? cache.LastSuccess.ToRelativeTimeSpanMini() : cache.LastSuccess.ToRelativeTimeSpan();
            
            var lf = cache.LastPoll;
            if (lf == DateTime.MinValue)
                return MonitorStatus.Warning.Span("Unknown", "No Data Available Yet");

            var dateToUse = cache.LastSuccess ?? cache.LastPoll;
            if (cache.LastPollStatus == FetchStatus.Fail)
                return MonitorStatus.Warning.Span(mini ? dateToUse.ToRelativeTime() : dateToUse.ToRelativeTimeMini(),
                    $"Last Poll: {lf.ToZuluTime()} ({lf.ToRelativeTime()})\nError: {cache.ErrorMessage}");

            return mini ? lf.ToRelativeTimeSpanMini() : lf.ToRelativeTimeSpan();
        }
        
        public static string ToDateOnlyString(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd");
        }

        public static MvcHtmlString ToDateOnlySpanPretty(this DateTime dt, string cssClass)
        {
            return MvcHtmlString.Create($@"<span title=""{dt:u}"" class=""{cssClass}"">{ToDateOnlyStringPretty(dt, DateTime.UtcNow)}</span>");
        }

        public static string ToDateOnlyStringPretty(this DateTime dt, DateTime? utcNow = null)
        {
            var now = utcNow ?? DateTime.UtcNow;
            return dt.ToString(now.Year != dt.Year ? @"MMM %d \'yy" : "MMM %d");
        }

        /// <summary>
        /// Convert a nullable datetime to a zulu string
        /// </summary>
        public static string ToZuluTime(this DateTime? dt)
        {
            return !dt.HasValue ? string.Empty : ToZuluTime(dt.Value);
        }

        /// <summary>
        /// Convert a datetime to a zulu string
        /// </summary>
        public static string ToZuluTime(this DateTime dt)
        {
            return dt.ToString("u");
        }

        /// <summary>
        /// Converts a timespan to a readable string adapted from http://stackoverflow.com/a/4423615
        /// </summary>
        public static string ToReadableString(this TimeSpan span)
        {
            var dur = span.Duration();
            var sb = new StringBuilder();
            if (dur.Days > 0) sb.AppendFormat("{0:0} day{1}, ", span.Days, span.Days == 1 ? "" : "s");
            if (dur.Hours > 0) sb.AppendFormat("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? "" : "s");
            if (dur.Minutes > 0) sb.AppendFormat("{0:0} minute{1}, ", span.Minutes, span.Minutes == 1 ? "" : "s");
            if (dur.Seconds > 0) sb.AppendFormat("{0:0} second{1}, ", span.Seconds, span.Seconds == 1 ? "" : "s");

            if (sb.Length >= 2) sb.Length -= 2;
            return sb.ToString().IsNullOrEmptyReturn("0 seconds");
        }

        /// <summary>
        /// returns a html span element with relative time elapsed since this event occurred, eg, "3 months ago" or "yesterday"; 
        /// assumes time is *already* stored in UTC format!
        /// </summary>
        public static IHtmlString ToRelativeTimeSpan(this DateTime dt)
        {
            return ToRelativeTimeSpan(dt, "relativetime");
        }
        public static IHtmlString ToRelativeTimeSpan(this DateTime dt, string cssclass, bool asPlusMinus = false, DateTime? compareTo = null)
        {
            // TODO: Make this a setting?
            // UTC Time is good for Stack Exchange but many people don't run their servers on UTC
            compareTo = compareTo ?? DateTime.UtcNow;
            if (string.IsNullOrEmpty(cssclass))
                return $@"<span title=""{dt:u}"">{dt.ToRelativeTime(asPlusMinus: asPlusMinus, compareTo: compareTo)}</span>".AsHtml();
            else
                return string.Format(@"<span title=""{0:u}"" class=""{2}"">{1}</span>", dt, dt.ToRelativeTime(asPlusMinus: asPlusMinus, compareTo: compareTo), cssclass).AsHtml();
        }
        public static IHtmlString ToRelativeTimeSpan(this DateTime? dt, string cssclass = "")
        {
            return dt == null
                       ? MvcHtmlString.Empty
                       : ToRelativeTimeSpan(dt.Value, "relativetime" + (cssclass.HasValue() ? " " + cssclass : ""));
        }


        /// <summary>
        /// returns a very *small* humanized string indicating how long ago something happened, eg "3d ago"
        /// </summary>
        public static string ToRelativeTimeMini(this DateTime dt, bool includeTimeForOldDates = true, bool includeAgo = true)
        {
            var ts = new TimeSpan(DateTime.UtcNow.Ticks - dt.Ticks);
            var delta = ts.TotalSeconds;

            if (delta < 60)
            {
                return ts.Seconds + "s" + (includeAgo ? " ago" : "");
            }
            if (delta < 3600) // 60 mins * 60 sec
            {
                return ts.Minutes + "m" + (includeAgo ? " ago" : "");
            }
            if (delta < 86400)  // 24 hrs * 60 mins * 60 sec
            {
                return ts.Hours + "h" + (includeAgo ? " ago" : "");
            }
            var days = ts.Days;
            if (days <= 2)
            {
                return days + "d" + (includeAgo ? " ago" : "");
            }
            else if (days <= 330)
            {
                return dt.ToString(includeTimeForOldDates ? "MMM %d 'at' %H:mmm" : "MMM %d").ToLowerInvariant();
            }
            return dt.ToString(includeTimeForOldDates ? @"MMM %d \'yy 'at' %H:mmm" : @"MMM %d \'yy").ToLowerInvariant();
        }


        /// <summary>
        /// returns a very *small* humanized string indicating how long ago something happened, e.g. "3d 10m" or "2m 10s"
        /// </summary>
        public static string ToRelativeTimeMiniAll(this DateTime dt)
        {
            var ts = new TimeSpan(DateTime.UtcNow.Ticks - dt.Ticks);
            var delta = ts.TotalSeconds;

            if (delta < 60)
            {
                return ts.Seconds + "s";
            }
            if (delta < 3600) // 60 mins * 60 sec
            {
                return ts.Minutes + "m" + ((ts.Seconds > 0) ? " " + ts.Seconds + "s" : "");
            }
            if (delta < 86400)  // 24 hrs * 60 mins * 60 sec
            {
                return ts.Hours + "h" + ((ts.Minutes > 0) ? " " + ts.Minutes + "m" : "");
            }
            return ts.Days + "d" + ((ts.Hours > 0) ? " " + ts.Hours + "h" : "");
        }

        /// <summary>
        /// returns AN HTML SPAN ELEMENT with minified relative time elapsed since this event occurred, eg, "3mo ago" or "yday"; 
        /// assumes time is *already* stored in UTC format!
        /// </summary>
        public static IHtmlString ToRelativeTimeSpanMini(this DateTime dt, bool includeTimeForOldDates = true)
        {
            return $@"<span title=""{dt:u}"" class=""relativetime"">{ToRelativeTimeMini(dt, includeTimeForOldDates)}</span>".AsHtml();
        }
        /// <summary>
        /// returns AN HTML SPAN ELEMENT with minified relative time elapsed since this event occurred, eg, "3mo ago" or "yday"; 
        /// assumes time is *already* stored in UTC format!
        /// If this DateTime? is null, will return empty string.
        /// </summary>
        public static IHtmlString ToRelativeTimeSpanMini(this DateTime? dt, bool includeTimeForOldDates = true)
        {
            return dt == null
                       ? MvcHtmlString.Empty
                       : ToRelativeTimeSpanMini(dt.Value, includeTimeForOldDates);
        }

        /// <summary>
        /// Mini rep - 1.1k allowed
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string Mini(this int number)
        {
            if (number >= 1000000)
                return $"{(double) number/1000000:0.0m}";

            if (number >= 1000)
                return $"{(double) number/1000:0.#k}";

            return $"{number:#,##0}";
        }
        /// <summary>
        /// Full representation of a number
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string Full(this int number)
        {
            return $"{number:#,##0}";
        }

        /// <summary>
        /// Micro representation of a number
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string Micro(this int number)
        {
            if (number >= 1000000)
                return $"{(double) number/1000000:0.0m}";

            if (number >= 100000)
                return $"{(double) number/1000:0k}";

            if (number >= 10000)
                return $"{(double) number/1000:0k}";

            if (number >= 1000)
                return $"{(double) number/1000:0k}";

            return $"{number:#,##0}";
        }

        /// <summary>
        /// Adds the parameter items to this list.
        /// </summary>
        public static void AddAll<T>(this List<T> list, params T[] items)
        {
            list.AddRange(items);
        }

        /// <summary>
        /// Converts seconds to a human readable timespan
        /// </summary>
        public static IHtmlString ToTimeString(this int seconds)
        {
            if (seconds == 0) return MvcHtmlString.Empty;
            var ts = new TimeSpan(0, 0, seconds);
            var sb = new StringBuilder();
            if (ts.Days > 0)
                sb.AppendFormat("<b>{0}</b>d ", ts.Days);
            if (ts.Hours > 0)
                sb.AppendFormat("<b>{0}</b>hr ", ts.Hours);
            if (ts.Minutes > 0)
                sb.AppendFormat("<b>{0}</b>min ", ts.Minutes);
            if (ts.Seconds > 0 && ts.Days == 0)
                sb.AppendFormat("<b>{0}</b>sec ", ts.Seconds);
            return sb.ToString().AsHtml();
        }

        public static IHtmlString ToYesNo(this int number)
        {
            return MvcHtmlString.Create(number == 1 ? "Yes" : "No");
        }

        /// <summary>
        /// Micro representation of a time in millisecs 
        /// </summary>
        public static IHtmlString MicroTime(this long time)
        {
            const string format = "<span title='{0}'>{1}</span>";
            string title;
            string body;

            if (time < 1000)
            {
                title = time + " milliseconds";
                body = time + "ms";
            }
            else if (time < 1000 * 500)
            {
                title = $"{(double) time/1000:0.###}" + " seconds";
                body = $"{(double) time/1000:0.#} secs";
            }
            else if (time < 1000 * 60 * 300)
            {
                title = $"{(double) time/(1000*60):0.###}" + " minutes";
                body = $"{(double) time/(1000*60):0.#} mins";
            }
            else
            {
                title = $"{(double) time/(1000*60*60):0.###}" + " hours";
                body = $"{(double) time/(1000*60*60):0.#} hours";
            }

            return string.Format(format, title, body).AsHtml();
        }

        /// <summary>
        /// Micro representation of a time in millisecs 
        /// </summary>
        public static IHtmlString MicroUnit(this long unit)
        {
            const string format = "<span title='{0}'>{1}</span>";
            var title = unit + "";
            string body;

            if (unit < 1000)
            {
                body = unit + "";
            }
            else if (unit < 1000 * 1000)
            {
                body = $"{(double) unit/1000:0.##}K";
            }
            else if (unit < 1000 * 1000 * 1000)
            {
                body = $"{(double) unit/(1000*1000):0.###}M";
            }
            else
            {
                body = $"{(double) unit/(1000*1000*1000):0.###}B";
            }

            return string.Format(format, title, body).AsHtml();
        }

        public static IHtmlString ToNoteIfNA(this string data)
        {
            return MvcHtmlString.Create(data == "n/a"
                                            ? string.Concat(@"<span class=""note"">", data.HtmlEncode(), "</span>")
                                            : data.HtmlEncode());
        }
    }

    public static class ViewsExtensionMethods
    {
        public static void SetPageTitle(this WebViewPage page, string title)
        {
            title = HtmlUtilities.Encode(title);
            page.ViewData[ViewDataKeys.PageTitle] = GetPageTitle(page, title);
        }

        public static string GetPageTitle(this WebViewPage page, string title)
        {
            return title.IsNullOrEmpty() ? SiteSettings.SiteName : string.Concat(title, " - ", SiteSettings.SiteName);
        }

        public static void SetTopSearch(this WebViewPage page,
                                        string searchText,
                                        string searchValue = null,
                                        string url = null,
                                        Dictionary<string, string> addParams = null)
        {
            page.ViewData[ViewDataKeys.TopBoxOptions] = new TopBoxOptions
                {
                    SearchOnly = true,
                    SearchText = searchText,
                    SearchValue = searchValue,
                    SearchParams = addParams,
                    Url = url
                };
        }

        public static void SetTopNodes(this WebViewPage page, IEnumerable<ISearchableNode> nodes,
                                       string searchText,
                                       ISearchableNode currentNode = null,
                                       string url = null)
        {
            page.ViewData[ViewDataKeys.TopBoxOptions] = new TopBoxOptions
                {
                    AllNodes = nodes,
                    CurrentNode = currentNode,
                    SearchText = searchText,
                    Url = url
                };
        }
    }
    
    public static class EnumExtensions
    {
        public static IHtmlString ToSpan(this SynchronizationStates? state, string tooltip = null)
        {
            switch (state)
            {
                case SynchronizationStates.Synchronizing:
                case SynchronizationStates.Synchronized:
                    return StatusIndicator.UpCustomSpam(state.GetDescription(), tooltip);
                case SynchronizationStates.NotSynchronizing:
                case SynchronizationStates.Reverting:
                case SynchronizationStates.Initializing:
                    return StatusIndicator.DownCustomSpam(state.GetDescription(), tooltip);
                default:
                    return StatusIndicator.UnknownCustomSpam(state.GetDescription(), tooltip);
            }
        }
        public static IHtmlString ToSpan(this ReplicaRoles? state, string tooltip = null, bool abbreviate = false)
        {
            var desc = state.GetDescription();
            if (abbreviate) desc = desc.Substring(0, 1);
            switch (state)
            {
                case ReplicaRoles.Primary:
                    return StatusIndicator.UpCustomSpam(desc, tooltip);
                case ReplicaRoles.Secondary:
                    return desc.AsHtml();
                //case ReplicaRoles.Resolving:
                default:
                    return StatusIndicator.DownCustomSpam(desc, tooltip);
            }
        }
    }
}
