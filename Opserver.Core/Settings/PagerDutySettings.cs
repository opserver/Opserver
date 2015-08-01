using System.Collections.Generic;

namespace StackExchange.Opserver
{
    public class PagerDutySettings : Settings<PagerDutySettings>
    {
        public override bool Enabled => APIKey.HasValue();

        public string APIKey { get; set; }
        public string APIBaseUrl { get; set; }
        public List<EmailMapping> UserNameMap { get; set; } 

        public int OnCallToShow { get; set; }
        public int DaysToCache { get; set; }
        public string HeaderHtml { get; set; }

        public string PrimaryScheduleName { get; set; }

        public PagerDutySettings()
        {
            // Defaults
            OnCallToShow = 2;
            DaysToCache = 60;
            UserNameMap = new List<EmailMapping>();
        }
    }

    public class EmailMapping
    {
        public string OpServerName { get; set; }
        public string EmailUser { get; set; }
    }
}
