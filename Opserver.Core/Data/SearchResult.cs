using Newtonsoft.Json;

namespace StackExchange.Opserver.Data
{
    public class SearchResult<T> : SearchResult
    {
        [JsonIgnore]
        public T Item { get; set; }
    }

    public class SearchResult : IMonitorStatus
    {
        public MonitorStatus MonitorStatus { get; set; }
        public string MonitorStatusReason { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
