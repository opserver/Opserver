namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Node
    {
        public class CPUUtilization : GraphPoint
        {
            public short? AvgLoad { get; internal set; }
            public override double? Value => AvgLoad;
        }

        public class MemoryUtilization : GraphPoint
        {
            public float? AvgMemoryUsed { get; internal set; }
            public override double? Value => AvgMemoryUsed;
        }
    }
}