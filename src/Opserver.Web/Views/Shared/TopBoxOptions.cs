using System.Collections.Generic;

namespace Opserver.Views.Shared
{
    public class TopBoxOptions
    {
        public string SearchUrl { get; set; }
        public IEnumerable<ISearchableNode> AllNodes { get; set; }

        public ISearchableNode CurrentNode { get; set; }
        public string Url { get; set; }
        public bool SearchOnly { get; set; }
        public string SearchText { get; set; }
        public string SearchValue { get; set; }
        public string QueryParam { get; set; } = "q";
        public Dictionary<string, string> SearchParams { get; set; }
    }
}