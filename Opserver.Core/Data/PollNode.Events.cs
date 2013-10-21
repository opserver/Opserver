using System;

namespace StackExchange.Opserver.Data
{
    public partial class PollNode
    {
        public event EventHandler<MonitorStatusArgs> MonitorStatusChanged;
        public event EventHandler<PollStartArgs> Polling;
        public event EventHandler<PollResultArgs> Polled;
        public event EventHandler CacheItemFetching;
        public event EventHandler CacheItemFetched;

        public class PollStartArgs : EventArgs
        {
            /// <summary>
            /// Whether to abort the poll
            /// </summary>
            public bool AbortPoll { get; set; }
        }

        public class PollResultArgs : EventArgs
        {
            public int Polled { get; internal set; }
        }

        public class MonitorStatusArgs : EventArgs
        {
            public MonitorStatus OldMonitorStatus { get; internal set; }
            public MonitorStatus NewMonitorStatus { get; internal set; }
        }
    }
}
