using StackExchange.Opserver.Data.Dashboard;

namespace StackExchange.Opserver.Views.Dashboard
{
    public class NodeGraphModel
    {
        public Node Node { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public Interface Interface { get; set; }
        public Volume Volume { get; set; }
        public object GraphData { get; set; }

        public static class KnownTypes
        {
            public const string CPU = "cpu";
            public const string Memory = "memory";
            public const string Network = "network";
            public const string Volume = "volume";
        }
    }
}