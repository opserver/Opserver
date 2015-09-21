using System;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Node
    {
        public class CPUUtilization
        {
            public DateTime DateTime { get; internal set; }

            public short? AvgLoad { get; internal set; }
            public short? MaxLoad { get; internal set; }
        }

        public class MemoryUtilization
        {
            public DateTime DateTime { get; internal set; }

            public float? AvgMemoryUsed { get; internal set; }
            public float? MaxMemoryUsed { get; internal set; }
            public float? TotalMemory { get; internal set; }
        }
    }
}