using System.Collections.Generic;
using Opserver.Data;

namespace Opserver.Helpers
{
    public sealed class TestItem : IMonitorStatus
    {
        public static readonly TestItem Good = new TestItem(MonitorStatus.Good);
        public static readonly TestItem Warning = new TestItem(MonitorStatus.Warning);
        public static readonly TestItem Maintenance = new TestItem(MonitorStatus.Maintenance);
        public static readonly TestItem Critical = new TestItem(MonitorStatus.Critical);
        public static readonly TestItem Unknown = new TestItem(MonitorStatus.Unknown);

        public MonitorStatus MonitorStatus { get; }
        public string MonitorStatusReason { get; }

        private TestItem(MonitorStatus status)
        {
            MonitorStatus = status;
            MonitorStatusReason = status.ToString();
        }

        public static IEnumerable<TestItem> Batch(int good = 0, int warning = 0, int maintenance = 0, int critical = 0, int unknown = 0)
        {
            for (var i = 0; i < good; i++)
                yield return Good;
            for (var i = 0; i < warning; i++)
                yield return Warning;
            for (var i = 0; i < maintenance; i++)
                yield return Maintenance;
            for (var i = 0; i < critical; i++)
                yield return Critical;
            for (var i = 0; i < unknown; i++)
                yield return Unknown;
        }
    }
}
