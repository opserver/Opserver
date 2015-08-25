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
        protected override ISecurableSection SettingsSection => Current.Settings.SQL;

        protected override string TopTab => TopTabs.BuiltIn.SQL;

        [Route("sql")]
        public ActionResult Dashboard()
        {
            return Redirect("/sql/servers");
        }

        [Route("sql/servers")]
        public ActionResult Servers(string cluster, string node, string ag, bool detailOnly = false)
        {
            var vd = new ServersModel
                {
                    StandaloneInstances = SQLInstance.AllStandalone,
                    Clusters = SQLCluster.AllClusters,
                    Refresh = node.HasValue() ? 10 : 5
                };

            if (cluster.HasValue())
                vd.CurrentCluster = vd.Clusters.FirstOrDefault(c => string.Equals(c.Name, cluster, StringComparison.OrdinalIgnoreCase));
            if (vd.CurrentCluster != null)
                vd.AvailabilityGroups = vd.CurrentCluster.GetAvailabilityGroups(node, ag).ToList();

            if (detailOnly && vd.CurrentCluster != null)
                return View("Servers.ClusterDetail", vd);

            return View("Servers", vd);
        }

        [Route("sql/instance")]
        public ActionResult Instance(string node)
        {
            var i = SQLInstance.Get(node);

            var vd = new InstanceModel
            {
                View = SQLViews.Instance,
                Refresh = node.HasValue() ? 10 : 5,
                CurrentInstance = i
            };
            return View("Instance", vd);
        }

        [Route("sql/instance/summary/{type}")]
        public ActionResult InstanceSummary(string node, string type)
        {
            var i = SQLInstance.Get(node);
            if (i == null) return NoInstanceRedirect(node);

            switch (type)
            {
                case "configuration":
                    return View("Instance.Configuration", i);
                case "connections":
                    return View("Instance.Connections", i);
                case "errors":
                    return View("Instance.Errors", i);
                case "memory":
                    return View("Instance.Memory", i);
                case "jobs":
                    return View("Instance.Jobs", i);
                case "db-files":
                    return View("Instance.DBFiles", i);
                default:
                    return ContentNotFound("Unknown summary view requested");
            }
        }

        [OutputCache(Duration = 5 * 1, VaryByParam = "node;sort;options", VaryByContentEncoding = "gzip;deflate")]
        [Route("sql/top")]
        public ActionResult Top(string node, SQLInstance.TopSearchOptions options, bool? detailed = false)
        {
            var i = SQLInstance.Get(node);
            options.SetDefaults();


            var vd = new OperationsTopModel
            {
                View = SQLViews.Top,
                Detailed = detailed.GetValueOrDefault(),
                CurrentInstance = i,
                TopSearchOptions = options
            };

            if (i != null)
            {
                var cache = i.GetTopOperations(options);
                vd.TopOperations = cache.SafeData(true);
                vd.ErrorMessage = cache.ErrorMessage;
            }

            return View("Operations.Top", vd);
        }

        [Route("sql/top/detail")]
        public ActionResult TopDetail(string node, string handle, int? offset = null)
        {
            var planHandle = HttpServerUtility.UrlTokenDecode(handle);

            var i = SQLInstance.Get(node);

            var vd = new OperationsTopDetailModel
            {
                Instance = i,
                Op = i.GetTopOperation(planHandle, offset).Data
            };
            return View("Operations.Top.Detail", vd);
        }

        [Route("sql/top/plan")]
        public ActionResult TopPlan(string node, string handle)
        {
            var planHandle = HttpServerUtility.UrlTokenDecode(handle);
            var i = SQLInstance.Get(node);
            var op = i.GetTopOperation(planHandle);
            if (op.Data == null) return ContentNotFound("Plan was not found.");

            var ms = new MemoryStream(Encoding.UTF8.GetBytes(op.Data.QueryPlan));

            return File(ms, "text/xml", $"QueryPlan-{Math.Abs(handle.GetHashCode())}.sqlplan");
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

            var i = SQLInstance.Get(node);

            var vd = new OperationsActiveModel
            {
                View = SQLViews.Active,
                CurrentInstance = i,
                ActiveSearchOptions = options
            };
            return View("Operations.Active", vd);
        }

        [Route("sql/connections")]
        public ActionResult Connections(string node)
        {
            var i = SQLInstance.Get(node);

            var vd = new DashboardModel
            {
                View = SQLViews.Connections,
                CurrentInstance = i
            };
            return View(vd);
        }

        [Route("sql/databases")]
        public ActionResult Databases(string node)
        {
            var i = SQLInstance.Get(node);

            var vd = new DashboardModel
            {
                View = SQLViews.Databases,
                CurrentInstance = i,
                Refresh = 10*60
            };
            return View(vd);
        }

        [Route("sql/db/{database}/{view}")]
        [Route("sql/db/{database}/{view}/{objectName}")]
        public ActionResult DatabaseDetail(string node, string database, string view, string objectName)
        {
            var i = SQLInstance.Get(node);

            var vd = new DatabasesModel
            {
                Instance = i,
                Database = database,
                ObjectName = objectName
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
            var i = SQLInstance.Get(node);

            var vd = new DatabasesModel
            {
                Instance = i,
                Database = database
            };
            return View("Databases.Modal.Tables", vd);
        }
        
        private ActionResult NoInstanceRedirect(string node)
        {
            if (Current.IsAjaxRequest)
                return ContentNotFound("Instance " + node + " was not found.");
            return View("Instance.Selector", new DashboardModel());
        }
    }
}
