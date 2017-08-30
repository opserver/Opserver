﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using System.Web.Mvc;
using Jil;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Opserver.Data.SQL;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Views.Shared;
using UnconstrainedMelody;
using System.Text;

namespace StackExchange.Opserver
{
    /// <summary>
    /// Provides a centralized place for common functionality exposed via extension methods.
    /// </summary>
    public static class WebExtensionMethods
    {
        /// <summary>
        /// This string is already correctly encoded HTML and can be sent to the client "as is" without additional encoding.
        /// </summary>
        /// <param name="html">The already-encoded HTML string.</param>
        public static IHtmlString AsHtml(this string html) => MvcHtmlString.Create(html);

        /// <summary>
        /// Title cases a string given the current culture.
        /// </summary>
        /// <param name="s">The string to convert to title case.</param>
        public static string ToTitleCase(this string s) => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s);

        /// <summary>
        /// Appends a <see cref="string"/>, HTML encoding the contents first.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to append to.</param>
        /// <param name="s">The <see cref="string"/> to encode and append.</param>
        /// <returns>The original <see cref="StringBuilder"/> for chaining.</returns>
        public static StringBuilder AppendHtmlEncode(this StringBuilder sb, string s) => sb.Append(s.HtmlEncode());

        /// <summary>
        /// Encodes an object as JSON for direct use without quote crazy
        /// </summary>
        /// <param name="o">The object to serialize.</param>
        public static IHtmlString ToJson(this object o) => JSON.Serialize(o).AsHtml();

        public static IHtmlString ToStatusSpan(this Data.HAProxy.Item item)
        {
            if (item.Status == "UP") return MvcHtmlString.Empty;

            switch (item.MonitorStatus)
            {
                case MonitorStatus.Good:
                    return ("(" + item.Status + ")").AsHtml();
                default:
                    return ("(<b>" + item.Status + "</b>)").AsHtml();
            }
        }

        public static IHtmlString ColorCode(this string s)
        {
            switch (s)
            {
                case "None":
                    return @"<span class=""text-muted"">None</span>".AsHtml();
                case "Unknown":
                    return @"<span class=""text-warning"">Unknown</span>".AsHtml();
                case "n/a":
                    return @"<span class=""text-warning"">n/a</span>".AsHtml();
                default:
                    return s.HtmlEncode().AsHtml();
            }
        }

        /// <summary>
        /// Returns an icon span representation of this MonitorStatus.
        /// </summary>
        /// <param name="status">The status to get an icon for.</param>
        public static IHtmlString IconSpan(this MonitorStatus status)
        {
            switch (status)
            {
                case MonitorStatus.Good:
                    return StatusIndicator.IconSpanGood;
                case MonitorStatus.Warning:
                case MonitorStatus.Maintenance:
                    return StatusIndicator.IconSpanWarning;
                case MonitorStatus.Critical:
                    return StatusIndicator.IconSpanCritical;
                default:
                    return StatusIndicator.IconSpanUnknown;
            }
        }

        /// <summary>
        /// Gets an icon representing the current status of a <see cref="Node"/>.
        /// </summary>
        /// <param name="node">The node to represent.</param>
        /// <returns>An icon representing the node and its current status.</returns>
        public static IHtmlString IconSpan(this Node node)
        {
            if (node == null)
                return @"<span class=""text-muted"">●</span>".AsHtml();

            var monitorStatusClass = node.MonitorStatus.TextClass(true);
            switch (node.HardwareType)
            {
                case HardwareType.Physical:
                    return $@"<i class=""{monitorStatusClass} fa fa-server"" aria-hidden=""true"" title=""Physical""></i>".AsHtml();
                case HardwareType.VirtualMachine:
                    return $@"<i class=""{monitorStatusClass} fa fa-cloud"" aria-hidden=""true"" title=""Virtual Machine""></i>".AsHtml();
                case HardwareType.Network:
                    return $@"<i class=""{monitorStatusClass} fa fa-exchange"" aria-hidden=""true"" title=""Network""></i>".AsHtml();
                //case HardwareType.Unknown:
                default:
                    return $@"<i class=""{monitorStatusClass} fa fa-question-circle-o"" aria-hidden=""true"" title=""Unknown hardware type""></i>".AsHtml();
            }
        }

        public static IHtmlString IconSpan(this IMonitorStatus status)
        {
            if (status == null)
                return @"<span class=""text-muted"">●</span>".AsHtml();

            switch (status.MonitorStatus)
            {
                case MonitorStatus.Good:
                    return StatusIndicator.IconSpanGood;
                case MonitorStatus.Warning:
                case MonitorStatus.Maintenance:
                    var reason = status.MonitorStatusReason;
                    return reason.HasValue()
                        ? $@"<span class=""text-warning"" title=""{reason.HtmlEncode()}"">●</span>".AsHtml()
                        : StatusIndicator.IconSpanWarning;
                case MonitorStatus.Critical:
                    var cReason = status.MonitorStatusReason;
                    return cReason.HasValue()
                        ? $@"<span class=""text-danger"" title=""{cReason.HtmlEncode()}"">●</span>".AsHtml()
                        : StatusIndicator.IconSpanCritical;
                default:
                    var uReason = status.MonitorStatusReason;
                    return uReason.HasValue()
                        ? $@"<span class=""text-muted"" title=""{uReason.HtmlEncode()}"">●</span>".AsHtml()
                        : StatusIndicator.IconSpanUnknown;
            }
        }

        public static IHtmlString Span(this MonitorStatus status, string text, string tooltip = null)
        {
            switch (status)
            {
                case MonitorStatus.Good:
                    return StatusIndicator.UpCustomSpan(text, tooltip);
                case MonitorStatus.Warning:
                    return StatusIndicator.WarningCustomSpan(text, tooltip);
                case MonitorStatus.Critical:
                    return StatusIndicator.DownCustomSpan(text, tooltip);
                default:
                    return StatusIndicator.UnknownCustomSpan(text, tooltip);
            }
        }

        public static string RawClass(this IMonitorStatus status) => RawClass(status.MonitorStatus);
        public static string RawClass(this MonitorStatus status, bool showGood = false, bool maint = false)
        {
            switch (status)
            {
                case MonitorStatus.Good:
                    return showGood ? "success" : "";
                case MonitorStatus.Warning:
                    return "warning";
                case MonitorStatus.Critical:
                    return "danger";
                case MonitorStatus.Maintenance:
                    return maint ? "info" : "muted";
                default:
                    return "muted";
            }
        }

        public static string RowClass(this IMonitorStatus status) => RawClass(status.MonitorStatus);
        public static string RowClass(this MonitorStatus status) => RawClass(status);

        public static string TextClass(this IMonitorStatus status) => TextClass(status.MonitorStatus);
        public static string TextClass(this MonitorStatus status, bool showGood = false)
        {
            switch (status)
            {
                case MonitorStatus.Good:
                    return showGood ? "text-success" : "";
                case MonitorStatus.Warning:
                    return "text-warning";
                case MonitorStatus.Critical:
                    return "text-danger";
                //case MonitorStatus.Maintenance:
                default:
                    return "text-muted";
            }
        }

        public static string BackgroundClass(this IMonitorStatus status) => BackgroundClass(status.MonitorStatus);
        public static string BackgroundClass(this MonitorStatus status, bool showGood = true)
        {
            switch (status)
            {
                case MonitorStatus.Good:
                    return showGood ? "bg-success" : "";
                case MonitorStatus.Warning:
                    return "bg-warning";
                case MonitorStatus.Critical:
                    return "bg-danger";
                default:
                    return "bg-muted";
            }
        }

        public static string ProgressBarClass(this MonitorStatus status)
        {
            switch (status)
            {
                case MonitorStatus.Good:
                    return "progress-bar-success";
                case MonitorStatus.Unknown:
                case MonitorStatus.Maintenance:
                case MonitorStatus.Warning:
                    return "progress-bar-warning";
                case MonitorStatus.Critical:
                    return "progress-bar-danger";
                default:
                    return "";
            }
        }

        public static IHtmlString ToPollSpan(this Cache cache, bool mini = true, bool lastSuccess = false)
        {
            if (cache?.LastPoll == null)
                return MonitorStatus.Warning.Span("Unknown", "No Data Available Yet");

            if (lastSuccess)
                return mini ? cache.LastSuccess?.ToRelativeTimeSpanMini() : cache.LastSuccess?.ToRelativeTimeSpan();

            var lf = cache.LastPoll;
            if (lf == null)
                return MonitorStatus.Warning.Span("Unknown", "No Data Available Yet");

            var dateToUse = cache.LastSuccess ?? cache.LastPoll;
            if (!cache.LastPollSuccessful)
            {
                return MonitorStatus.Warning.Span(mini ? dateToUse?.ToRelativeTime() : dateToUse?.ToRelativeTimeMini(),
                   $"Last Poll: {lf.Value.ToZuluTime()} ({lf.Value.ToRelativeTime()})\nError: {cache.ErrorMessage}");
            }

            return mini ? lf.Value.ToRelativeTimeSpanMini() : lf.Value.ToRelativeTimeSpan();
        }

        public static string ToDateOnlyStringPretty(this DateTime dt, DateTime? utcNow = null)
        {
            var now = utcNow ?? DateTime.UtcNow;
            return dt.ToString(now.Year != dt.Year ? @"MMM %d \'yy" : "MMM %d");
        }

        /// <summary>
        /// Convert a datetime to a zulu string.
        /// </summary>
        /// <param name="dt">The time to represent as zulu.</param>
        public static string ToZuluTime(this DateTime dt) => dt.ToString("u");

        /// <summary>
        /// Converts a timespan to a readable string adapted from https://stackoverflow.com/a/4423615
        /// </summary>
        /// <param name="span">The span of time to represent.</param>
        public static string ToReadableString(this TimeSpan span)
        {
            var dur = span.Duration();
            var sb = StringBuilderCache.Get();
            if (dur.Days > 0) sb.AppendFormat("{0:0} day{1}, ", span.Days, span.Days == 1 ? "" : "s");
            if (dur.Hours > 0) sb.AppendFormat("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? "" : "s");
            if (dur.Minutes > 0) sb.AppendFormat("{0:0} minute{1}, ", span.Minutes, span.Minutes == 1 ? "" : "s");
            if (dur.Seconds > 0) sb.AppendFormat("{0:0} second{1}, ", span.Seconds, span.Seconds == 1 ? "" : "s");

            if (sb.Length >= 2) sb.Length -= 2;
            return sb.ToStringRecycle().IsNullOrEmptyReturn("0 seconds");
        }

        /// <summary>
        /// Gets a HTML span element with relative time elapsed since this event occurred, eg, "3 months ago" or "yesterday"; 
        /// assumes time is *already* stored in UTC format!
        /// </summary>
        /// <param name="dt">The time to represent.</param>
        /// <param name="cssClass">(Optional) CSS class sting to add.</param>
        /// <param name="asPlusMinus">(Optional) Whether to render a +/- in front of the text.</param>
        /// <param name="compareTo">(Optional) The date to be relative to (defaults to <see cref="DateTime.UtcNow"/>).</param>
        public static IHtmlString ToRelativeTimeSpan(this DateTime dt, string cssClass = null, bool asPlusMinus = false, DateTime? compareTo = null)
        {
            // TODO: Make this a setting?
            // UTC Time is good for Stack Exchange but many people don't run their servers on UTC
            compareTo = compareTo ?? DateTime.UtcNow;
            return $@"<span title=""{dt.ToString("u")}"" class=""js-relative-time {cssClass}"">{dt.ToRelativeTime(asPlusMinus: asPlusMinus, compareTo: compareTo)}</span>".AsHtml();
        }

        /// <summary>
        /// Gets a very *small* humanized string indicating how long ago something happened, eg "3d ago".
        /// </summary>
        /// <param name="dt">The time to represent.</param>
        /// <param name="includeTimeForOldDates">(Optional) Whether to include the time portion for dates 12+ months ago.</param>
        /// <param name="includeAgo">(Optional) Whether to include the "ago" suffix on the end.</param>
        public static string ToRelativeTimeMini(this DateTime dt, bool includeTimeForOldDates = true, bool includeAgo = true)
        {
            var ts = new TimeSpan(DateTime.UtcNow.Ticks - dt.Ticks);
            var delta = ts.TotalSeconds;

            if (delta < 60)
            {
                return ts.Seconds.ToString() + "s" + (includeAgo ? " ago" : "");
            }
            if (delta < 3600) // 60 mins * 60 sec
            {
                return ts.Minutes.ToString() + "m" + (includeAgo ? " ago" : "");
            }
            if (delta < 86400)  // 24 hrs * 60 mins * 60 sec
            {
                return ts.Hours.ToString() + "h" + (includeAgo ? " ago" : "");
            }
            var days = ts.Days;
            if (days <= 2)
            {
                return days.ToString() + "d" + (includeAgo ? " ago" : "");
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
        /// <param name="dt">The time to represent, compared to <see cref="DateTime.UtcNow"/></param>
        public static string ToRelativeTimeMiniAll(this DateTime dt)
        {
            var ts = new TimeSpan(DateTime.UtcNow.Ticks - dt.Ticks);
            var delta = ts.TotalSeconds;

            if (delta < 60)
            {
                return ts.Seconds.ToString() + "s";
            }
            if (delta < 3600) // 60 mins * 60 sec
            {
                return ts.Minutes.ToString() + "m" + (ts.Seconds > 0 ? " " + ts.Seconds.ToString() + "s" : "");
            }
            if (delta < 86400)  // 24 hrs * 60 mins * 60 sec
            {
                return ts.Hours.ToString() + "h" + (ts.Minutes > 0 ? " " + ts.Minutes.ToString() + "m" : "");
            }
            return ts.Days.ToString() + "d" + (ts.Hours > 0 ? " " + ts.Hours.ToString() + "h" : "");
        }

        /// <summary>
        /// Gets an HTML span element with minified relative time elapsed since this event occurred, eg, "3mo ago" or "yday"; 
        /// assumes time is *already* stored in UTC format!
        /// </summary>
        /// <param name="dt">The time to represent.</param>
        /// <param name="includeTimeForOldDates">(Optional) Whether to include the time portion for dates 12+ months ago.</param>
        public static IHtmlString ToRelativeTimeSpanMini(this DateTime dt, bool includeTimeForOldDates = true)
        {
            return $@"<span title=""{dt.ToString("u")}"" class=""js-relative-time"">{ToRelativeTimeMini(dt, includeTimeForOldDates)}</span>".AsHtml();
        }

        /// <summary>
        /// Mini number, e.g. 1.1k
        /// </summary>
        /// <param name="number">The number to represent.</param>
        public static string Mini(this int number)
        {
            if (number >= 1000000)
                return ((double)number / 1000000).ToString("0.0m");

            if (number >= 1000)
                return ((double)number / 1000).ToString("0.#k");

            return number.ToString("#,##0");
        }

        /// <summary>
        /// Full representation of a number.
        /// </summary>
        /// <param name="number">The number to represent.</param>
        /// <returns>A formatted number, with commas.</returns>
        public static string Full(this int number) => number.ToString("#,##0");

        /// <summary>
        /// Micro representation of a number.
        /// </summary>
        /// <param name="number">The number to represent.</param>
        public static string Micro(this int number)
        {
            if (number >= 1000000)
                return ((double)number / 1000000).ToString("0.0m");

            if (number >= 1000)
                return ((double)number / 1000).ToString("0k");

            return number.ToString("#,##0");
        }

        /// <summary>
        /// Converts seconds to a human readable timespan.
        /// </summary>
        /// <param name="seconds">The number of seconds to represent.</param>
        public static IHtmlString ToTimeString(this int seconds)
        {
            if (seconds == 0) return MvcHtmlString.Empty;
            var ts = new TimeSpan(0, 0, seconds);
            var sb = StringBuilderCache.Get();
            if (ts.Days > 0)
                sb.Append("<b>").Append(ts.Days.ToString()).Append("</b>d ");
            if (ts.Hours > 0)
                sb.Append("<b>").Append(ts.Hours.ToString()).Append("</b>hr ");
            if (ts.Minutes > 0)
                sb.Append("<b>").Append(ts.Minutes.ToString()).Append("</b>min ");
            if (ts.Seconds > 0 && ts.Days == 0)
                sb.Append("<b>").Append(ts.Seconds.ToString()).Append("</b>sec ");
            return sb.ToStringRecycle().AsHtml();
        }

        private static readonly MvcHtmlString YesHtml = MvcHtmlString.Create("Yes");
        private static readonly MvcHtmlString NoHtml = MvcHtmlString.Create("No");

        public static IHtmlString ToYesNo(this bool value)
        {
            return value ? YesHtml : NoHtml;
        }

        /// <summary>
        /// Micro representation of a number in bytes. 
        /// </summary>
        /// <param name="unit">The number of bytes to represent.</param>
        public static IHtmlString MicroUnit(this long unit)
        {
            const string format = "<span title='{0}'>{1}</span>";
            var title = unit.ToString();
            string body;

            if (unit < 1000)
            {
                body = unit.ToString();
            }
            else if (unit < 1000 * 1000)
            {
                body = ((double)unit / 1000).ToString("0.##K");
            }
            else if (unit < 1000 * 1000 * 1000)
            {
                body = ((double)unit / (1000 * 1000)).ToString("0.###M");
            }
            else
            {
                body = ((double)unit / (1000 * 1000 * 1000)).ToString("0.###B");
            }

            return string.Format(format, title, body).AsHtml();
        }

        public static IHtmlString ToMutedIfNA(this string data)
        {
            return MvcHtmlString.Create(data == "n/a"
                ? @"<span class=""text-muted"">n/a</span>"
                : data.HtmlEncode());
        }

        public static string ToQueryString(this NameValueCollection nvc)
        {
            var sb = StringBuilderCache.Get();
            sb.Append("?");
            foreach (string key in nvc)
            {
                foreach (var value in nvc.GetValues(key))
                {
                    if (sb.Length > 1) sb.Append("&");
                    sb.Append(key.UrlEncode())
                        .Append("=")
                        .Append(value.UrlEncode());
                }
            }
            var result = sb.ToStringRecycle();
            return result.Length > 1 ? result : "";
        }
    }

    public static class ViewsExtensionMethods
    {
        public static void SetPageTitle(this WebViewPage page, string title)
        {
            title = title.HtmlEncode();
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
                                        Dictionary<string, string> addParams = null,
                                        string queryParam = "q")
        {
            page.ViewData[ViewDataKeys.TopBoxOptions] = new TopBoxOptions
            {
                SearchOnly = true,
                SearchText = searchText,
                SearchValue = searchValue,
                SearchParams = addParams,
                QueryParam = queryParam,
                Url = url
            };
        }

        public static void SetTopNodes(this WebViewPage page,
                                       IEnumerable<ISearchableNode> nodes,
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
                    return StatusIndicator.UpCustomSpan(state.Value.GetDescription(), tooltip);
                case SynchronizationStates.NotSynchronizing:
                case SynchronizationStates.Reverting:
                case SynchronizationStates.Initializing:
                    return StatusIndicator.DownCustomSpan(state.Value.GetDescription(), tooltip);
                default:
                    return StatusIndicator.UnknownCustomSpan(state.Value.GetDescription(), tooltip);
            }
        }

        public static IHtmlString ToSpan(this ReplicaRoles? state, string tooltip = null, bool abbreviate = false)
        {
            var desc = state.HasValue ? state.Value.GetDescription() : "";
            if (abbreviate) desc = desc.Substring(0, 1);
            switch (state)
            {
                case ReplicaRoles.Primary:
                    return StatusIndicator.UpCustomSpan(desc, tooltip);
                case ReplicaRoles.Secondary:
                    return desc.AsHtml();
                //case ReplicaRoles.Resolving:
                default:
                    return StatusIndicator.DownCustomSpan(desc, tooltip);
            }
        }
    }
}
