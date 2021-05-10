using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Opserver.Data.Redis;
using Opserver.Helpers;
using Opserver.Views.Redis;

namespace Opserver.Controllers
{
    [OnlyAllow(RedisRoles.Viewer)]
    public partial class RedisController : StatusController<RedisModule>
    {
        public RedisController(RedisModule module, IOptions<OpserverSettings> settings) : base(module, settings) { }

        [DefaultRoute("redis")]
        public ActionResult Dashboard(string node)
        {
            var instances = Module.GetAllInstances(node);
            if (instances.Count == 1 && instances[0] != null)
            {
                // In the 1 case, redirect
                return RedirectToAction(nameof(Instance), new { node });
            }
            var vd = new DashboardModel
            {
                ReplicationGroups = Module.ReplicationGroups,
                Instances = instances.Count > 1 ? instances : Module.Instances,
                View = node.HasValue() ? RedisViews.Server : RedisViews.All,
                CurrentRedisServer = node,
                Refresh = true
            };
            return View("Dashboard.Instances", vd);
        }

        [Route("redis/instance")]
        public ActionResult Instance(string node)
        {
            var instance = Module.GetInstance(node);

            var vd = new DashboardModel
            {
                Instances = Module.Instances,
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
            var instance = Module.GetInstance(host, port);
            var configZip = instance.GetConfigZip();
            return new FileContentResult(configZip, "application/zip");
        }

        [Route("redis/instance/summary/{type}")]
        public ActionResult InstanceSummary(string node, string type)
        {
            var i = Module.GetInstance(node);
            if (i == null) return ContentNotFound("Could not find instance " + node);

            return type switch
            {
                "config" => PartialView("Instance.Config", i),
                "clients" => PartialView("Instance.Clients", i),
                "info" => PartialView("Instance.Info", i),
                "slow-log" => PartialView("Instance.SlowLog", i),
                _ => ContentNotFound("Unknown summary view requested"),
            };
        }

        [Route("redis/analyze/memory")]
        public ActionResult Analysis(string node, int db, bool runOnMaster = false)
        {
            var instance = Module.GetInstance(node);
            if (instance == null)
                return TextPlain("Instance not found");
            var analysis = instance.GetDatabaseMemoryAnalysis(db, runOnMaster);

            return View("Instance.Analysis.Memory", analysis);
        }

        [Route("redis/analyze/memory/clear")]
        public ActionResult ClearAnalysis(string node, int db)
        {
            var instance = Module.GetInstance(node);
            if (instance == null)
                return TextPlain("Instance not found");
            instance.ClearDatabaseMemoryAnalysisCache(db);

            return RedirectToAction(nameof(Analysis), new { node, db });
        }
    }
}
