using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.Dashboard;

namespace StackExchange.Opserver.Controllers
{
    public partial class DashboardController : StatusController
    {
        public override ISecurableSection SettingsSection => Current.Settings.Dashboard;

        public override TopTab TopTab => new TopTab("Dashboard", nameof(Dashboard), this, 0);
        
        [Route("dashboard")]
        public ActionResult Dashboard(string filter)
        {
            var vd = new DashboardModel
                {
                    Nodes = DashboardData.AllNodes.ToList(),
                    ErrorMessages = DashboardData.ProviderExceptions.ToList(),
                    Filter = filter,
                    IsStartingUp = DashboardData.AnyDoingFirstPoll
                };
            return View(Current.IsAjaxRequest ? "Dashboard.Table" : "Dashboard", vd);
        }

        [Route("dashboard/node")]
        public ActionResult Node([DefaultValue(CurrentStatusTypes.Stats)]CurrentStatusTypes view, string node = null)
        {
            var vd = new NodeModel
            {
                CurrentNode = DashboardData.GetNodeByName(node),
                CurrentStatusType = view
            };

            return View("Node",vd);
        }

        [Route("dashboard/node/summary/{type}")]
        public ActionResult InstanceSummary(string node, string type)
        {
            var n = DashboardData.GetNodeByName(node);
            switch (type)
            {
                case "hardware":
                    return View("Node.Hardware", n);
                case "network":
                    return View("Node.Network", n);
                default:
                    return ContentNotFound("Unknown summary view requested");
            }
        }

        [Route("dashboard/graph/{nodeId}/{type}")]
        [Route("dashboard/graph/{nodeId}/{type}/{subId?}")]
        public async Task<ActionResult> NodeGraph(string nodeId, string type, string subId)
        {
            var n = DashboardData.GetNodeById(nodeId);
            var vd = new NodeGraphModel
            {
                Node = n,
                Type = type
            };

            if (n != null)
            {
                switch (type)
                {
                    case NodeGraphModel.KnownTypes.CPU:
                        vd.Title = "CPU Utilization (" + (n.PrettyName ?? "Unknown") + ")";
                        vd.GraphData = await GraphController.CPUData(n, summary: true);
                        break;
                    case NodeGraphModel.KnownTypes.Memory:
                        vd.Title = "Memory Utilization (" + (n.TotalMemory?.ToSize() ?? "Unknown Max") + ")";
                        vd.GraphData = await GraphController.MemoryData(n, summary: true);
                        break;
                    case NodeGraphModel.KnownTypes.Network:
                        if (subId.HasValue())
                        {
                            var i = vd.Node.GetInterface(subId);
                            vd.Interface = i;
                            vd.Title = "Network Utilization (" + (i?.PrettyName ?? "Unknown") + ")";
                            vd.GraphData = await GraphController.NetworkData(i, summary: true);
                        }
                        else
                        {
                            vd.Title = "Network Utilization (" + (n.PrettyName ?? "Unknown") + ")";
                            vd.GraphData = await GraphController.NetworkData(n, summary: true);
                        }
                        break;
                    case NodeGraphModel.KnownTypes.Volume:
                        var v = vd.Node.GetVolume(subId);
                        vd.Volume = v;
                        vd.Title = "Volume Usage (" + (v?.PrettyName ?? "Unknown") + ")";
                        break;
                }
            }

            return View("Node.Graph", vd);
        }
    }
}