using System.Collections.Generic;
using System.Web;
using StackExchange.Opserver.Data.SQL;
using StackExchange.Opserver.Helpers;
using static StackExchange.Opserver.Data.SQL.SQLInstance.TopSearchOptions;

namespace StackExchange.Opserver.Views.SQL
{
    public class OperationsTopModel : DashboardModel
    {
        public SQLInstance.TopSearchOptions TopSearchOptions { get; set; }
        public List<SQLInstance.TopOperation> TopOperations { get; set; }

        private IHtmlString _topSearchOptionsQueryString;
        public IHtmlString TopSearchOptionsQueryString =>
            _topSearchOptionsQueryString ?? (_topSearchOptionsQueryString = GetQueryString(TopSearchOptions));

        public static IHtmlString GetQueryString(SQLInstance.TopSearchOptions options)
        {
            var sb = StringBuilderCache.Get();
            if (options.MinExecs != Default.MinExecs) sb.Append("&").Append(nameof(options.MinExecs)).Append("=").Append(options.MinExecs.Value);
            if (options.MinExecsPerMin != Default.MinExecsPerMin) sb.Append("&").Append(nameof(options.MinExecsPerMin)).Append("=").Append(options.MinExecsPerMin.Value);
            if (options.Search != Default.Search) sb.Append("&").Append(nameof(options.Search)).Append("=").Append(options.Search.UrlEncode());
            if (options.Database != Default.Database) sb.Append("&").Append(nameof(options.Database)).Append("=").Append(options.Database.Value);
            if (options.LastRunSeconds != Default.LastRunSeconds) sb.Append("&").Append(nameof(options.LastRunSeconds)).Append("=").Append(options.LastRunSeconds.Value);

            return sb.ToStringRecycle().AsHtml();
        }
    }
}