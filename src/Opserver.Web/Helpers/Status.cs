using Microsoft.AspNetCore.Html;

namespace StackExchange.Opserver.Helpers
{
    public static class StatusIndicator
    {
        public const string UpClass = "text-primary"; // TODO: text-success?
        public const string WarningClass = "text-warning";
        public const string DownClass = "text-danger";
        public const string UnknownClass = "text-muted";

        public static readonly HtmlString IconSpanGood = @"<span class=""text-success"">●</span>".AsHtml();
        public static readonly HtmlString IconSpanWarning = $@"<span class=""{WarningClass}"">●</span>".AsHtml();
        public static readonly HtmlString IconSpanCritical = $@"<span class=""{DownClass}"">●</span>".AsHtml();
        public static readonly HtmlString IconSpanUnknown = $@"<span class=""{UnknownClass}"">●</span>".AsHtml();

        public static HtmlString UpCustomSpan(string text, string tooltip = null) =>
            CustomSpan(UpClass, text, tooltip);

        public static HtmlString DownCustomSpan(string text, string tooltip = null) =>
            CustomSpan(DownClass, text, tooltip);

        public static HtmlString WarningCustomSpan(string text, string tooltip = null) =>
            CustomSpan(WarningClass, text, tooltip);

        public static HtmlString UnknownCustomSpan(string text, string tooltip = null) =>
            CustomSpan(UnknownClass, text, tooltip);

        private static HtmlString CustomSpan(string className, string text, string tooltip) =>
            $@"<span class=""{className}""{(tooltip.HasValue() ? " title=\"" + tooltip.HtmlEncode() + "\"" : "")}>{text}</span>".AsHtml();
    }
}
