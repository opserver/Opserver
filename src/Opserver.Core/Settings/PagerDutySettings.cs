using System.Collections.Generic;
using Opserver.Data.PagerDuty;

namespace Opserver
{
    public class PagerDutySettings : ModuleSettings
    {
        public override bool Enabled => APIKey.HasValue();
        public override string AdminRole => PagerDutyRoles.Admin;
        public override string ViewRole => PagerDutyRoles.Viewer;

        public string APIKey { get; set; }
        public List<EmailMapping> UserNameMap { get; set; } = new List<EmailMapping>();

        public int OnCallToShow { get; set; } = 2;
        public int DaysToCache { get; set; } = 60;
        public string HeaderTitle { get; set; }
        public string HeaderHtml { get; set; }

        public string PrimaryScheduleName { get; set; }
    }

    public class EmailMapping
    {
        public string OpServerName { get; set; }
        public string EmailUser { get; set; }
    }
}
