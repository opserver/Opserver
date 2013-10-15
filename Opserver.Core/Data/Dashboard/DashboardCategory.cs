using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver.Data.Dashboard
{
    public class DashboardCategory
    {
        private static List<DashboardCategory> _allCategories;
        public static List<DashboardCategory> AllCategories
        {
            get { return _allCategories ?? (_allCategories = Current.Settings.Dashboard.Categories.Select(sc => new DashboardCategory(sc)).ToList()); }
        }

        public static DashboardCategory Unknown { get; private set; }

        static DashboardCategory()
        {
            var unknownSettings = new DashboardSettings.Category
                {
                    Name = "Other Nodes",
                    Pattern = ""
                };
            Unknown = new DashboardCategory(unknownSettings)
                {
                    Index = int.MaxValue - 100
                };
        }

        public string Name { get { return Settings.Name; } }
        public Regex PatternRegex { get { return Settings.PatternRegex; } }
        public int Index { get; private set; }

        public DashboardSettings.Category Settings { get; private set; }

        public decimal? CPUWarningPercent { get { return Settings.CPUWarningPercent; } }
        public decimal? CPUCriticalPercent { get { return Settings.CPUCriticalPercent; } }
        public decimal? MemoryWarningPercent { get { return Settings.MemoryWarningPercent; } }
        public decimal? MemoryCriticalPercent { get { return Settings.MemoryCriticalPercent; } }
        public decimal? DiskWarningPercent { get { return Settings.DiskWarningPercent; } }
        public decimal? DiskCriticalPercent { get { return Settings.DiskCriticalPercent; } }

        public Regex PrimaryInterfacePatternRegex { get { return Settings.PrimaryInterfacePatternRegex; } }

        
        public DashboardCategory() { }
        public DashboardCategory(DashboardSettings.Category settingsCategory)
        {
            Settings = settingsCategory;
            Index = Current.Settings.Dashboard.Categories.IndexOf(settingsCategory);
        }

        public List<Node> Nodes
        {
            get
            {
                var servers = DashboardData.AllNodes.Where(n => PatternRegex.IsMatch(n.Name)).ToList();

                var excluder = Current.Settings.Dashboard.ExcludePatternRegex;
                if (excluder != null)
                    servers = servers.Where(n => !excluder.IsMatch(n.PrettyName)).ToList();

                return servers.ToList();
            }
        }
    }
}