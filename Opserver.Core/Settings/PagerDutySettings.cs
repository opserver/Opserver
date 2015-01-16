namespace StackExchange.Opserver
{
    public class PagerDutySettings : Settings<PagerDutySettings>
    {
        public override bool Enabled
        {
            get { return APIKey.HasValue(); }
        }

        public string APIKey { get; set; }
        public string APIBaseUrl { get; set; }

        public int OnCalllToShow { get; set; }
        public int DaysToCache { get; set; }

        public PagerDutySettings()
        {
            // Defaults
            OnCalllToShow = 2;
            DaysToCache = 60;
        }
    }
}
