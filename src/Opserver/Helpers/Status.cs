using System.Web;

namespace StackExchange.Opserver.Helpers
{
    public static class StatusIndicator
    {
        public const string UpClass = "text-primary"; // TODO: text-success?
        public const string WarningClass = "text-warning";
        public const string DownClass = "text-danger";
        public const string UnknownClass = "text-muted";

        public static readonly IHtmlString IconSpanGood = @"<span class=""text-success"">●</span>".AsHtml();
        public static readonly IHtmlString IconSpanWarning = $@"<span class=""{WarningClass}"">●</span>".AsHtml();
        public static readonly IHtmlString IconSpanCritical = $@"<span class=""{DownClass}"">●</span>".AsHtml();
        public static readonly IHtmlString IconSpanUnknown = $@"<span class=""{UnknownClass}"">●</span>".AsHtml();

        public static IHtmlString UpCustomSpan(string text, string tooltip = null) =>
            CustomSpan(UpClass, text, tooltip);

        public static IHtmlString DownCustomSpan(string text, string tooltip = null) =>
            CustomSpan(DownClass, text, tooltip);

        public static IHtmlString WarningCustomSpan(string text, string tooltip = null) =>
            CustomSpan(WarningClass, text, tooltip);

        public static IHtmlString UnknownCustomSpan(string text, string tooltip = null) =>
            CustomSpan(UnknownClass, text, tooltip);

        private static IHtmlString CustomSpan(string className, string text, string tooltip) =>
            $@"<span class=""{className}""{(tooltip.HasValue() ? " title=\"" + tooltip.HtmlEncode() + "\"" : "")}>{text}</span>".AsHtml();
    }
}