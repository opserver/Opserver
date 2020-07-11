using Opserver.Data;

namespace Opserver
{
    public interface ISearchableNode
    {
        string DisplayName { get; }
        string Name { get; }
        string CategoryName { get; }
        MonitorStatus MonitorStatus { get; }
    }
}
