using System;
using StackExchange.Opserver.Data.Dashboard.Providers;

namespace StackExchange.Opserver.Data.Dashboard
{
    public class Application
    {
        internal DashboardDataProvider DataProvider { get; set; }

        public string Id { get; internal set; }
        public string NodeId { get; internal set; }
        public string NiceName { get; internal set; }
        public string AppName { get; internal set; }
        public string ComponentName { get; internal set; }
        public DateTime LastUpdated { get; internal set; }
        public bool IsUnwatched { get; internal set; }
        public DateTime? UnwatchedFrom { get; internal set; }
        public DateTime? UnwatchedUntil { get; internal set; }

        public int? ProcessID { get; internal set; }
        public string ProcessName { get; internal set; }
        public DateTime? LastTimeUp { get; internal set; }

        public double? CurrentPercentCPU { get; internal set; }
        public double? CurrentPercentMemory { get; internal set; }
        public long? CurrentMemoryUsed { get; internal set; }
        public long? CurrentVirtualMemoryUsed { get; internal set; }

        public decimal? PercentCPU { get; internal set; }
        public decimal? PercentMemory { get; internal set; }
        public long? MemoryUsed { get; internal set; }
        public long? VirtualMemoryUsed { get; internal set; }
        public string ErrorMessage { get; internal set; }
    }
}