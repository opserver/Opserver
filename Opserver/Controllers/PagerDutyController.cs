using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StackExchange.Opserver.Data.PagerDuty;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.PagerDuty;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.PagerDuty)]
    public partial class PagerDutyController : StatusController
    {
        protected override ISecurableSection SettingsSection
        {
            get { return Current.Settings.PagerDuty; }
        }
        protected override string TopTab
        {
            get { return TopTabs.BuiltIn.PagerDuty; }
        }

        public PagerDutyPerson CurrentPagerDutyPerson
        {
            get
            {
                var pdMap = PagerDutyApi.Instance.Settings.UserNameMap.FirstOrDefault(
                    un => un.OpServerName == Current.User.AccountName);
                return pdMap != null
                    ? PagerDutyApi.Instance.AllUsers.Data.Find(u => u.EmailUserName == pdMap.EmailUser)
                    : null;
            }
        }
        
        [Route("pagerduty")]
        public ActionResult PagerDutyDashboard()
        {
            var i = PagerDutyApi.Instance;
            i.WaitForFirstPoll(5000);
            
            var vd = new PagerDutyModel
            {
                Schedule = i.GetSchedule(),
                OnCallToShow = i.Settings.OnCallToShow,
                CachedDays = i.Settings.DaysToCache,
                AllIncidents = i.Incidents.SafeData(true),
                CurrentPagerDutyPerson = CurrentPagerDutyPerson
            };
            return View("PagerDuty", vd);
        }

        [Route("pagerduty/incident/detail/{id}")]
        public ActionResult PagerDutyIncidentDetail(int id)
        {
            var incident = PagerDutyApi.Instance.Incidents.Data.First(i => i.Number == id);
            return View("PagerDuty.Incident", incident);
        }

        [Route("pagerduty/escalation/full")]
        public ActionResult PagerDutyFullEscalation()
        {
            return View("PagerDuty.EscFull", PagerDutyApi.Instance.GetSchedule());
        }

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
                Incidents = new List<PagerDutyEditIncident> {activeIncident},
                RequesterId = pdUser.Id
            };
            PagerDutyApi.Instance.GetFromPagerDuty("incidents",
                getFromJson: response => response.ToString(), httpMethod: "PUT", data: data);

            PagerDutyApi.Instance.Incidents.Poll(true);

            return Json(true);
        }

        [Route("pagerduty/action/oncall/override")]
        public ActionResult PagerDutyActionOnCallOverride(DateTime? start = null, int durationHours = 1, int durationMins = 0)
        {
            var pdUser = CurrentPagerDutyPerson;
            if (pdUser == null) return ContentNotFound("PagerDuty Persoon Not Found for " + Current.User.AccountName);

            var overrideDuration = new TimeSpan(durationHours, durationMins, 0);
            var currentPrimarySchedule = PagerDutyApi.Instance.PrimaryScheduleId;
            if (start == null)
            {
                start = DateTime.UtcNow; 
            }

            var overrideData = new PagerDutyScheduleOverride()
            {
                StartTime = start,
                EndTime = start.Value.Add(overrideDuration),
                UserID = CurrentPagerDutyPerson.Id
            };

            PagerDutyApi.Instance.GetFromPagerDuty("schedules/" + currentPrimarySchedule + "/overrides",
                getFromJson: response => response.ToString(), httpMethod: "POST", data: overrideData);

            PagerDutyApi.Instance.Poll(true);

            return Json(true);
        }
    }
}