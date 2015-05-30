using System;
using System.Collections.Generic;
using System.Web.Mvc;
using StackExchange.Opserver.Data.PagerDuty;
using StackExchange.Opserver.Views.PagerDuty;

namespace StackExchange.Opserver.Controllers
{
	public partial class PagerDutyController
    {
        [Route("pagerduty/action/incident/updatestatus")]
        public ActionResult PagerDutyActionIncident(string apiAction, string incident)
        {
            var pdUser = CurrentPagerDutyPerson;
            if (pdUser == null) return ContentNotFound("PagerDuty Persoon Not Found for " + Current.User.AccountName);
            
            var activeIncident = new PagerDutyEditIncident
            {
                Id = incident,
                Status = apiAction
            };
            var data = new PagerDutyIncidentModel
            {
                Incidents = new List<PagerDutyEditIncident> { activeIncident },
                RequesterId = pdUser.Id
            };
            PagerDutyApi.Instance.GetFromPagerDuty("incidents",
                getFromJson: response => response.ToString(), httpMethod: "PUT", data: data);

            PagerDutyApi.Instance.Incidents.Poll(true);

            return Json(true);
        }

        [Route("pagerduty/action/oncall/override")]
        public ActionResult PagerDutyActionOnCallOverride(DateTime? start = null, int durationMins = 60)
        {
            var pdUser = CurrentPagerDutyPerson;
            if (pdUser == null) return ContentNotFound("PagerDuty Persoon Not Found for " + Current.User.AccountName);

            var currentPrimarySchedule = PagerDutyApi.Instance.PrimarySchedule;
            if (currentPrimarySchedule == null)
                return ContentNotFound(PagerDutyApi.Instance.Settings.PrimaryScheduleName.IsNullOrEmpty()
                    ? "PagerDuty PrimarySchedule is not defined (\"PrimaryScheduleName\" in config)."
                    : "PagerDuty Schedule '" + PagerDutyApi.Instance.Settings.PrimaryScheduleName + "' not found.");

            start = start ?? DateTime.UtcNow;
            
            currentPrimarySchedule.SetOverride(start.Value, start.Value.AddMinutes(durationMins), CurrentPagerDutyPerson);
      
            return Json(true);
        }
    }
}