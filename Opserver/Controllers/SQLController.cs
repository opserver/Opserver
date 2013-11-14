using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using StackExchange.Opserver.Data.SQL;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.SQL;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.SQL)]
    public partial class SQLController : StatusController
    {
        protected override ISecurableSection SettingsSection
        {
            get { return Current.Settings.SQL; }
        }

        [Route("sql")]
        public ActionResult Dashboard()
        {
            return Redirect("/sql/servers");
        }

        [Route("sql/servers")]
        public ActionResult Servers(string cluster, string node, string ag, bool ajax = false, bool detailOnly = false)
        {
            var vd = new DashboardModel
                {
                    StandaloneInstances = SQLInstance.AllStandalone,
                    Clusters = SQLCluster.AllClusters,
                    Refresh = node.HasValue() ? 10 : 5,
                    View = DashboardModel.Views.Servers
                };

            if (cluster.HasValue())
                vd.CurrentCluster = vd.Clusters.FirstOrDefault(c => string.Equals(c.Name, cluster, StringComparison.OrdinalIgnoreCase));
            if (vd.CurrentCluster != null)
                vd.AvailabilityGroups = vd.CurrentCluster.GetAvailabilityGroups(node, ag).ToList();

            if (detailOnly && vd.CurrentCluster != null)
                return View("Servers.ClusterDetail", vd);

            return View(ajax ? "Servers" : "Dashboard", vd);
        }


        [Route("sql/instance")]
        public ActionResult Instance(string node, bool ajax = false)
        {
            var instance = SQLInstance.Get(node);
            if (instance == null && ajax)
                return ContentNotFound("Instance " + node + " was not found.");

            var vd = new DashboardModel
            {
                StandaloneInstances = SQLInstance.AllStandalone,
                Refresh = node.HasValue() ? 10 : 5,
                View = DashboardModel.Views.Instance,
                CurrentInstance = instance
            };
            return View(ajax ? "Instance" : "Dashboard", vd);
        }

        [Route("sql/instance/summary/{type}")]
        public ActionResult InstanceSummary(string node, string type)
        {
            var i = SQLInstance.Get(node);
            if (i == null) return ContentNotFound("Could not find instance " + node);

            switch (type)
            {
                case "connections":
                    return View("Instance.Connections", i);
                case "errors":
                    return View("Instance.Errors", i);
                case "memory":
                    return View("Instance.Memory", i);
                case "jobs":
                    return View("Instance.Jobs", i);
                default:
                    return ContentNotFound("Unknown summary view requested");
            }
        }

        [OutputCache(Duration = 5 * 1, VaryByParam = "node;sort;options", VaryByContentEncoding = "gzip;deflate")]
        [Route("sql/top")]
        public ActionResult Top(string node, SQLInstance.TopSearchOptions options, bool? detailed = false)
        {
            options.SetDefaults();

            var vd = new DashboardModel
                {
                    View = DashboardModel.Views.Top,
                    Detailed = detailed.GetValueOrDefault(),
                    CurrentInstance = SQLInstance.Get(node),
                    TopSearchOptions = options
                };
            return View("Dashboard", vd);
        }

        [Route("sql/top/detail")]
        public ActionResult TopDetail(string node, string handle, int? offset = null)
        {
            var planHandle = HttpServerUtility.UrlTokenDecode(handle);
            var instance = SQLInstance.Get(node);
            if (instance == null) return ContentNotFound("Server " + node + " not found.");

            var vd = new OpsTopDetailModel
                {
                    Instance = instance,
                    Op = instance.GetTopOperation(planHandle, offset).Data
                };
            return View("Operations.Top.Detail", vd);
        }

        [Route("sql/top/plan")]
        public ActionResult TopPlan(string node, string handle)
        {
            var planHandle = HttpServerUtility.UrlTokenDecode(handle);
            var instance = SQLInstance.Get(node);
            var op = instance.GetTopOperation(planHandle);
            if (op.Data == null) return ContentNotFound("Plan was not found.");

            var ms = new MemoryStream(Encoding.UTF8.GetBytes(op.Data.QueryPlan));

            return File(ms, "text/xml", string.Format("QueryPlan-{0}.sqlplan", Math.Abs(handle.GetHashCode())));
        }

        [Route("sql/active")]
        public ActionResult Active(string node, SQLInstance.ActiveSearchOptions options,
                                   SQLInstance.ActiveSearchOptions.ShowSleepingSessionOptions? sleeping = null,
                                   bool? system = false,
                                   bool? details = false)
        {
            if (sleeping.HasValue) options.IncludeSleepingSessions = sleeping.Value;
            if (system.HasValue) options.IncludeSystemSessions = system.Value;
            if (details.HasValue) options.GetAdditionalInfo = details.Value;

            var vd = new DashboardModel
                {
                    View = DashboardModel.Views.Active,
                    CurrentInstance = SQLInstance.Get(node),
                    ActiveSearchOptions = options
                };
            return View("Dashboard", vd);
        }

        [Route("sql/connections")]
        public ActionResult Connections(string node, bool refresh = false)
        {
            var instance = SQLInstance.Get(node);
            if (refresh && instance != null)
            {
                instance.Connections.Purge();
            }

            var vd = new DashboardModel
            {
                View = DashboardModel.Views.Connections,
                CurrentInstance = instance
            };
            return View("Dashboard", vd);
        }

        [Route("sql/databases")]
        public ActionResult Databases(string node)
        {
            var vd = new DashboardModel
            {
                View = DashboardModel.Views.Databases,
                CurrentInstance = SQLInstance.Get(node)
            };
            return View("Dashboard", vd);
        }

        [Route("sql/db/{database}/{view}")]
        public ActionResult DatabaseDetail(string node, string database, string view)
        {
            var vd = new DatabasesModel
            {
                Instance = SQLInstance.Get(node),
                Database = database
            };
            switch (view)
            {
                case "tables":
                    vd.View = DatabasesModel.Views.Tables;
                    return View("Databases.Modal.Tables", vd);
                case "views":
                    vd.View = DatabasesModel.Views.Views;
                    return View("Databases.Modal.Views", vd);
            }
            return View("Databases.Modal.Tables", vd);
        }

        [Route("sql/databases/tables")]
        public ActionResult DatabaseTables(string node, string database)
        {
            var vd = new DatabasesModel
            {
                Instance = SQLInstance.Get(node),
                Database = database
            };
            return View("Databases.Modal.Tables", vd);
        }
    }
}
