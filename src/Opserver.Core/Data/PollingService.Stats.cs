using System;
using System.Linq;
using Opserver.Helpers;

namespace Opserver.Data
{
    public partial class PollingService
    {
        public GlobalPollingStatus GetPollingStatus() => new GlobalPollingStatus
        {
            MonitorStatus = _globalPollingThread.IsAlive ? (AllPollNodes.Count > 0 ? MonitorStatus.Good : MonitorStatus.Unknown) : MonitorStatus.Critical,
            MonitorStatusReason = _globalPollingThread.IsAlive ? (AllPollNodes.Count > 0 ? null : "No Poll Nodes") : "Global Polling Thread Dead",
            StartTime = _startTime,
            LastPollAll = _lastPollAll,
            IsAlive = _globalPollingThread.IsAlive,
            TotalPollIntervals = _totalPollIntervals,
            ActivePolls = _globalActivePolls,
            NodeCount = AllPollNodes.Count,
            TotalPollers = AllPollNodes.Sum(n => n.DataPollers.Count()),
            NodeBreakdown = AllPollNodes.GroupBy(n => n.GetType()).Select(g => Tuple.Create(g.Key, g.Count())).ToList(),
            Nodes = AllPollNodes.ToList()
        };

        public ThreadStats GetThreadStats() => new ThreadStats();
    }
}
