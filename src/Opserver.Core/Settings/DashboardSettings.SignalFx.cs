namespace Opserver
{
    public class SignalFxSettings : IProviderSettings
    {
        public bool Enabled => AccessToken.HasValue();
        public string Name => "SignalFx";

        /// <summary>
        /// Realm for SignalFx, e.g. "us1", "eu0"
        /// </summary>
        public string Realm { get; set; }

        /// <summary>
        /// API Key for accessing SignalFx APIs
        /// </summary>
        public string AccessToken { get; set; }

        public void Normalize()
        {
        }
    }
}
