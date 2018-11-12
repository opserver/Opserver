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
        public override ISecurableModule SettingsModule => Current.Settings.Redis;

        public override TopTab TopTab => new TopTab("Redis", nameof(Dashboard), this, 20)
        {
            GetMonitorStatus = () => RedisModule.Instances.GetWorstStatus()
        };

        [Route("redis")]
        public ActionResult Dashboard(string node)
        {
            var instances = RedisInstance.GetAll(node);
            if (instances.Count == 1 && instances[0] != null)
            {
                // In the 1 case, redirect
                return RedirectToAction(nameof(Instance), new { node });
            }
            var vd = new DashboardModel
            {
                Instances = instances.Count > 1 ? instances : RedisModule.Instances,
                View = node.HasValue() ? RedisViews.Server : RedisViews.All,
                CurrentRedisServer = node,
                Refresh = true
            };
            return View("Dashboard.Instances", vd);
        }

        [Route("redis/instance")]
        public ActionResult Instance(string node)
        {
            var instance = RedisInstance.Get(node);

            var vd = new DashboardModel
            {
                Instances = RedisModule.Instances,
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
            var instance = RedisInstance.Get(host, port);
            var configZip = instance.GetConfigZip();
            return new FileContentResult(configZip, "application/zip");
        }

        [Route("redis/instance/summary/{type}")]
        public ActionResult InstanceSummary(string node, string type)
        {
            var i = RedisInstance.Get(node);
            if (i == null) return ContentNotFound("Could not find instance " + node);

            switch (type)
            {
                case "config":
                    return View("Instance.Config", i);
                case "clients":
                    return View("Instance.Clients", i);
                case "info":
                    return View("Instance.Info", i);
                case "slow-log":
                    return View("Instance.SlowLog", i);
                default:
                    return ContentNotFound("Unknown summary view requested");
            }
        }

        [Route("redis/analyze/memory")]
        public ActionResult Analysis(string node, int db, bool runOnMaster = false)
        {
            var instance = RedisInstance.Get(node);
            if (instance == null)
                return TextPlain("Instance not found");
            var analysis = instance.GetDatabaseMemoryAnalysis(db, runOnMaster);

            return View("Instance.Analysis.Memory", analysis);
        }

        [Route("redis/analyze/memory/clear")]
        public ActionResult ClearAnalysis(string node, int db)
        {
            var instance = RedisInstance.Get(node);
            if (instance == null)
                return TextPlain("Instance not found");
            instance.ClearDatabaseMemoryAnalysisCache(db);

            return RedirectToAction(nameof(Analysis), new { node, db });
        }
    }
}
