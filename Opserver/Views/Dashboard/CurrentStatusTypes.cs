﻿using System.ComponentModel;

namespace StackExchange.Opserver.Views.Dashboard
{
    public enum CurrentStatusTypes
    {
        [Description("None")]
        None = 0,
        [Description("Stats")]
        Stats = 1,
        [Description("Interfaces")]
        Interfaces = 2,
        [Description("VM Info")]
        VMHost = 3,
        [Description("Elastic")]
        Elastic = 4,
        [Description("HAProxy")]
        HAProxy = 5,
        [Description("SQL Instance")]
        SQLInstance = 6,
        [Description("Active SQL")]
        SQLActive = 7,
        [Description("Top SQL")]
        SQLTop = 8,
        [Description("Redis Info")]
        Redis = 9
    }
}