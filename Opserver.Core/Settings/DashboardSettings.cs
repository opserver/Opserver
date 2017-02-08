using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver
{
    public class DashboardSettings : Settings<DashboardSettings>, INodeSettings
    {
        public override bool Enabled => Providers.Any();

        public List<Category> Categories { get; set; } = new List<Category>();

        public ProvidersSettings Providers { get; set; } = new ProvidersSettings();

        public List<NodeSettings> PerNodeSettings { get; set; } = new List<NodeSettings>();

        public bool ShowOther { get; set; } = true;
        
        public NodeSettings GetNodeSettings(string node) => 
            PerNodeSettings?.FirstOrDefault(n => n.PatternRegex.IsMatch(node)) ?? NodeSettings.Empty;

        #region Direct Properties

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

        #endregion

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
            public Regex PatternRegex => _patternRegEx ?? (_patternRegEx = GetPatternMatcher(Pattern));

            /// <summary>
            /// The Pattern to match on node interfaces, an interface matching this pattern will be shown on the dashboard.
            /// </summary>
            public string PrimaryInterfacePattern { get; set; }

            private Regex _primaryInterfacePatternRegEx;
            public Regex PrimaryInterfacePatternRegex
            {
                get { return _primaryInterfacePatternRegEx ?? (_primaryInterfacePatternRegEx = GetPatternMatcher(PrimaryInterfacePattern)); }
                set { _primaryInterfacePatternRegEx = value; }
            }

            protected Regex GetPatternMatcher(string pattern)
            {
                return pattern.IsNullOrEmpty() ? null : new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
            }

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
            public Regex PatternRegex => _patternRegEx ?? (_patternRegEx = GetPatternMatcher(Pattern));

            /// <summary>
            /// The Pattern to match on node interfaces, an interface matching this pattern will be shown on the dashboard.
            /// </summary>
            public string PrimaryInterfacePattern { get; set; }

            private Regex _primaryInterfacePatternRegEx;
            public Regex PrimaryInterfacePatternRegex
            {
                get { return _primaryInterfacePatternRegEx ?? (_primaryInterfacePatternRegEx = GetPatternMatcher(PrimaryInterfacePattern)); }
                set { _primaryInterfacePatternRegEx = value; }
            }

            protected Regex GetPatternMatcher(string pattern)
            {
                return pattern.IsNullOrEmpty() ? null : new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
            }

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
    }
}
