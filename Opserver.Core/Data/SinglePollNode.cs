namespace StackExchange.Opserver.Data
{
    /// <summary>
    /// For instances where you want a singleton static instance of a poller. For example a single API endpoint like CloudFlare or pagerduty.
    /// </summary>
    public abstract class SinglePollNode<T> : PollNode where T : PollNode, new()
    {
        protected static volatile T _instance;

        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;

                lock (typeof (T))
                {
                    if (_instance != null) return _instance;
                    _instance = new T();
                    _instance.TryAddToGlobalPollers();
                    return _instance;
                }
            }
        }

        private static readonly string _shortName = typeof (T).FullName.Replace("StackExchange.Opserver.Data.", "");

        protected SinglePollNode() : base(_shortName)
        {
            TryAddToGlobalPollers();
        }

        public override string ToString()
        {
            return _shortName;
        }
    }
}
