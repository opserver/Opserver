using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Opserver.Data.PagerDuty;

namespace StackExchange.Opserver.Controllers
{
    public partial class PagerDutyController
    {
        [Route("pagerduty/action/incident/updatestatus")]
        public async Task<ActionResult> PagerDutyActionIncident(string incident, IncidentStatus newStatus)
        {
            var pdUser = CurrentPagerDutyPerson;
            if (pdUser == null) return ContentNotFound("PagerDuty Person Not Found for " + Current.User.AccountName);

            var newIncident = await Module.API.UpdateIncidentStatusAsync(incident, pdUser, newStatus).ConfigureAwait(false);
            return Json(newIncident?.Status == newStatus);
        }

        [Route("pagerduty/action/oncall/override")]
        public async Task<ActionResult> PagerDutyActionOnCallOverride(DateTime? start = null, int durationMins = 60)
        {
            var pdUser = CurrentPagerDutyPerson;
            if (pdUser == null) return ContentNotFound("PagerDuty Persoon Not Found for " + Current.User.AccountName);

            var currentPrimarySchedule = Module.API.PrimarySchedule;
            if (currentPrimarySchedule == null)
            {
                return ContentNotFound(Module.Settings.PrimaryScheduleName.IsNullOrEmpty()
                   ? "PagerDuty PrimarySchedule is not defined (\"PrimaryScheduleName\" in config)."
                   : "PagerDuty Schedule '" + Module.Settings.PrimaryScheduleName + "' not found.");
            }

            start = start ?? DateTime.UtcNow;

            await Module.API.SetOverrideAsync(currentPrimarySchedule, start.Value, start.Value.AddMinutes(durationMins), CurrentPagerDutyPerson).ConfigureAwait(false);

            return Json(true);
        }
    }
}
