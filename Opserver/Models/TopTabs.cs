using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using StackExchange.Opserver.Controllers;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Models.Security;

namespace StackExchange.Opserver.Models
{
    public class TopTabs
    {
        public static List<TopTab> Tabs { get; private set; }

        public static string CurrentTab
        {
            get { return HttpContext.Current.Items["TopTabs-Current"] as string; }
            set { HttpContext.Current.Items["TopTabs-Current"] = value; }
        }

        public static void SetCurrent(Type type)
        {
            var tab = Tabs.FirstOrDefault(t => t.ControllerType == type);
            if (tab != null) CurrentTab = tab.Name;
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
            ReloadTabs();
        }

        public static void ReloadTabs()
        {
            var newTabs = new List<TopTab>();

            var tabControllers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
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
        public ISecurableSection SecurableSection { get; set; }
        public Func<MonitorStatus> GetMonitorStatus { get; set; }
        public Func<string> GetTooltip { get; set; }
        public Func<int> GetBadgeCount { get; set; }

        public bool IsEnabled
        {
            get
            {
                if (SecurableSection != null)
                {
                    if (!SecurableSection.Enabled) return false;
                    if (!SecurableSection.HasAccess()) return false;
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
            SecurableSection = controller.SettingsSection;
            Order = order;
        }
    }
}