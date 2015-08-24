using System.ComponentModel;

namespace StackExchange.Opserver.Data.PagerDuty
{
    public enum IncidentStatus
    {
        // ReSharper disable InconsistentNaming
        [Description("Triggered")]
        triggered,
        [Description("Acknowledged")]
        acknowledged,
        [Description("Resolved")]
        resolved
        // ReSharper restore InconsistentNaming
    }
}
