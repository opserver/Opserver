using System.Collections.Generic;
using System.Linq;
using Jil;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Opserver.Data;

namespace Opserver.Helpers.Tag
{
    [HtmlTargetElement("poll", TagStructure = TagStructure.WithoutEndTag)]
    public class PollTagHelper : TagHelper
    {
        /// <summary>The node, if any, to poll.</summary>
        public PollNode Node { get; set; }
        /// <summary>The many nodes, if any, to poll.</summary>
        public IEnumerable<PollNode> Nodes { get; set; }
        /// <summary>The type of node, if any, to poll.</summary>
        public string NodeType { get; set; }
        /// <summary>The cache, if any, to poll.</summary>
        public Cache Cache { get; set; }
        /// <summary>The caches, if any, to poll.</summary>
        public IEnumerable<Cache> Caches { get; set; }

        // Can be passed via <email mail-to="..." />. 
        // Pascal case gets translated into lower-kebab-case.
        public string MailTo { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "a";
            output.Attributes.SetAttribute("href", "#");
            output.Attributes.SetAttribute("class", "pull-right hover-pulsate js-reload-link");
            if (NodeType.HasValue())
            {
                output.Attributes.SetAttribute("data-type", NodeType);
                output.Attributes.SetAttribute("data-uk", "all");
            }
            else if (Node != null)
            {
                output.Attributes.SetAttribute("data-type", Node.NodeType);
                output.Attributes.SetAttribute("data-uk", Node.UniqueKey);
                output.Attributes.SetAttribute("title", "Updated " + Node.LastPoll?.ToZuluTime());

                if (Cache != null)
                {
                    output.Attributes.SetAttribute("data-guid", Cache.UniqueId.ToString());
                }
                else if (Caches != null)
                {
                    output.Attributes.SetAttribute("data-guid", JSON.Serialize(Caches.Select(i => i.UniqueId)));
                }
            }
            else if (Nodes != null)
            {
                output.Attributes.SetAttribute("data-type", Nodes.FirstOrDefault()?.NodeType);
                output.Attributes.SetAttribute("data-uk", JSON.Serialize(Nodes.Where(n => n != null).Select(n => n.UniqueKey)));
            }

            output.Content.SetContent(Icon.Refresh + @" <span class=""js-text"">Poll Now</span>");

            //<a href="#" class="pull-right hover-pulsate js-reload-link" data-type="@n.NodeType" data-uk="@n.UniqueKey" data-guid="@(c?.UniqueId.ToString())" title="Updated @(n.LastPoll?.ToZuluTime())"></a>

//@helper PollNow(params LightweightCache[] caches)
//{
//    if (caches != null)
//    {
//        <a data-type="@Opserver.Data.Cache.TimedCacheKey" data-uk="@Json.Encode(caches.Where(i => i != null).Select(i => i.Key))"></a>
//    }
//}
//@helper PollNow(LightweightCache cache)
//{
//    <a data-type="@Opserver.Data.Cache.TimedCacheKey" data-uk="@cache.Key"></a>
//}
        }
    }
}
