using System;
using System.Linq;
using System.Web.Mvc;
using StackExchange.Opserver.Data.Elastic;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.Elastic;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.Elastic)]
    public class ElasticController : StatusController
    {
        public override ISecurableSection SettingsSection => Current.Settings.Elastic;

        public override TopTab TopTab => new TopTab("Elastic", nameof(Dashboard), this, 30)
        {
            GetMonitorStatus = () => ElasticCluster.AllClusters.GetWorstStatus()
        };

        [Route("elastic")]
        public ActionResult Dashboard(string cluster, string node)
        {
            var vd = new DashboardModel
            {
                View = DashboardModel.Views.AllClusters,
                WarningsOnly = true
            };
            return View("AllClusters", vd);
        }

        [Route("elastic/cluster")]
        public ActionResult Cluster(string cluster)
        {
            var vd = GetViewData(cluster);
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
            switch (type)
            {
                case "settings":
                    return View("Node.Settings", vd);
                default:
                    return ContentNotFound("Unknown modal view requested");
            }
        }

        [Route("elastic/cluster/modal/{type}")]
        public ActionResult ClusterModal(string cluster, string node, string type)
        {
            var vd = GetViewData(cluster, node);
            switch (type)
            {
                case "indexes":
                    return View("Cluster.Indexes", vd);
                default:
                    return ContentNotFound("Unknown modal view requested");
            }
        }

        [Route("elastic/index/modal/{type}")]
        public ActionResult IndexModal(string cluster, string node, string index, string type, string subtype)
        {
            var vd = GetViewData(cluster, node, index);
            switch (type)
            {
                case "shards":
                    return View("Cluster.Shards", vd);
                default:
                    return ContentNotFound("Unknown modal view requested");
            }
        }

        private static DashboardModel GetViewData(string cluster, string node = null, string index = null)
        {
            // Cluster names are not unique, names + node names should be though
            // If we see too many people with crazy combos, then node GUIDs it is.
            var cc = ElasticCluster.AllClusters.FirstOrDefault(c => string.Equals(c.Name, cluster, StringComparison.InvariantCultureIgnoreCase)
                                             && (node.IsNullOrEmpty() || (c.Nodes.Data?.Get(node) != null)));
            var cn = cc?.Nodes.Data.Get(node);

            return new DashboardModel
            {
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
        public ActionResult Reroute(string type, string index, int shard, string node)
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
