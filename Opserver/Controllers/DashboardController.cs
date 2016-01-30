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
        protected override ISecurableSection SettingsSection => Current.Settings.Dashboard;

        protected override string TopTab => TopTabs.BuiltIn.Dashboard;

        [Route("dashboard")]
        public ActionResult Dashboard(string filter)
        {
            var vd = new DashboardModel
                {
                    Nodes = DashboardData.AllNodes.Where(n => !Current.Settings.Dashboard.ExcludePatternRegex.IsMatch(n.Name)).ToList(),
                    ErrorMessages = DashboardData.ProviderExceptions.ToList(),
                    Filter = filter,
                    IsStartingUp = DashboardData.AnyDoingFirstPoll
                };
            return View(Current.IsAjaxRequest ? "Dashboard.Table" : "Dashboard", vd);
        }

        [Route("dashboard/node")]
        public ActionResult SingleNode([DefaultValue(CurrentStatusTypes.Stats)]CurrentStatusTypes view, string node = null)
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
                default:
                    return ContentNotFound("Unknown summary view requested");
            }
        }

        [Route("dashboard/graph/{node}/{type}")]
        [Route("dashboard/graph/{node}/{type}/{subId?}")]
        public async Task<ActionResult> NodeGraph(string node, string type, string subId)
        {
            var n = DashboardData.GetNodeByName(node);
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