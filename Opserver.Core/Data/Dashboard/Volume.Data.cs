using System;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Volume
    {
        public class VolumeUtilization
        {
            public DateTime DateTime { get; internal set; }

            public Double AvgDiskUsed { get; internal set; }
            public Double MaxDiskUsed { get; internal set; }
            public Double DiskSize { get; internal set; }
            public Single PercentDiskUsed { get; internal set; }
        }
    }
}