namespace StackExchange.Opserver
{
    public class BosunSettings : IProviderSettings
    {
        public bool Enabled => Host.HasValue();
        public string Name => "Bosun";

        /// <summary>
        /// Endpoint for Bosun APIs, e.g. "bosun.mydomain.com", also accepts http or https:// forms
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// API Key for accessing Bosun APIs
        /// </summary>
        public string APIKey { get; set; }

        public void Normalize()
        {
            Host = Host.NormalizeHostOrFQDN();
        }
    }
}
