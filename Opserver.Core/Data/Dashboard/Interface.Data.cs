namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Interface
    {
        public class InterfaceUtilization : DoubleGraphPoint
        {
            public override long DateEpoch { get; set; }
            public float? InAvgBps { get; internal set; }
            public float? OutAvgBps { get; internal set; }

            public override double? Value => InAvgBps;
            public override double? BottomValue => OutAvgBps;
        }
    }
}