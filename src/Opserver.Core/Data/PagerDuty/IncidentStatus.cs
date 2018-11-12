using System.ComponentModel;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public enum IncidentStatus
    {
        // ReSharper disable InconsistentNaming
        [Description("Triggered")]
        triggered = 0,
        [Description("Acknowledged")]
        acknowledged = 1,
        [Description("Resolved")]
        resolved = 2
        // ReSharper restore InconsistentNaming
    }
}
