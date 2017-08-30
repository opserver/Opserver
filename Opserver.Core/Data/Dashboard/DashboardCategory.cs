﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver.Data.Dashboard
{
    public class DashboardCategory
    {
        public static List<DashboardCategory> AllCategories { get; } =
            Current.Settings.Dashboard.Categories
                .Select(sc => new DashboardCategory(sc))
                .ToList();

        public static DashboardCategory Unknown { get; } = new DashboardCategory(new DashboardSettings.Category
        {
            Name = "Other Nodes",
            Pattern = ""
        })
        {
            Index = int.MaxValue - 100
        };

        public string Name => Settings.Name;
        public Regex PatternRegex => Settings.PatternRegex;
        public int Index { get; private set; }

        public DashboardSettings.Category Settings { get; }

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
                var servers = DashboardModule.AllNodes.Where(n => PatternRegex.IsMatch(n.Name)).ToList();

                var excluder = Current.Settings.Dashboard.ExcludePatternRegex;
                if (excluder != null)
                    servers = servers.Where(n => !excluder.IsMatch(n.PrettyName)).ToList();

                return servers.ToList();
            }
        }
    }
}