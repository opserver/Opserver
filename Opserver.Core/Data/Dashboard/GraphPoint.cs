namespace StackExchange.Opserver.Data.Dashboard
{
    public interface IGraphPoint
    {
        long DateEpoch { get; }
    }

    public class GraphPoint : IGraphPoint
    {
        /// <summary>
        /// Date represented as a unix epoch
        /// </summary>
        public virtual long DateEpoch { get; set; }
        /// <summary>
        /// Value of the top (or only) point
        /// </summary>
        public virtual double? Value { get; set; }
    }

    public class DoubleGraphPoint : GraphPoint
    {
        /// <summary>
        /// Value of the bottom (or second) point
        /// </summary>
        public virtual double? BottomValue { get; set; }
    }
}
