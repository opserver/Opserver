namespace StackExchange.Opserver
{
    public class BosunSettings : IProviderSettings
    {
        public bool Enabled => Host.HasValue();
        public string Name => "Bosun";

        public string Host { get; set; }

        public void Normalize()
        {
            Host = Host.NormalizeHostOrFQDN();
        }
    }
}
