using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Data.Elastic;
using StackExchange.Opserver.Data.Exceptions;
using StackExchange.Opserver.Data.HAProxy;
using StackExchange.Opserver.Data.Redis;
using StackExchange.Opserver.Data.SQL;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models.Security;

namespace StackExchange.Opserver.Models
{
    public class TopTabs
    {
        public static class BuiltIn
        {
            public const string Dashboard = "Dashboard";
            public const string Exceptions = "Exceptions";
            public const string SQL = "SQL";
            public const string Redis = "Redis";
            public const string Elastic = "Elastic";
            public const string CloudFlare = "CloudFlare";
            public const string HAProxy = "HAProxy";
        }

        public static SortedList<int, TopTab> Tabs { get; set; }

        public static string CurrentTab
        {
            get { return HttpContext.Current.Items["TopTabs-Current"] as string; }
            set { HttpContext.Current.Items["TopTabs-Current"] = value; }
        }

        public static bool HideAll
        {
            get
            {
                var val = HttpContext.Current.Items["TopTabs-HideAll"];
                return val != null && (bool)val;
            }
            set { HttpContext.Current.Items["TopTabs-HideAll"] = value; }
        }

        static TopTabs()
        {
            Tabs = new SortedList<int, TopTab>();

            AddTab("Dashboard", "/dashboard", 0);

            var s = Current.Settings;

            AddTab(new TopTab("SQL", "/sql", 10, s.SQL) { GetMonitorStatus = () => SQLInstance.AllInstances.GetWorstStatus() });
            AddTab(new TopTab("Redis", "/redis", 20, s.Redis) { GetMonitorStatus = () => RedisInstance.AllInstances.GetWorstStatus() });
            AddTab(new TopTab("Elastic", "/elastic", 30, s.Elastic) { GetMonitorStatus = () => ElasticCluster.AllClusters.GetWorstStatus() });
            AddTab(new TopTab("CloudFlare", "/cloudflare", 40, s.CloudFlare) { GetMonitorStatus = () => ElasticCluster.AllClusters.GetWorstStatus() });
            AddTab(new TopTab("Exceptions", "/exceptions", 50, s.Exceptions)
            {
                GetMonitorStatus = () => ExceptionStores.MonitorStatus,
                GetText = () =>
                {
                    var exceptionCount = ExceptionStores.TotalExceptionCount;
                    return string.Format("<span class=\"count exception-count\">{0}</span> {1}",
                        exceptionCount.ToComma(),
                        exceptionCount.Pluralize("Exception", false));
                },
                GetTooltip = () => ExceptionStores.TotalRecentExceptionCount.ToComma() + " recent"
            });
            AddTab(new TopTab("HAProxy", "/haproxy", 60, s.HAProxy) { GetMonitorStatus = () => HAProxyGroup.AllGroups.GetWorstStatus() });
        }

        public static TopTab AddTab(string name, string url, int order = 0)
        {
            var tab = new TopTab(name, url, order);
            Tabs.Add(tab.Order, tab);
            return tab;
        }

        public static void AddTab(TopTab tab)
        {
            Tabs.Add(tab.Order, tab);
        }
    }

    public class TopTab : ITopTab
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public int Order { get; set; }
        public ISecurableSection SecurableSection { get; set; }
        public Func<bool> GetIsEnabled { get; set; }
        public Func<MonitorStatus> GetMonitorStatus { get; set; }
        public Func<string> GetText { get; set; }
        public Func<string> GetTooltip { get; set; }

        public bool IsEnabled
        {
            get
            {
                if (SecurableSection != null)
                {
                    if (!SecurableSection.Enabled) return false;
                    if (!SecurableSection.HasAccess()) return false;
                }
                return GetIsEnabled == null || GetIsEnabled();
            }
        }

        public bool IsCurrentTab
        {
            get { return string.Equals(TopTabs.CurrentTab, Name); }
        }

        public TopTab(string name, string url, int order = 0, ISecurableSection section = null)
        {
            Name = name;
            Url = url;
            Order = order;
            SecurableSection = section;
        }

        public IHtmlString Render()
        {
            if (!IsEnabled) return MvcHtmlString.Empty;

            // Optimism!
            var status = GetMonitorStatus != null ? GetMonitorStatus() : MonitorStatus.Good;

            return string.Format(@"<a class=""{0}{1}"" href=""{2}"" title=""{3}"">{4}</a>",
                IsCurrentTab ? "selected " : "",
                status.GetDescription(),
                Url,
                GetTooltip != null ? GetTooltip() : null,
                GetText != null ? GetText() : Name).AsHtml();

        }
    }

    public interface ITopTab
    {
        string Name { get; set; }
        string Url { get; set; }
        int Order { get; set; }
        Func<bool> GetIsEnabled { get; set; }
        Func<MonitorStatus> GetMonitorStatus { get; set; }
        Func<string> GetText { get; set; }
        Func<string> GetTooltip { get; set; }
    }
}