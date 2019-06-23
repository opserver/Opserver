using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver.Data.Dashboard
{
    public class DashboardCategory
    {
        public static DashboardCategory Unknown { get; } = new DashboardCategory(null, new DashboardSettings.Category
        {
            Name = "Other Nodes",
            Pattern = ""
        });

        public string Name => Settings.Name;
        public Regex PatternRegex => Settings.PatternRegex;
        public int Index { get; }

        private DashboardModule Module { get; }
        public DashboardSettings.Category Settings { get; }

        public DashboardCategory() { }
        public DashboardCategory(DashboardModule module, DashboardSettings.Category settingsCategory)
        {
            Module = module;
            Settings = settingsCategory;
            Index = module?.Settings.Categories.IndexOf(settingsCategory) ?? int.MaxValue - 100;
        }

        public List<Node> Nodes
        {
            get
            {
                // TODO: Handle Unknown here (null Module)
                var servers = Module.AllNodes.Where(n => PatternRegex.IsMatch(n.Name)).ToList();

                var excluder = Module.Settings.ExcludePatternRegex;
                if (excluder != null)
                    servers = servers.Where(n => !excluder.IsMatch(n.PrettyName)).ToList();

                return servers.ToList();
            }
        }
    }
}
