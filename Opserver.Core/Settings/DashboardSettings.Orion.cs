namespace StackExchange.Opserver
{
    public class OrionSettings : IProviderSettings
    {
        public bool Enabled => Host.HasValue();
        public string Name => "Orion";

        /// <summary>
        /// The host for Orion, used for generating links
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The connection string for this provider
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Default maximum timeout in milliseconds before giving up on fetching data from this provider
        /// </summary>
        public int QueryTimeoutMs { get; set; }

        public OrionSettings()
        {
            QueryTimeoutMs = 10 * 1000;
        }

        public void Normalize()
        {
            Host = Host.NormalizeHostOrFQDN();
        }
    }
}
