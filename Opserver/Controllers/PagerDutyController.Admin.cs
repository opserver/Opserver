﻿using System;
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

            var newIncident = await PagerDutyAPI.Instance.UpdateIncidentStatusAsync(incident, pdUser, newStatus).ConfigureAwait(false);
            return Json(newIncident?.Status == newStatus);
        }

        [Route("pagerduty/action/oncall/override")]
        public async Task<ActionResult> PagerDutyActionOnCallOverride(DateTime? start = null, int durationMins = 60)
        {
            var pdUser = CurrentPagerDutyPerson;
            if (pdUser == null) return ContentNotFound("PagerDuty Persoon Not Found for " + Current.User.AccountName);

            var currentPrimarySchedule = PagerDutyAPI.Instance.PrimarySchedule;
            if (currentPrimarySchedule == null)
            {
                return ContentNotFound(PagerDutyAPI.Instance.Settings.PrimaryScheduleName.IsNullOrEmpty()
                   ? "PagerDuty PrimarySchedule is not defined (\"PrimaryScheduleName\" in config)."
                   : "PagerDuty Schedule '" + PagerDutyAPI.Instance.Settings.PrimaryScheduleName + "' not found.");
            }

            start = start ?? DateTime.UtcNow;

            await currentPrimarySchedule.SetOverrideAsync(start.Value, start.Value.AddMinutes(durationMins), CurrentPagerDutyPerson).ConfigureAwait(false);

            return Json(true);
        }
    }
}