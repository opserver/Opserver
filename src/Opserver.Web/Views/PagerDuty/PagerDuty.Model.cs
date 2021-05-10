using System.Collections.Generic;
using Opserver.Data.PagerDuty;

namespace Opserver.Views.PagerDuty
{
    public class PagerDutyModel
    {
        public List<OnCall> Schedule { get; set; }
        public int CachedDays { get; set; }

        public List<Incident> AllIncidents { get; set; }
        public PagerDutyPerson CurrentPagerDutyPerson { get; set; }
    }
}