using System.Collections.Generic;
using StackExchange.Opserver.Data.PagerDuty;

namespace StackExchange.Opserver.Views.PagerDuty
{
    public class PagerDutyModel
    {
        public List<OnCall> Schedule { get; set; }
        public int CachedDays { get; set; }

        public List<Incident> AllIncidents { get; set; }
        public PagerDutyPerson CurrentPagerDutyPerson { get; set; }
    }
}