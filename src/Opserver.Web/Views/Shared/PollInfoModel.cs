using System.Collections.Generic;
using StackExchange.Opserver.Data;

namespace StackExchange.Opserver.Views
{
    public class PollInfoModel
    {
        public string Name { get; set; }
        public PollNode Node { get; set; }
        public IEnumerable<PollNode> Nodes { get; set; }
        public Cache Cache { get; set; }
    }
}
