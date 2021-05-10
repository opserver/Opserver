using System.Collections.Generic;
using Microsoft.AspNetCore.Html;
using Opserver.Data.SQL;
using static Opserver.Data.SQL.SQLInstance.TopSearchOptions;

namespace Opserver.Views.SQL
{
    public class OperationsTopModel : DashboardModel
    {
        public SQLInstance.TopSearchOptions TopSearchOptions { get; set; }
        public List<SQLInstance.TopOperation> TopOperations { get; set; }

        private HtmlString _topSearchOptionsQueryString;
        public HtmlString TopSearchOptionsQueryString => _topSearchOptionsQueryString ??= GetQueryString(TopSearchOptions);

        public static HtmlString GetQueryString(SQLInstance.TopSearchOptions options)
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
