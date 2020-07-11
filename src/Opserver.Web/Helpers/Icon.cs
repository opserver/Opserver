using Microsoft.AspNetCore.Html;

namespace Opserver.Helpers
{
    public static class Icon
    {
        public static readonly HtmlString Cache = GetIcon("usd");
        public static readonly HtmlString Cluster = GetIcon("share-alt");
        public static readonly HtmlString Cog = GetIcon("cog");
        public static readonly HtmlString Database = GetIcon("database");
        public static readonly HtmlString Disk = GetIcon("hdd-o");
        public static readonly HtmlString Download = GetIcon("arrow-circle-o-down");
        public static readonly HtmlString Memory = GetIcon("microchip");
        public static readonly HtmlString Network = GetIcon("exchange");
        public static readonly HtmlString Performance = GetIcon("bar-chart");
        public static readonly HtmlString Refresh = GetIcon("refresh");
        public static readonly HtmlString Server = GetIcon("server");
        public static readonly HtmlString StackOverflow = GetIcon("stack-overflow");
        public static readonly HtmlString Stats = GetIcon("area-chart");
        public static readonly HtmlString Time = GetIcon("clock-o");
        public static readonly HtmlString Upload = GetIcon("arrow-circle-o-up");
        public static readonly HtmlString Users = GetIcon("users");
        public static readonly HtmlString VM = GetIcon("cloud");
        public static readonly HtmlString X = GetIcon("times");

        private static HtmlString GetIcon(string className) =>
            $@"<i class=""fa fa-{className}"" aria-hidden=""true""></i>".AsHtml();
    }
}
