using StackExchange.Opserver.Data;

namespace StackExchange.Opserver
{
    /// <summary>
    /// For creating on-the-fly search additions
    /// </summary>
    public class SeachableNode : ISearchableNode
    {
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public MonitorStatus MonitorStatus { get; set; }

        public override string ToString() => Name;
    }

    public interface ISearchableNode
    {
        string DisplayName { get; }
        string Name { get; }
        string CategoryName { get; }
        MonitorStatus MonitorStatus { get; }
    }
}
