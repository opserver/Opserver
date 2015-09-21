using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver
{
    public partial class DashboardSettings : Settings<DashboardSettings>, INodeSettings
    {
        public override bool Enabled => Providers.Any();

        public List<Category> Categories { get; set; }
        
        public ProvidersSettings Providers { get; set; }

        public List<NodeSettings> PerNodeSettings { get; set; }

        public bool ShowOther { get; set; }

        public DashboardSettings()
        {
            Categories = new List<Category>();
            Providers = new ProvidersSettings();
            PerNodeSettings = new List<NodeSettings>();
            ShowOther = true;
        }

        public NodeSettings GetNodeSettings(string node, Category c)
        {
            var s = PerNodeSettings?.FirstOrDefault(n => n.PatternRegex.IsMatch(node));
            
            // Grab setting from node, then category, then global
            Func<Func<INodeSettings, decimal?>, decimal?> getVal = f => (s != null ? f(s) : null) ?? f(c) ?? f(this);

            return new NodeSettings
                {
                    CPUWarningPercent = getVal(i => i.CPUWarningPercent),
                    CPUCriticalPercent = getVal(i => i.CPUCriticalPercent),
                    MemoryWarningPercent = getVal(i => i.MemoryWarningPercent),
                    MemoryCriticalPercent = getVal(i => i.MemoryCriticalPercent),
                    DiskWarningPercent = getVal(i => i.DiskWarningPercent),
                    DiskCriticalPercent = getVal(i => i.DiskCriticalPercent),
                    PrimaryInterfacePatternRegex = s?.PrimaryInterfacePatternRegex ?? c.PrimaryInterfacePatternRegex
                };
        }

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
        public class Category : INodeSettings, ISettingsCollectionItem<Category>
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
        public class NodeSettings : INodeSettings, ISettingsCollectionItem<NodeSettings>
        {
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
