using System.ComponentModel;
using System.Linq;
using System.Web.Mvc;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.Dashboard;

namespace StackExchange.Opserver.Controllers
{
    public partial class DashboardController : StatusController
    {
        protected override ISecurableSection SettingsSection => Current.Settings.Dashboard;

        protected override string TopTab => TopTabs.BuiltIn.Dashboard;

        [Route("dashboard")]
        public ActionResult Dashboard(string filter)
        {
            var vd = new DashboardModel
                {
                    Nodes = DashboardData.AllNodes.Where(n => !Current.Settings.Dashboard.ExcludePatternRegex.IsMatch(n.Name)).ToList(),
                    ErrorMessages = DashboardData.ProviderExceptions,
                    Filter = filter
                };
            return View(Current.IsAjaxRequest ? "Dashboard.Table" : "Dashboard", vd);
        }

        [Route("dashboard/node")]
        public ActionResult SingleNode([DefaultValue(CurrentStatusTypes.Stats)]CurrentStatusTypes view, string node = null)
        {
            var vd = new NodeModel
            {
                CurrentNode = DashboardData.GetNodeByName(node),
                CurrentStatusType = view
            };

            return View("Node",vd);
        }
    }
}