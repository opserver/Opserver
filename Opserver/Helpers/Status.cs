using System.Web;

namespace StackExchange.Opserver.Helpers
{
    public class StatusIndicator
    {
        public static string UpClass = "status-up";
        public static string DownClass = "status-down";
        public static string WarningClass = "status-warning";
        public static string UnknownClass = "status-unknown";

        public static IHtmlString UpCustomSpam(string text, string tooltip = null)
        {
            return CustomSpan("up", text, tooltip);
        }

        public static IHtmlString DownCustomSpam(string text, string tooltip = null)
        {
            return CustomSpan("down", text, tooltip);
        }

        public static IHtmlString WarningCustomSpam(string text, string tooltip = null)
        {
            return CustomSpan("warning", text, tooltip);
        }

        public static IHtmlString UnknownCustomSpam(string text, string tooltip = null)
        {
            return CustomSpan("unknown", text, tooltip);
        }

        private static IHtmlString CustomSpan(string className, string text, string tooltip)
        {
            return ($@"<span class=""status-{className}""{(tooltip.HasValue() ? " title=\"" + tooltip.HtmlEncode() + "\"" : "")}>{text}</span>").AsHtml();
        }

        public static IHtmlString IconSpan(string statusClass, string tooltip = null)
        {
            return $"<span class=\"status-icon {statusClass} icon\"{(tooltip.HasValue() ? "title=\"" + tooltip.HtmlEncode() + "\"" : "")}>●</span>".AsHtml();
        }
    }
}