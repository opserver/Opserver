using System;
using System.Collections.Generic;

namespace Opserver.Data
{
    public class GlobalPollingStatus : IMonitorStatus
    {
        public MonitorStatus MonitorStatus { get; internal set; }
        public string MonitorStatusReason { get; internal set; }
        public DateTime StartTime { get; internal set; }
        public DateTime? LastPollAll { get; internal set; }
        public bool IsAlive { get; internal set; }
        public long TotalPollIntervals { get; internal set; }
        public long ActivePolls { get; internal set; }
        public int NodeCount { get; internal set; }
        public int TotalPollers { get; internal set; }
        public List<Tuple<Type, int>> NodeBreakdown { get; internal set; }
        public List<PollNode> Nodes { get; internal set; }
    }
}
