using System;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Interface
    {
        public class InterfaceUtilization
        {
            public DateTime DateTime { get; internal set; }

            public Int16? AvgLoad { get; internal set; }
            public Int16? MaxLoad { get; internal set; }

            public Single? InMaxBps { get; internal set; }
            public Single? InAvgBps { get; internal set; }

            public Single? OutMaxBps { get; internal set; }
            public Single? OutAvgBps { get; internal set; }
        }
    }
}