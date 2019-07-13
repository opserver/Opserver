using System.Collections.Generic;
using Opserver.Data.PagerDuty;

namespace Opserver.Views.PagerDuty
{
    public class PagerDutyIncidentModel
    {
        public Incident Incident { get; set; }
        public List<LogEntry> Logs { get; set; }
    }
}