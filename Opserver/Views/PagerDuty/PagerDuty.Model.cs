using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using StackExchange.Opserver.Data.PagerDuty;

namespace StackExchange.Opserver.Views.PagerDuty
{
    public class PagerDutyModel
    {
        public PdPerson PrimaryOnCall { get; set; }
        public PdPerson EscalationOnCall { get; set; }
    }
}