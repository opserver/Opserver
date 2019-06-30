using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Opserver.Controllers;
using StackExchange.Opserver.Models.Security;

namespace StackExchange.Opserver.Helpers
{
    public static class NavTabs
    {
        public static List<NavTab> All { get; }

        static NavTabs()
        {
            var newTabs = new List<NavTab>();

            var tabControllers = typeof(NavTab).Assembly.GetTypes()
                .Where(t => t.BaseType == typeof (StatusController));

            foreach (var tc in tabControllers)
            {
                try
                {
                    //var tt = (Activator.CreateInstance(tc) as StatusController)?.TopTab;
                    //if (tt != null) newTabs.Add(tt);
                }
                catch (Exception e)
                {
                    Current.LogException("Error creating StatusController instance for " + tc, e);
                }
            }
            All = newTabs;
        }

        private static List<NavTab> GetAll(IEnumerable<StatusModule> modules)
        {
            var moduleControllers = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.BaseType == typeof(StatusController<>));
            var result = new List<NavTab>();
            foreach (var m in modules)
            {
            }
            result.Sort((a, b) => a.Order.CompareTo(b.Order));
            return result;
        }
    }

    public class NavTab
    {
        public string Name => Module?.Name ?? "Unknown"; // TODO: Remove when ordering is correct
        public string Controller { get; set; }
        public string Action { get; set; }
        public int Order { get; set; }
        public StatusModule Module { get; }

        public bool IsEnabled
        {
            get
            {
                var ss = Module.SecuritySettings;
                if (ss != null)
                {
                    if (!ss.Enabled|| !ss.HasAccess()) return false;
                }
                return true;
            }
        }

        public NavTab(StatusModule module, string action, StatusController controller)
        {
            Module = module;
            Controller = controller.GetType().Name.Replace("Controller", "");
            Action = action;
        }
    }

    public static class NavTabExtensions
    {
        public static IServiceCollection AddNavTabs(this IServiceCollection services, IConfiguration _configuration)
        {
            // TODO: Discovery instead

            services.Configure<HAProxySettings>(_configuration.GetSection("HAProxy"))
                    .AddSingleton<Data.HAProxy.HAProxyModule>();

            return services;
        }
    }
}
