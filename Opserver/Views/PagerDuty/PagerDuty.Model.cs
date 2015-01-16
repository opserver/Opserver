using System.Collections.Generic;
using StackExchange.Opserver.Data.PagerDuty;

namespace StackExchange.Opserver.Views.PagerDuty
{
    public class PagerDutyModel
    {
        public List<PagerDutyPerson> AllOnCall { get; set; }
        public int OnCallToShow { get; set; }

        public List<PagerDutyIncident> AllIncidents { get; set; }
    }
}