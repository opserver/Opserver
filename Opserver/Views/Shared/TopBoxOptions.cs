using System.Collections.Generic;

namespace StackExchange.Opserver.Views.Shared
{
    public class TopBoxOptions
    {
        public IEnumerable<ISearchableNode> AllNodes { get; set; }

        public ISearchableNode CurrentNode { get; set; }
        public string Url { get; set; }
        public bool SearchOnly { get; set; }
        public string SearchText { get; set; }
        public string SearchValue { get; set; }
        public Dictionary<string, string> SearchParams { get; set; }
    }
}