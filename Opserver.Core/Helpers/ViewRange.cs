using System.ComponentModel;

namespace StackExchange.Opserver.Helpers
{
    public enum ViewRange
    {
        [Description("Summary")]
        Summary,
        [Description("Today")]
        Day,
        [Description("This Week")]
        Week,
        [Description("This Month")]
        Month,
        [Description("This Year")]
        Year,
        [Description("Custom")]
        Custom
    }
}