using System.ComponentModel;

namespace StackExchange.Opserver.Views.Dashboard
{
    public enum CurrentStatusTypes
    {
        [Description("None")]
        None = 0,
        Stats = 1,
        Interfaces = 2,
        [Description("VM Info")]
        VMHost = 3,
        [Description("Active SQL")]
        Elastic = 4,
        HAProxy = 5,
        [Description("Active SQL")]
        SQLActive = 6,
        [Description("Top SQL")]
        SQLTop = 7,
        [Description("Redis Info")]
        Redis = 8
    }
}