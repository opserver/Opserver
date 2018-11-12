using System.Collections.Generic;
using StackExchange.Opserver.Data.PagerDuty;

namespace StackExchange.Opserver.Views.PagerDuty
{
    public class PagerDutyIncidentModel
    {
        public Incident Incident { get; set; }
        public List<LogEntry> Logs { get; set; }
    }
}