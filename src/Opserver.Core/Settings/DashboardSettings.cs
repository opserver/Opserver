using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Opserver.Data.Dashboard;

namespace Opserver
{
    public class DashboardSettings : ModuleSettings, INodeSettings
    {
        public override bool Enabled => Providers.Any();
        public override string AdminRole => DashboardRoles.Admin;
        public override string ViewRole => DashboardRoles.Viewer;

        public List<Category> Categories { get; set; } = new List<Category>();

        public ProvidersSettings Providers { get; set; } = new ProvidersSettings();

        public List<NodeSettings> PerNodeSettings { get; set; } = new List<NodeSettings>();

        public bool ShowOther { get; set; } = true;

        public NodeSettings GetNodeSettings(string node) =>
            PerNodeSettings?.FirstOrDefault(n => n.PatternRegex.IsMatch(node)) ?? NodeSettings.Empty;

        /// <summary>
        /// The Pattern to match on server names for hiding from the dashboard
        /// </summary>
        public string ExcludePattern { get; set; }

        private Regex _excludePatternRegEx;
        public Regex ExcludePatternRegex => _excludePatternRegEx ?? (ExcludePattern.HasValue() ? _excludePatternRegEx = new Regex(ExcludePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline) : null);

        /// <summary>
        /// Percent at which CPU on a node is marked at a warning level
        /// </summary>
        public decimal? CPUWarningPercent { get; set; }
        /// <summary>
        /// Percent at which CPU on a node is marked at a crtitical level
        /// </summary>
        public decimal? CPUCriticalPercent { get; set; }

        /// <summary>
        /// Percent at which memory usage on a node is marked at a warning level
        /// </summary>
        public decimal? MemoryWarningPercent { get; set; }
        /// <summary>
        /// Percent at which memory usage on a node is marked at a crtitical level
        /// </summary>
        public decimal? MemoryCriticalPercent { get; set; }

        /// <summary>
        /// Percent at which disk utilization on a node is marked at a warning level
        /// </summary>
        public decimal? DiskWarningPercent { get; set; }
        /// <summary>
        /// Percent at which disk utilization on a node is marked at a critical level
        /// </summary>
        public decimal? DiskCriticalPercent { get; set; }

        /// <summary>
        /// Whether to show volume performance on the dashboard
        /// </summary>
        public bool ShowVolumePerformance { get; set; }

        /// <summary>
        /// The Pattern to match on node services, all services matching this pattern will be shown on the dashboard. 
        /// </summary>
        public string ServicesPattern { get; set; }

        private Regex _servicesPatternRegEx;
        public Regex ServicesPatternRegEx
        {
            get => _servicesPatternRegEx ??= GetPatternMatcher(ServicesPattern);
            set => _servicesPatternRegEx = value;
        }

        protected static Regex GetPatternMatcher(string pattern) =>
            pattern.IsNullOrEmpty() ? null : new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Category that a server belongs to
        /// </summary>
        public class Category : INodeSettings, ISettingsCollectionItem
        {
            /// <summary>
            /// The name that appears for this category
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The name that appears for this category
            /// </summary>
            public string Pattern { get; set; }

            private Regex _patternRegEx;
            /// <summary>
            /// The pattern to match for these node settings
            /// </summary>
            public Regex PatternRegex => _patternRegEx ??= GetPatternMatcher(Pattern);

            /// <summary>
            /// The Pattern to match on node interfaces, an interface matching this pattern will be shown on the dashboard.
            /// </summary>
            public string PrimaryInterfacePattern { get; set; }

            private Regex _primaryInterfacePatternRegEx;
            public Regex PrimaryInterfacePatternRegex
            {
                get => _primaryInterfacePatternRegEx ??= GetPatternMatcher(PrimaryInterfacePattern);
                set => _primaryInterfacePatternRegEx = value;
            }

            protected static Regex GetPatternMatcher(string pattern) =>
                pattern.IsNullOrEmpty() ? null : new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

            /// <summary>
            /// Percent at which CPU on a node is marked at a warning level
            /// </summary>
            public decimal? CPUWarningPercent { get; set; }
            /// <summary>
            /// Percent at which CPU on a node is marked at a crtitical level
            /// </summary>
            public decimal? CPUCriticalPercent { get; set; }

            /// <summary>
            /// Percent at which memory usage on a node is marked at a warning level
            /// </summary>
            public decimal? MemoryWarningPercent { get; set; }
            /// <summary>
            /// Percent at which memory usage on a node is marked at a crtitical level
            /// </summary>
            public decimal? MemoryCriticalPercent { get; set; }

            /// <summary>
            /// Percent at which disk utilization on a node is marked at a warning level
            /// </summary>
            public decimal? DiskWarningPercent { get; set; }
            /// <summary>
            /// Percent at which disk utilization on a node is marked at a critical level
            /// </summary>
            public decimal? DiskCriticalPercent { get; set; }

            /// <summary>
            /// The Pattern to match on node services, all services matching this pattern will be shown on the dashboard. 
            /// </summary>
            public string ServicesPattern { get; set; }

            private Regex _servicesPatternRegEx;
            public Regex ServicesPatternRegEx
            {
                get => _servicesPatternRegEx ??= GetPatternMatcher(ServicesPattern);
                set => _servicesPatternRegEx = value;
            }
        }

        /// <summary>
        /// Addtional per-node settings
        /// </summary>
        public class NodeSettings : INodeSettings, ISettingsCollectionItem
        {
            public static NodeSettings Empty { get; } = new NodeSettings();
            string ISettingsCollectionItem.Name => Pattern;

            /// <summary>
            /// The name that appears for this category
            /// </summary>
            public string Pattern { get; set; }

            private Regex _patternRegEx;
            /// <summary>
            /// The pattern to match for these node settings
            /// </summary>
            public Regex PatternRegex => _patternRegEx ??= GetPatternMatcher(Pattern);

            /// <summary>
            /// The Pattern to match on node interfaces, an interface matching this pattern will be shown on the dashboard.
            /// </summary>
            public string PrimaryInterfacePattern { get; set; }

            private Regex _primaryInterfacePatternRegEx;
            public Regex PrimaryInterfacePatternRegex
            {
                get => _primaryInterfacePatternRegEx ??= GetPatternMatcher(PrimaryInterfacePattern);
                set => _primaryInterfacePatternRegEx = value;
            }

            protected static Regex GetPatternMatcher(string pattern) =>
                pattern.IsNullOrEmpty() ? null : new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

            /// <summary>
            /// Percent at which CPU on a node is marked at a warning level
            /// </summary>
            public decimal? CPUWarningPercent { get; set; }
            /// <summary>
            /// Percent at which CPU on a node is marked at a crtitical level
            /// </summary>
            public decimal? CPUCriticalPercent { get; set; }

            /// <summary>
            /// Percent at which memory usage on a node is marked at a warning level
            /// </summary>
            public decimal? MemoryWarningPercent { get; set; }
            /// <summary>
            /// Percent at which memory usage on a node is marked at a crtitical level
            /// </summary>
            public decimal? MemoryCriticalPercent { get; set; }

            /// <summary>
            /// Percent at which disk utilization on a node is marked at a warning level
            /// </summary>
            public decimal? DiskWarningPercent { get; set; }
            /// <summary>
            /// Percent at which disk utilization on a node is marked at a critical level
            /// </summary>
            public decimal? DiskCriticalPercent { get; set; }

            /// <summary>
            /// The Pattern to match on node services, all services matching this pattern will be shown on the dashboard. 
            /// </summary>
            public string ServicesPattern { get; set; }

            private Regex _servicesPatternRegEx;
            public Regex ServicesPatternRegEx
            {
                get => _servicesPatternRegEx ??= GetPatternMatcher(ServicesPattern);
                set => _servicesPatternRegEx = value;
            }
        }
    }

    // TODO: Temporary/simple use case, full alerting will replace this
    public interface INodeSettings
    {
        decimal? CPUWarningPercent { get; set; }
        decimal? CPUCriticalPercent { get; set; }
        decimal? MemoryWarningPercent { get; set; }
        decimal? MemoryCriticalPercent { get; set; }
        decimal? DiskWarningPercent { get; set; }
        decimal? DiskCriticalPercent { get; set; }
        Regex ServicesPatternRegEx { get; set; }
    }
}
