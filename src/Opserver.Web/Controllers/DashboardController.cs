using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Opserver.Data.Dashboard;
using Opserver.Helpers;
using Opserver.Views.Dashboard;

namespace Opserver.Controllers
{
    [OnlyAllow(DashboardRoles.Viewer)]
    public partial class DashboardController : StatusController<DashboardModule>
    {
        public DashboardController(DashboardModule module, IOptions<OpserverSettings> settings) : base(module, settings) { }

        [DefaultRoute("dashboard")]
        public ActionResult Dashboard(string q)
        {
            var vd = new DashboardModel
            {
                Nodes = GetNodes(q),
                ErrorMessages = Module.ProviderExceptions.ToList(),
                Filter = q,
                IsStartingUp = Module.AnyDoingFirstPoll
            };
            return Request.IsAjax() ? PartialView("Dashboard.Table", vd) : (ActionResult)View("Dashboard.Table", vd);
        }

        private List<Node> GetNodes(string search) =>
            search.HasValue()
            ? Module.AllNodes.Where(n => n.SearchString?.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1).ToList()
            : Module.AllNodes.ToList();

        [Route("dashboard/json")]
        public ActionResult DashboardJson()
        {
            var categories = Module.AllNodes
                .GroupBy(n => n.Category)
                .Where(g => g.Any() && (g.Key != DashboardCategory.Unknown || Module.Settings.ShowOther))
                .OrderBy(g => g.Key.Index);

            var resultCategories = categories.Select(g =>
            {
                var c = g.Key;
                var cNodes = g.OrderBy(n => n.PrettyName);
                var nodes = cNodes.Select(n => new
                {
                    n.Id,
                    Status = n.RawClass(),
                    Class = n.RowClass().Nullify() + (n.IsVM ? " virtual-machine" : null),
                    Search = n.SearchString,
                    Name = n.PrettyName,
                    Label = n.IsVM ? "Virtual Machine hosted on " + n.VMHost.PrettyName + " " : null + "Last Updated:" + n.LastSync?.ToRelativeTime(),
                    AppText = n.ApplicationCPUTextSummary,
                    CpuStatus = n.CPUMonitorStatus().RawClass(),
                    CPU = n.CPULoad,
                    AppMemory = n.ApplicationMemoryTextSummary.Nullify(),
                    MemStatus = n.MemoryMonitorStatus().RawClass().Nullify(),
                    MemPercent = n.PercentMemoryUsed > 0 ? n.PercentMemoryUsed : null,
                    MemText = $"{n.PrettyMemoryUsed()} / {n.PrettyTotalMemory()}({n.PercentMemoryUsed?.ToString("n2")}%)",
                    NetPretty = n.PrettyTotalNetwork(),
                    DiskPercent = n.Volumes?.Where(v => v.PercentUsed.HasValue).Max(v => v.PercentUsed.Value),
                    Disks = n.Volumes?.Select(v => new
                    {
                        Name = v.PrettyName,
                        Status = v.RawClass(),
                        Tooltip = $"{v.PrettyName}: {v.PercentUsed?.ToString("n2")}% used ({v.PrettyUsed}/{v.PrettySize})",
                        PercentUsed = v.PercentUsed?.ToString("n2")
                    })
                });
                return new
                {
                    c.Name,
                    Nodes = nodes
                };
            }).ToList();
            return Json(new
            {
                Module.HasData,
                Categories = resultCategories
            }, Jil.Options.ExcludeNulls);
        }

        [Route("dashboard/node")]
        public ActionResult Node([DefaultValue(CurrentStatusTypes.Stats)]CurrentStatusTypes view, string node = null)
        {
            var vd = new NodeModel
            {
                CurrentNode = Module.GetNodeByName(node),
                CurrentStatusType = view
            };

            return View("Node", vd);
        }

        [Route("dashboard/node/summary/{type}")]
        public ActionResult InstanceSummary(string node, string type)
        {
            var n = Module.GetNodeByName(node);
            return type switch
            {
                "hardware" => PartialView("Node.Hardware", n),
                "network" => PartialView("Node.Network", n),
                _ => ContentNotFound("Unknown summary view requested"),
            };
        }

        [Route("dashboard/graph/{nodeId}/{type}")]
        [Route("dashboard/graph/{nodeId}/{type}/{subId?}")]
        public async Task<ActionResult> NodeGraph(string nodeId, string type, string subId)
        {
            var n = Module.GetNodeById(nodeId);
            var vd = new NodeGraphModel
            {
                Node = n,
                Type = type
            };

            if (n != null)
            {
                switch (type)
                {
                    case NodeGraphModel.KnownTypes.Live:
                        await PopulateModel(vd, NodeGraphModel.KnownTypes.CPU, subId);
                        await PopulateModel(vd, NodeGraphModel.KnownTypes.Memory, subId);
                        //await PopulateModel(vd, NodeGraphModel.KnownTypes.Network, subId);
                        break;
                    case NodeGraphModel.KnownTypes.CPU:
                    case NodeGraphModel.KnownTypes.Memory:
                    case NodeGraphModel.KnownTypes.Network:
                    case NodeGraphModel.KnownTypes.Volume:
                    case NodeGraphModel.KnownTypes.VolumePerformance:
                        await PopulateModel(vd, type, subId);
                        break;
                }
            }

            return PartialView("Node.Graph", vd);
        }

        private static async Task PopulateModel(NodeGraphModel vd, string type, string subId)
        {
            var n = vd.Node;
            switch (type)
            {
                case NodeGraphModel.KnownTypes.CPU:
                    vd.Title = "CPU Utilization (" + (n.PrettyName ?? "Unknown") + ")";
                    vd.CpuData = await GraphController.CPUData(n, summary: true);
                    break;
                case NodeGraphModel.KnownTypes.Memory:
                    vd.Title = "Memory Utilization (" + (n.TotalMemory?.ToSize() ?? "Unknown Max") + ")";
                    vd.MemoryData = await GraphController.MemoryData(n, summary: true);
                    break;
                case NodeGraphModel.KnownTypes.Network:
                    if (subId.HasValue())
                    {
                        var i = vd.Node.GetInterface(subId);
                        vd.Interface = i;
                        vd.Title = "Network Utilization (" + (i?.PrettyName ?? "Unknown") + ")";
                        vd.NetworkData = await GraphController.NetworkData(i, summary: true);
                    }
                    else
                    {
                        vd.Title = "Network Utilization (" + (n.PrettyName ?? "Unknown") + ")";
                        vd.NetworkData = await GraphController.NetworkData(n, summary: true);
                    }
                    break;
                case NodeGraphModel.KnownTypes.Volume:
                    {
                        var v = vd.Node.GetVolume(subId);
                        vd.Volume = v;
                        vd.Title = "Volume Usage (" + (v?.PrettyName ?? "Unknown") + ")";
                        // TODO: Volume data
                    }
                    break;
                case NodeGraphModel.KnownTypes.VolumePerformance:
                    if (subId.HasValue())
                    {
                        var v = vd.Node.GetVolume(subId);
                        vd.Volume = v;
                        vd.Title = "Volume Performance (" + (v?.PrettyName ?? "Unknown") + ")";
                        vd.VolumePerformanceData = await GraphController.VolumePerformanceData(v, summary: true);
                    }
                    else
                    {
                        vd.Title = "Volume Performance (" + (n.PrettyName ?? "Unknown") + ")";
                        vd.VolumePerformanceData = await GraphController.VolumePerformanceData(n, summary: true);
                    }
                    break;
            }
        }
    }
}
