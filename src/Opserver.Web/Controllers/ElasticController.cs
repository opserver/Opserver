using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Opserver.Data.Elastic;
using Opserver.Helpers;
using Opserver.Views.Elastic;

namespace Opserver.Controllers
{
    [OnlyAllow(ElasticRoles.Viewer)]
    public class ElasticController : StatusController<ElasticModule>
    {
        public ElasticController(ElasticModule module, IOptions<OpserverSettings> settings) : base(module, settings) { }

        [DefaultRoute("elastic")]
        public ActionResult Dashboard()
        {
            var vd = new DashboardModel
            {
                Clusters = Module.Clusters,
                View = DashboardModel.Views.AllClusters,
                DisplayMode = DashboardModel.DisplayModes.InterestingOnly
            };
            return View("AllClusters", vd);
        }

        [Route("elastic/cluster")]
        public ActionResult Cluster(string cluster, string node)
        {
            var vd = GetViewData(cluster, node);
            vd.View = DashboardModel.Views.Cluster;
            return View("Cluster", vd);
        }

        [Route("elastic/node")]
        public ActionResult Node(string cluster, string node)
        {
            var vd = GetViewData(cluster, node);
            vd.View = DashboardModel.Views.Node;
            return View("Node", vd);
        }

        [Route("elastic/node/modal/{type}")]
        public ActionResult NodeModal(string cluster, string node, string type)
        {
            var vd = GetViewData(cluster, node);
            return type switch
            {
                "settings" => View("Node.Settings", vd),
                _ => ContentNotFound("Unknown modal view requested"),
            };
        }

        [Route("elastic/cluster/modal/{type}")]
        public ActionResult ClusterModal(string cluster, string node, string type)
        {
            var vd = GetViewData(cluster, node);
            return type switch
            {
                "indexes" => View("Cluster.Indexes", vd),
                _ => ContentNotFound("Unknown modal view requested"),
            };
        }

        [Route("elastic/index/modal/{type}")]
        public ActionResult IndexModal(string cluster, string node, string index, string type)
        {
            var vd = GetViewData(cluster, node, index);
            return type switch
            {
                "shards" => View("Cluster.Shards", vd),
                _ => ContentNotFound("Unknown modal view requested"),
            };
        }

        private DashboardModel GetViewData(string cluster, string node = null, string index = null)
        {
            // Cluster names are not unique, names + node names should be though
            // If we see too many people with crazy combos, then node GUIDs it is.
            var cc = Module.Clusters.Find(
                c => string.Equals(c.Name, cluster, StringComparison.InvariantCultureIgnoreCase)
                  && (node.IsNullOrEmpty() || (c.Nodes.Data?.Get(node) != null)));
            var cn = cc?.Nodes.Data.Get(node);

            return new DashboardModel
            {
                Clusters = Module.Clusters,
                CurrentNodeName = node,
                CurrentClusterName = cc?.Name,
                CurrentIndexName = index,
                CurrentCluster = cc,
                CurrentNode = cn
            };
        }

        [Route("elastic/indexes")]
        public ActionResult Indexes(string cluster, string node, string guid)
        {
            var vd = GetViewData(cluster, node ?? guid);
            vd.View = DashboardModel.Views.Indexes;
            return View("Indexes", vd);
        }

        [Route("elastic/reroute/{type}")]
        public ActionResult Reroute(string type) //, string index, int shard, string node)
        {
            bool result = false;
            switch (type)
            {
                case "assign":
                    result = true;
                    break;
                case "cancel":
                    result = true;
                    break;
            }
            return Json(result);
        }
    }
}
