using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using StackExchange.Opserver.Data.PagerDuty;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.PagerDuty;
using System;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.PagerDuty)]
    public partial class PagerDutyController : StatusController
    {
        public override ISecurableSection SettingsSection => Current.Settings.PagerDuty;

        public override TopTab TopTab => new TopTab("PagerDuty", nameof(Dashboard), this, 45)
        {
            GetMonitorStatus = () => PagerDutyAPI.Instance.MonitorStatus
        };

        public PagerDutyPerson CurrentPagerDutyPerson
        {
            get
            {
                var allUsers = PagerDutyAPI.Instance.AllUsers.SafeData(true);
                var pdMap = PagerDutyAPI.Instance.Settings.UserNameMap.FirstOrDefault(
                    un => un.OpServerName == Current.User.AccountName);
                return pdMap != null
                    ? allUsers.Find(u => u.EmailUserName == pdMap.EmailUser)
                    : allUsers.FirstOrDefault(u => string.Equals(u.EmailUserName, Current.User.AccountName, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Route("pagerduty")]
        public ActionResult Dashboard()
        {
            var i = PagerDutyAPI.Instance;
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
        public async Task<ActionResult> IncidentDetail(int id)
        {
            var incident = PagerDutyAPI.Instance.Incidents.Data.First(i => i.Number == id);
            var vd = new PagerDutyIncidentModel
            {
                Incident = incident,
                Logs = await incident.Logs
            };

            return View("PagerDuty.Incident", vd);
        }

        [Route("pagerduty/escalation/full")]
        public ActionResult FullEscalation()
        {
            return View("PagerDuty.EscFull", PagerDutyAPI.Instance.GetSchedule());
        }
    }
}