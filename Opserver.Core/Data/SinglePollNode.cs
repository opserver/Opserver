namespace StackExchange.Opserver.Data
{
    /// <summary>
    /// For instances where you want a singleton static instance of a poller. For example a single API endpoint like CloudFlare or pagerduty.
    /// </summary>
    public abstract class SinglePollNode<T> : PollNode where T : SinglePollNode<T>, new()
    {
        public static T Instance { get; } = new T();
        public static string ShortName { get; } = typeof (T).FullName.Replace("StackExchange.Opserver.Data.", "");

        protected SinglePollNode() : base(typeof(T).FullName.Replace("StackExchange.Opserver.Data.", ""))
        {
            TryAddToGlobalPollers();
        }

        public override string ToString() => ShortName;
    }
}
