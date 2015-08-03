using System.Web.Mvc;
using StackExchange.Opserver.Data.Redis;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.Redis;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.Redis)]
    public partial class RedisController : StatusController
    {
        protected override ISecurableSection SettingsSection => Current.Settings.Redis;

        protected override string TopTab => TopTabs.BuiltIn.Redis;

        [Route("redis")]
        public ActionResult Dashboard(string node)
        {
            var instance = RedisInstance.GetInstance(node);
            if (instance != null)
                return RedirectToAction("Instance", new {node});

            var vd = new DashboardModel
            {
                Instances = RedisInstance.AllInstances,
                View = node.HasValue() ? RedisViews.Server : RedisViews.All,
                CurrentRedisServer = node,
                Refresh = true
            };
            return View("AllServers", vd);
        }

        [Route("redis/server")]
        public ActionResult ServerView(string node)
        {
            if (node == null)
                return RedirectToAction("Dashboard");

            var vd = new DashboardModel
            {
                Instances = RedisInstance.AllInstances,
                View = node.HasValue() ? RedisViews.Server : RedisViews.All,
                CurrentRedisServer = node,
                Refresh = true
            };
            return View("Server", vd);
        }

        [Route("redis/instance")]
        public ActionResult Instance(string node)
        {
            var instance = RedisInstance.GetInstance(node);

            var vd = new DashboardModel
            {
                Instances = RedisInstance.AllInstances,
                CurrentInstance = instance,
                View = RedisViews.Instance,
                CurrentRedisServer = node,
                Refresh = true
            };
            return View(vd);
        }


        [Route("redis/instance/get-config/{host}-{port}-config.zip")]
        public ActionResult DownloadConfiguration(string host, int port)
        {
            var instance = RedisInstance.GetInstance(host, port);
            var configZip = instance.GetConfigZip();
            return new FileContentResult(configZip, "application/zip");
        }

        [Route("redis/instance/summary/{type}")]
        public ActionResult InstanceSummary(string node, string type)
        {
            var i = RedisInstance.GetInstance(node);
            if (i == null) return ContentNotFound("Could not find instance " + node);

            switch (type)
            {
                case "config":
                    return View("Instance.Config", i);
                case "clients":
                    return View("Instance.Clients", i);
                case "slow-log":
                    return View("Instance.SlowLog", i);
                default:
                    return ContentNotFound("Unknown summary view requested");
            }
        }

        [Route("redis/analyze/memory")]
        public ActionResult Analysis(string node, int db, bool runOnMaster = false)
        {
            var instance = RedisInstance.GetInstance(node);
            if (instance == null)
                return TextPlain("Instance not found");
            var analysis = instance.GetDatabaseMemoryAnalysis(db, runOnMaster);

            return View("Server.Analysis.Memory", analysis);
        }

        [Route("redis/analyze/memory/clear")]
        public ActionResult ClearAnalysis(string node, int db)
        {
            var instance = RedisInstance.GetInstance(node);
            if (instance == null)
                return TextPlain("Instance not found");
            instance.ClearDatabaseMemoryAnalysisCache(db);

            return RedirectToAction("Analysis", new { node, db });
        }
    }
}
