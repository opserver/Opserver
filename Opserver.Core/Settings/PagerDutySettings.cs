using System.Collections.Generic;


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
        public List<EmailMapping> UserNameMap { get; set; } 

        public int OnCallToShow { get; set; }
        public int DaysToCache { get; set; }

        public PagerDutySettings()
        {
            // Defaults
            OnCallToShow = 2;
            DaysToCache = 60;
        }


    }

    public class EmailMapping
    {
        public string OpServerName { get; set; }
        public string EmailUser { get; set; }
    }
    
}
