using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StackExchange.Opserver.Data.Pagerduty;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.PagerDuty)]
    public partial class PagerDutyController : StatusController
    {
        protected override ISecurableSection SettingsSection
        {
            get { return Current.Settings.Pagerduty; }
        }
        [Route("pagerduty")]
        public ActionResult PagerDutyDashboard()
        {
            return View("PagerDuty");
        }
    }
}