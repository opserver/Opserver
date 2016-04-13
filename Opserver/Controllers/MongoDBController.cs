using System.Web.Mvc;
using StackExchange.Opserver.Data.MongoDB;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.MongoDB;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.MongoDB)]
    public partial class MongoDBController : StatusController
    {
        public override ISecurableSection SettingsSection => Current.Settings.MongoDB;

        public override TopTab TopTab => new TopTab("MongoDB", nameof(Dashboard), this, 35)
        {
            GetMonitorStatus = () => MongoDBInstance.AllInstances.GetWorstStatus()
        };

        [Route("mongodb")]
        public ActionResult Dashboard(string node)
        {
            var instance = MongoDBInstance.GetInstance(node);
            if (instance != null)
                return RedirectToAction(nameof(Instance), new { node });

            var vd = new DashboardModel
            {
                Instances = MongoDBInstance.AllInstances,
                View = node.HasValue() ? MongoDBViews.Server : MongoDBViews.All,
                CurrentMongoDBServer = node,
                Refresh = true
            };
            return View("Dashboard.Instances", vd);
        }

        [Route("mongodb/instance")]
        public ActionResult Instance(string node)
        {
            var instance = MongoDBInstance.GetInstance(node);

            var vd = new DashboardModel
            {
                Instances = MongoDBInstance.AllInstances,
                CurrentInstance = instance,
                View = MongoDBViews.Instance,
                CurrentMongoDBServer = node,
                Refresh = true
            };
            return View(vd);
        }


        //[Route("mongodb/instance/get-config/{host}-{port}-config.zip")]
        //public ActionResult DownloadConfiguration(string host, int port)
        //{
        //    var instance = MongoDBInstance.GetInstance(host, port);
        //    var configZip = instance.GetConfigZip();
        //    return new FileContentResult(configZip, "application/zip");
        //}

        [Route("mongodb/instance/summary/{type}")]
        public ActionResult InstanceSummary(string node, string type)
        {
            var i = MongoDBInstance.GetInstance(node);
            if (i == null) return ContentNotFound("Could not find instance " + node);

            switch (type)
            {
                case "clients":
                    return View("Instance.Clients", i);
                case "info":
                    return View("Instance.Info", i);
                default:
                    return ContentNotFound("Unknown summary view requested");
            }
        }

        [Route("mongodb/analyze/memory")]
        public ActionResult Analysis(string node, string db, bool runOnMaster = false)
        {
            var instance = MongoDBInstance.GetInstance(node);
            if (instance == null)
                return TextPlain("Instance not found");
            var analysis = instance.GetDatabaseMemoryAnalysis(db, runOnMaster);

            return View("Instance.Analysis.Memory", analysis);
        }

        [Route("mongodb/analyze/memory/clear")]
        public ActionResult ClearAnalysis(string node, string db)
        {
            var instance = MongoDBInstance.GetInstance(node);
            if (instance == null)
                return TextPlain("Instance not found");
            instance.ClearDatabaseMemoryAnalysisCache(db);

            return RedirectToAction(nameof(Analysis), new { node, db });
        }
    }
}
