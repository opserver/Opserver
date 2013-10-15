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
    public partial class ElasticController : StatusController
    {
        protected override ISecurableSection SettingsSection
        {
            get { return Current.Settings.Elastic; }
        }

        [Route("elastic")]
        public ActionResult Dashboard(string cluster, string node, bool ajax = false)
        {
            var vd = new DashboardModel
            {
                Clusters = ElasticCluster.AllClusters,
                Refresh = true,
                View = DashboardModel.Views.Cluster,
                WarningsOnly = true
            };
            return View(ajax ? "Dashboard.Cluster" : "Dashboard", vd);
        }

        [Route("elastic/node")]
        public ActionResult Node(string cluster, string node, DashboardModel.Popups popup = DashboardModel.Popups.None, bool ajax = false)
        {
            var cn = GetNode(cluster, node);
            var vd = new DashboardModel
                {
                    Clusters = ElasticCluster.AllClusters,
                    Refresh = true,
                    View = DashboardModel.Views.Node,
                    Current = cn,
                    Popup = popup
                };
            return View(ajax ? "Node" : "Dashboard", vd);
        }


        [Route("elastic/node/summary/{type}")]
        public ActionResult NodeSummary(string cluster, string node, string type)
        {
            var vd = GetNode(cluster, node);

            switch (type)
            {
                case "settings":
                    return View("Node.Settings", vd);
                case "indices":
                    return View("Indices", vd);
                default:
                    return ContentNotFound("Unknown summary view requested");
            }
        }

        [Route("elastic/index/summary/{type}")]
        public ActionResult IndexSummary(string cluster, string node, string index, string type, string subtype)
        {
            var vd = GetNode(cluster, node, index);

            switch (type)
            {
                case "shards":
                    return View("Indices.Shards", vd);
                default:
                    return ContentNotFound("Unknown summary view requested");
            }
        }

        private static DashboardModel.CurrentData GetNode(string cluster, string node = null, string index = null)
        {
            // TODO: node string split to cluster

            // Cluster names are not unique, names + node names should be though
            // If we see too many people with crazy combos, then node GUIDs it is.
            var cc = ElasticCluster.AllClusters.FirstOrDefault(c => string.Equals(c.Name, cluster, StringComparison.InvariantCultureIgnoreCase)
                                             && (node == null || (c.Nodes.Data != null && c.Nodes.Data.Get(node) != null)));
            var cn = cc != null ? cc.Nodes.Data.Get(node) : null;
            
            return new DashboardModel.CurrentData
                {
                    NodeName = node,
                    ClusterName = cc != null ? cc.Name : null,
                    IndexName = index,
                    Cluster = cc,
                    Node = cn
                };
        }

        [Route("elastic/indices")]
        public ActionResult Indices(string cluster, string node, string guid, bool ajax = false)
        {
            var current = GetNode(cluster, node ?? guid);
            if (ajax)
            {
                return View("Indices", current);
            }

            var vd = new DashboardModel
                {
                    Clusters = ElasticCluster.AllClusters,
                    Refresh = true,
                    View = DashboardModel.Views.Indices,
                    Current = current
                };
            return View("Dashboard", vd);
        }

        [Route("elastic/shards")]
        public ActionResult Shards(string cluster, string server, bool ajax = false)
        {
            var vd = new DashboardModel
            {
                Clusters = ElasticCluster.AllClusters,
                Refresh = true,
                View = DashboardModel.Views.Shards
            };
            return View(ajax ? "Dashboard.Cluster.Shards" : "Dashboard", vd);
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
