using System.Web;

namespace StackExchange.Opserver.Helpers
{
    public static class Icon
    {
        public static readonly IHtmlString Cache = GetIcon("usd");
        public static readonly IHtmlString Cluster = GetIcon("share-alt");
        public static readonly IHtmlString Cog = GetIcon("cog");
        public static readonly IHtmlString Database = GetIcon("database");
        public static readonly IHtmlString Disk = GetIcon("hdd-o");
        public static readonly IHtmlString Download = GetIcon("arrow-circle-o-down");
        public static readonly IHtmlString Memory = GetIcon("microchip");
        public static readonly IHtmlString Network = GetIcon("exchange");
        public static readonly IHtmlString Performance = GetIcon("bar-chart");
        public static readonly IHtmlString Refresh = GetIcon("refresh");
        public static readonly IHtmlString Server = GetIcon("server");
        public static readonly IHtmlString StackOverflow = GetIcon("stack-overflow");
        public static readonly IHtmlString Stats = GetIcon("area-chart");
        public static readonly IHtmlString Time = GetIcon("clock-o");
        public static readonly IHtmlString Upload = GetIcon("arrow-circle-o-up");
        public static readonly IHtmlString Users = GetIcon("users");
        public static readonly IHtmlString VM = GetIcon("cloud");
        public static readonly IHtmlString X = GetIcon("times");

        private static IHtmlString GetIcon(string className) =>
            $@"<i class=""fa fa-{className}"" aria-hidden=""true""></i>".AsHtml();
    }
}