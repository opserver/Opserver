﻿using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Jil;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.Dashboard;
using System;
using System.Collections.Generic;

namespace StackExchange.Opserver.Controllers
{
    public partial class DashboardController : StatusController
    {
        public override ISecurableModule SettingsModule => Current.Settings.Dashboard;

        public override TopTab TopTab => new TopTab("Dashboard", nameof(Dashboard), this, 0);

        [Route("dashboard")]
        public ActionResult Dashboard(string q)
        {
            var vd = new DashboardModel
            {
                Nodes = GetNodes(q),
                ErrorMessages = DashboardModule.ProviderExceptions.ToList(),
                Filter = q,
                IsStartingUp = DashboardModule.AnyDoingFirstPoll
            };
            return View(Current.IsAjaxRequest ? "Dashboard.Table" : "Dashboard", vd);
        }

        private List<Node> GetNodes(string search) =>
            search.HasValue()
            ? DashboardModule.AllNodes.Where(n => n.SearchString?.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1).ToList()
            : DashboardModule.AllNodes.ToList();

        [Route("dashboard/json")]
        public ActionResult DashboardJson()
        {
            var categories = DashboardModule.AllNodes
                .GroupBy(n => n.Category)
                .Where(g => g.Any() && (g.Key != DashboardCategory.Unknown || Current.Settings.Dashboard.ShowOther))
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
                DashboardModule.HasData,
                Categories = resultCategories
            }, Options.ExcludeNulls);
        }

        [Route("dashboard/node")]
        public ActionResult Node([DefaultValue(CurrentStatusTypes.Stats)]CurrentStatusTypes view, string node = null)
        {
            var vd = new NodeModel
            {
                CurrentNode = DashboardModule.GetNodeByName(node),
                CurrentStatusType = view
            };

            return View("Node", vd);
        }

        [Route("dashboard/node/summary/{type}")]
        public ActionResult InstanceSummary(string node, string type)
        {
            var n = DashboardModule.GetNodeByName(node);
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
            var n = DashboardModule.GetNodeById(nodeId);
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
                        await PopulateModel(vd, NodeGraphModel.KnownTypes.CPU, subId).ConfigureAwait(false);
                        await PopulateModel(vd, NodeGraphModel.KnownTypes.Memory, subId).ConfigureAwait(false);
                        //await PopulateModel(vd, NodeGraphModel.KnownTypes.Network, subId);
                        break;
                    case NodeGraphModel.KnownTypes.CPU:
                    case NodeGraphModel.KnownTypes.Memory:
                    case NodeGraphModel.KnownTypes.Network:
                    case NodeGraphModel.KnownTypes.Volume:
                    case NodeGraphModel.KnownTypes.VolumePerformance:
                        await PopulateModel(vd, type, subId).ConfigureAwait(false);
                        break;
                }
            }

            return View("Node.Graph", vd);
        }

        private async Task PopulateModel(NodeGraphModel vd, string type, string subId)
        {
            var n = vd.Node;
            switch (type)
            {
                case NodeGraphModel.KnownTypes.CPU:
                    vd.Title = "CPU Utilization (" + (n.PrettyName ?? "Unknown") + ")";
                    vd.CpuData = await GraphController.CPUData(n, summary: true).ConfigureAwait(false);
                    break;
                case NodeGraphModel.KnownTypes.Memory:
                    vd.Title = "Memory Utilization (" + (n.TotalMemory?.ToSize() ?? "Unknown Max") + ")";
                    vd.MemoryData = await GraphController.MemoryData(n, summary: true).ConfigureAwait(false);
                    break;
                case NodeGraphModel.KnownTypes.Network:
                    if (subId.HasValue())
                    {
                        var i = vd.Node.GetInterface(subId);
                        vd.Interface = i;
                        vd.Title = "Network Utilization (" + (i?.PrettyName ?? "Unknown") + ")";
                        vd.NetworkData = await GraphController.NetworkData(i, summary: true).ConfigureAwait(false);
                    }
                    else
                    {
                        vd.Title = "Network Utilization (" + (n.PrettyName ?? "Unknown") + ")";
                        vd.NetworkData = await GraphController.NetworkData(n, summary: true).ConfigureAwait(false);
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
                        vd.VolumePerformanceData = await GraphController.VolumePerformanceData(v, summary: true).ConfigureAwait(false);
                    }
                    else
                    {
                        vd.Title = "Volume Performance (" + (n.PrettyName ?? "Unknown") + ")";
                        vd.VolumePerformanceData = await GraphController.VolumePerformanceData(n, summary: true).ConfigureAwait(false);
                    }
                    break;
            }
        }
    }
}