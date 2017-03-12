namespace StackExchange.Opserver.Data
{
    /// <summary>
    /// For instances where you want a singleton static instance of a poller. For example a single API endpoint like CloudFlare or pagerduty.
    /// </summary>
    /// <typeparam name="T">The type of node (of which there will be a single instance)</typeparam>
    public abstract class SinglePollNode<T> : PollNode where T : SinglePollNode<T>, new()
    {
#pragma warning disable RCS1158 // Avoid static members in generic types.
        public static T Instance { get; } = new T();
        public static string ShortName { get; } = typeof (T).FullName.Replace("StackExchange.Opserver.Data.", "");
#pragma warning restore RCS1158 // Avoid static members in generic types.

        protected SinglePollNode() : base(typeof(T).FullName.Replace("StackExchange.Opserver.Data.", ""))
        {
            TryAddToGlobalPollers();
        }

        public override string ToString() => ShortName;
    }
}
