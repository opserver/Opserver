using System.Web;

namespace StackExchange.Opserver.Helpers
{
    public class Icon
    {
        public static IHtmlString Cache = GetIcon("usd");
        public static IHtmlString Cluster = GetIcon("share-alt");
        public static IHtmlString Cog = GetIcon("cog");
        public static IHtmlString Database = GetIcon("database");
        public static IHtmlString Disk = GetIcon("hdd-o");
        public static IHtmlString Download = GetIcon("arrow-circle-o-down");
        public static IHtmlString Memory = GetIcon("microchip");
        public static IHtmlString Network = GetIcon("exchange");
        public static IHtmlString Performance = GetIcon("bar-chart");
        public static IHtmlString Refresh = GetIcon("refresh");
        public static IHtmlString Server = GetIcon("server");
        public static IHtmlString StackOverflow = GetIcon("stack-overflow");
        public static IHtmlString Stats = GetIcon("area-chart");
        public static IHtmlString Time = GetIcon("clock-o");
        public static IHtmlString Upload = GetIcon("arrow-circle-o-up");
        public static IHtmlString Users = GetIcon("users");
        public static IHtmlString VM = GetIcon("cloud");
        public static IHtmlString X = GetIcon("times");


        private static IHtmlString GetIcon(string className) => 
            $@"<i class=""fa fa-{className}"" aria-hidden=""true""></i>".AsHtml();
    }
}