﻿using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Data.Dashboard;
using StackExchange.Opserver.Data.Elastic;
using StackExchange.Opserver.Data.HAProxy;
using StackExchange.Opserver.Data.SQL;

namespace StackExchange.Opserver.Views.Hub
{
    public class HubView : IMonitorStatus
    {
        private static List<HubView> _registered { get; } = new List<HubView>();
        public static List<HubView> Views => _registered.Where(v => v.IsEnabled).OrderByWorst().ToList();

        private readonly Func<bool> _isEnabled;
        public bool IsEnabled => _isEnabled();

        private readonly Func<MonitorStatus> _getStatus;
        public MonitorStatus MonitorStatus => _getStatus();
        public string MonitorStatusReason => "Status is " + MonitorStatus;

        public string ViewName { get; }

        private HubView(Func<bool> isEnabled, Func<MonitorStatus> getStatus, string viewName)
        {
            _isEnabled = isEnabled;
            _getStatus = getStatus;
            ViewName = viewName;
        }

        public static void Register(Func<bool> isEnabled, Func<MonitorStatus> getStatus, string viewName)
        {
            _registered.Add(new HubView(isEnabled, getStatus, viewName));
        }

        // TODO: Move this to plugin registration
        static HubView()
        {
            var s = Current.Settings;
            Register(() => DashboardModule.Enabled, () => DashboardModule.AllNodes.GetWorstStatus(), "Index.Dashboard");
            Register(() => SQLModule.Enabled, () => SQLModule.AllInstances.GetWorstStatus(), "Index.SQL");
            Register(() => ElasticModule.Enabled, () => ElasticModule.Clusters.GetWorstStatus(), "Index.Elastic");
            Register(() => HAProxyModule.Enabled, () => HAProxyModule.Groups.GetWorstStatus(), "Index.HAProxy");
        }
    }
}