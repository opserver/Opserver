namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Volume
    {
        public class VolumeUtilization : GraphPoint
        {
            public override long DateEpoch { get; set; }
            public double AvgDiskUsed { get; internal set; }

            public override double? Value => AvgDiskUsed;
        }
    }
}