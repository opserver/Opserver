using System;
using StackExchange.Opserver.Data.Dashboard.Providers;

namespace StackExchange.Opserver.Data.Dashboard
{
    public class Application
    {
        internal DashboardDataProvider DataProvider { get; set; }

        public int Id { get; internal set; }
        public int NodeId { get; internal set; }
        public string NiceName { get; internal set; }
        public string AppName { get; internal set; }
        public string ComponentName { get; internal set; }

        public int? ProcessID { get; internal set; }

        public decimal? PercentCPU { get; internal set; }
        public long? MemoryUsed { get; internal set; }
    }
}