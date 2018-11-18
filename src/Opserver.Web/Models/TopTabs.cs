using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Opserver.Controllers;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Models.Security;

namespace StackExchange.Opserver.Models
{
    public static class TopTabs
    {
        public static List<TopTab> Tabs { get; private set; }

        public static string CurrentTab
        {
            get => Current.Context.Items["TopTabs-Current"] as string;
            set => Current.Context.Items["TopTabs-Current"] = value;
        }

        public static void SetCurrent(Type type)
        {
            var tab = Tabs.Find(t => t.ControllerType == type);
            if (tab != null) CurrentTab = tab.Name;
        }

        static TopTabs()
        {
            ReloadTabs();
        }

        public static void ReloadTabs()
        {
            var newTabs = new List<TopTab>();

            var tabControllers = typeof(TopTabs).Assembly.GetTypes()
                .Where(t => t.BaseType == typeof (StatusController));

            foreach (var tc in tabControllers)
            {
                try
                {
                    var tt = (Activator.CreateInstance(tc) as StatusController)?.TopTab;
                    if (tt != null) newTabs.Add(tt);
                }
                catch (Exception e)
                {
                    Current.LogException("Error creating StatusController instance for " + tc, e);
                }
            }
            newTabs.Sort((a, b) => a.Order.CompareTo(b.Order));
            Tabs = newTabs;
        }
    }

    public class TopTab
    {
        public string Name { get; set; }
        public string Controller { get; set; }
        public Type ControllerType { get; set; }
        public string Action { get; set; }
        public int Order { get; set; }
        public ISecurableModule SecurableModule { get; set; }
        public Func<MonitorStatus> GetMonitorStatus { get; set; }
        public Func<string> GetTooltip { get; set; }
        public Func<int> GetBadgeCount { get; set; }

        public bool IsEnabled
        {
            get
            {
                if (SecurableModule != null)
                {
                    if (!SecurableModule.Enabled) return false;
                    if (!SecurableModule.HasAccess()) return false;
                }
                return true;
            }
        }

        public bool IsCurrentTab => string.Equals(TopTabs.CurrentTab, Name);

        public TopTab(string name, string action, StatusController controller, int order)
        {
            Name = name;
            Controller = controller.GetType().Name.Replace("Controller", "");
            ControllerType = controller.GetType();
            Action = action;
            SecurableModule = controller.SettingsModule;
            Order = order;
        }
    }
}
