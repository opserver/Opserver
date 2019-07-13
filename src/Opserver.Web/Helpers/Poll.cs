using System.Collections.Generic;
using System.Linq;
using Jil;
using Microsoft.AspNetCore.Html;
using Opserver.Data;

namespace Opserver.Helpers
{
    public static class Poll
    {
        // TODO: Copy constructor on
        // Microsoft.AspNetCore.Mvc.Rendering.TagBuilder
        public static IHtmlContent Now(PollNode n, Cache c = null) =>
            new HtmlString($@"<a href=""#"" class=""pull-right hover-pulsate js-reload-link"" data-type=""{n.NodeType}"" data-uk=""{n.UniqueKey.HtmlEncode()}"" data-guid=""{c?.UniqueId.ToString().HtmlEncode()}"" title=""Updated {n.LastPoll?.ToZuluTime()}"">{Icon.Refresh} <span class=""js-text"">Poll Now</span></a>");

        public static IHtmlContent Now(PollNode n, params Cache[] c) =>
            new HtmlString($@"<a href=""#"" class=""pull-right hover-pulsate js-reload-link"" data-type=""{n.NodeType}"" data-uk=""{n.UniqueKey.HtmlEncode()}"" data-guid=""{JSON.Serialize(c.Select(i => i.UniqueId)).HtmlEncode()}"" title=""Updated {n.LastPoll?.ToZuluTime()}"">{Icon.Refresh} <span class=""js-text"">Poll Now</span></a>");

        public static IHtmlContent Now(IEnumerable<PollNode> nodes) =>
            nodes != null
                ? new HtmlString($@"<a href=""#"" class=""pull-right hover-pulsate js-reload-link"" data-type=""{nodes.FirstOrDefault()?.NodeType}"" data-uk=""{JSON.Serialize(nodes.Where(i => i != null).Select(i => i.UniqueKey)).HtmlEncode()}"">{Icon.Refresh} <span class=""js-text"">Poll Now</span></a>")
                : (IHtmlContent)HtmlString.Empty;

        public static IHtmlContent Now(string nodeType) =>
                new HtmlString($@"<a href=""#"" class=""pull-right hover-pulsate js-reload-link"" data-type=""{nodeType}"" data-uk=""all"">{Icon.Refresh} <span class=""js-text"">Poll Now</span></a>");

        public static IHtmlContent Now(LightweightCache cache) =>
            new HtmlString($@"<a href=""#"" class=""pull-right hover-pulsate js-reload-link"" data-type=""{Cache.TimedCacheKey}"" data-uk=""{cache.Key}"">{Icon.Refresh} <span class=""js-text"">Poll Now</span></a>");

        public static IHtmlContent Now(params LightweightCache[] caches) =>
            caches != null
                ? new HtmlString($@"<a href=""#"" class=""pull-right hover-pulsate js-reload-link"" data-type=""{Cache.TimedCacheKey}"" data-uk=""{JSON.Serialize(caches.Where(i => i != null).Select(i => i.Key)).HtmlEncode()}"">{Icon.Refresh} <span class=""js-text"">Poll Now</span></a>")
                : (IHtmlContent)HtmlString.Empty;
    }
}
