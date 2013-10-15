using System.Collections.Generic;
using StackExchange.Opserver.Data;

namespace StackExchange.Opserver.Views.Hub
{
    public class HubModel
    {
        public IEnumerable<IMonitorStatus> Items { get; set; } 
    }
}