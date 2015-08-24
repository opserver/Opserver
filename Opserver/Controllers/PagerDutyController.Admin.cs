using System;
using System.Threading.Tasks;
using System.Web.Mvc;
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

            var newIncident = await PagerDutyApi.Instance.UpdateIncidentStatusAsync(incident, pdUser, newStatus);

            return Json(newIncident?[0]?.Status == newStatus);
        }

        [Route("pagerduty/action/oncall/override")]
        public async Task<ActionResult> PagerDutyActionOnCallOverride(DateTime? start = null, int durationMins = 60)
        {
            var pdUser = CurrentPagerDutyPerson;
            if (pdUser == null) return ContentNotFound("PagerDuty Persoon Not Found for " + Current.User.AccountName);

            var currentPrimarySchedule = PagerDutyApi.Instance.PrimarySchedule;
            if (currentPrimarySchedule == null)
                return ContentNotFound(PagerDutyApi.Instance.Settings.PrimaryScheduleName.IsNullOrEmpty()
                    ? "PagerDuty PrimarySchedule is not defined (\"PrimaryScheduleName\" in config)."
                    : "PagerDuty Schedule '" + PagerDutyApi.Instance.Settings.PrimaryScheduleName + "' not found.");

            start = start ?? DateTime.UtcNow;
            
            await currentPrimarySchedule.SetOverrideAsync(start.Value, start.Value.AddMinutes(durationMins), CurrentPagerDutyPerson);
      
            return Json(true);
        }
    }
}