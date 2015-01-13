using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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
        
        [Route("pagerduty")]
        public ActionResult PagerDutyDashboard()
        {
            var tmp = PagerDutyApi.GetInstance();
            var vd = new PagerDutyModel()
            {

                PrimaryOnCall = tmp.PrimaryOnCall.Data,
                EscalationOnCall = tmp.SecondaryOnCall.Data
                
            };
            return View("PagerDuty", vd);
        }
    }
}