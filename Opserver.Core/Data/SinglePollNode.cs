namespace StackExchange.Opserver.Data
{
    /// <summary>
    /// For instances where you want a singleton static instance of a poller. For example a single API endpoint like CloudFlare or pagerduty.
    /// </summary>
    public abstract class SinglePollNode<T> : PollNode where T : SinglePollNode<T>, new()
    {
        public static T Instance { get; } = new T();
        private static readonly string _shortName = typeof (T).FullName.Replace("StackExchange.Opserver.Data.", "");

        protected SinglePollNode() : base(typeof(T).FullName.Replace("StackExchange.Opserver.Data.", ""))
        {
            TryAddToGlobalPollers();
        }

        public override string ToString() => _shortName;
    }
}
