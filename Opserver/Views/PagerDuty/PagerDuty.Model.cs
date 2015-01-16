using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using StackExchange.Opserver.Data.PagerDuty;

namespace StackExchange.Opserver.Views.PagerDuty
{
    public class PagerDutyModel
    {
        public PagerDutyPerson PrimaryOnCall { get; set; }
        public PagerDutyPerson EscalationOnCall { get; set; }
        public List<PagerDutyIncident> AllIncidents { get; set; }
    }
}