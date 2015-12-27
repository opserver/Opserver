using System.Web;

namespace StackExchange.Opserver.Helpers
{
    public class StatusIndicator
    {
        public const string UpClass = "text-primary"; // TODO: text-success?
        public const string WarningClass = "text-warning";
        public const string DownClass = "text-danger";
        public const string UnknownClass = "text-muted";

        public static IHtmlString IconSpanGood = @"<span class=""text-success"">●</span>".AsHtml();
        public static IHtmlString IconSpanWarning = $@"<span class=""{WarningClass}"">●</span>".AsHtml();
        public static IHtmlString IconSpanCritical = $@"<span class=""{DownClass}"">●</span>".AsHtml();
        public static IHtmlString IconSpanUnknown = $@"<span class=""{UnknownClass}"">●</span>".AsHtml();


        public static IHtmlString UpCustomSpam(string text, string tooltip = null) => 
            CustomSpan(UpClass, text, tooltip);

        public static IHtmlString DownCustomSpam(string text, string tooltip = null) => 
            CustomSpan(DownClass, text, tooltip);

        public static IHtmlString WarningCustomSpam(string text, string tooltip = null) => 
            CustomSpan(WarningClass, text, tooltip);

        public static IHtmlString UnknownCustomSpam(string text, string tooltip = null) => 
            CustomSpan(UnknownClass, text, tooltip);

        private static IHtmlString CustomSpan(string className, string text, string tooltip) => 
            $@"<span class=""{className}""{(tooltip.HasValue() ? " title=\"" + tooltip.HtmlEncode() + "\"" : "")}>{text}</span>".AsHtml();
    }
}