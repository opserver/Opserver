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
        public override ISecurableModule SettingsModule => Current.Settings.PagerDuty;

        public override TopTab TopTab => new TopTab("PagerDuty", nameof(Dashboard), this, 45)
        {
            GetMonitorStatus = () => PagerDutyAPI.Instance.MonitorStatus
        };

        public PagerDutyPerson CurrentPagerDutyPerson
        {
            get
            {
                var currentAccount = Current.User.AccountName;
                var allUsers = PagerDutyAPI.Instance.AllUsers.SafeData(true);
                var pdMap = PagerDutyAPI.Instance.Settings.UserNameMap.Find(un => un.OpServerName == currentAccount);
                return pdMap != null
                    ? allUsers.Find(u => u.EmailUserName == pdMap.EmailUser)
                    : allUsers.Find(u => string.Equals(u.EmailUserName, currentAccount, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Route("pagerduty")]
        public async Task<ActionResult> Dashboard()
        {
            var i = PagerDutyAPI.Instance;
            await i.PollAsync().ConfigureAwait(false);

            var vd = new PagerDutyModel
            {
                Schedule = i.GetOnCall(),
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
            return View("PagerDuty.EscFull", PagerDutyAPI.Instance.OnCallInfo.Data);
        }
    }
}