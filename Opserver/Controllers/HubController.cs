using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Opserver.Data.Elastic;
using StackExchange.Opserver.Data.HAProxy;
using StackExchange.Opserver.Data.SQL;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.Hub;

namespace StackExchange.Opserver.Controllers
{
    public partial class HubController : StatusController
    {
        protected override ISecurableSection SettingsSection => Current.Settings.Dashboard;

        protected override string TopTab => TopTabs.BuiltIn.Dashboard;

        [Route("headsup"), AlsoAllow(Roles.InternalRequest)]
        public ActionResult HeadsUp()
        {
            return RedirectToAction("Index");
        }

        [Route("hub"), Route("headsup"), AlsoAllow(Roles.InternalRequest)]
        public ActionResult Index()
        {
            var items = new List<IMonitorStatus>();

            var nodes = DashboardData.AllNodes
                                        .Where(s => !s.IsUnwatched)
                                        .Where(s => s.CPULoad >= 0 || s.MemoryUsed >= 0)
                                        .OrderByWorst(s => s.CPUMonitorStatus())
                                        .ThenByWorst(s => s.MemoryMonitorStatus())
                                        .ThenBy(s => s.Category.Index)
                                        .ThenBy(s => s.PrettyName);
            // Dashboard
            items.AddRange(nodes);
            items.AddRange(SQLCluster.AllClusters.SelectMany(c => c.AvailabilityGroups));
            // TODO: Redis
            items.AddRange(ElasticCluster.AllClusters.OrderByWorst().ThenBy(c => c.Name)); 
            // TODO: Exceptions
            items.AddRange(HAProxyGroup.AllGroups.OrderByWorst().ThenBy(g => g.Name));

            var vd = new HubModel
                {
                    Items = items
                };
            return View(vd);
        }
    }
}