﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver
{
    public class DashboardSettings : Settings<DashboardSettings>, IAfterLoadActions, INodeSettings
    {
        public override bool Enabled { get { return Provider != null; } }
        
        public ObservableCollection<Category> Categories { get; set; }
        public event EventHandler<Category> CategoryAdded = delegate { };
        public event EventHandler<List<Category>> CategoriesChanged = delegate { };
        public event EventHandler<Category> CategoryRemoved = delegate { };

        public ProviderSettings Provider { get; set; }

        public ObservableCollection<NodeSettings> PerNodeSettings { get; set; }
        public event EventHandler<NodeSettings> PerNodeSettingAdded = delegate { };
        public event EventHandler<List<NodeSettings>> PerNodeSettingsChanged = delegate { };
        public event EventHandler<NodeSettings> PerNodeSettingRemoved = delegate { };

        public bool ShowOther { get; set; }

        public DashboardSettings()
        {
            Categories = new ObservableCollection<Category>();
            PerNodeSettings = new ObservableCollection<NodeSettings>();
            ShowOther = true;
        }

        public void AfterLoad()
        {
            Categories.AddHandlers(this, CategoryAdded, CategoriesChanged, CategoryRemoved);
            PerNodeSettings.AddHandlers(this, PerNodeSettingAdded, PerNodeSettingsChanged, PerNodeSettingRemoved);
        }

        public NodeSettings GetNodeSettings(string node, Category c)
        {
            var s = PerNodeSettings != null ? PerNodeSettings.FirstOrDefault(n => n.PatternRegex.IsMatch(node)) : null;
            
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
                    PrimaryInterfacePatternRegex = (s != null ? s.PrimaryInterfacePatternRegex : null) ?? c.PrimaryInterfacePatternRegex
                };
        }

        /// <summary>
        /// The Pattern to match on server names for hiding from the dashboard
        /// </summary>
        public string ExcludePattern { get; set; }

        private Regex _excludePatternRegEx;
        public Regex ExcludePatternRegex
        {
            get { return _excludePatternRegEx ?? (ExcludePattern.HasValue() ? _excludePatternRegEx = new Regex(ExcludePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline) : null); }
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
            public Regex PatternRegex
            {
                get { return _patternRegEx ?? (_patternRegEx = GetPatternMatcher(Pattern)); }
            }

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

            public bool Equals(Category other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Name, other.Name)
                       && string.Equals(Pattern, other.Pattern)
                       && string.Equals(PrimaryInterfacePattern, other.PrimaryInterfacePattern)
                       && CPUWarningPercent == other.CPUWarningPercent
                       && CPUCriticalPercent == other.CPUCriticalPercent
                       && MemoryWarningPercent == other.MemoryWarningPercent
                       && MemoryCriticalPercent == other.MemoryCriticalPercent
                       && DiskWarningPercent == other.DiskWarningPercent
                       && DiskCriticalPercent == other.DiskCriticalPercent;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Category) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = (Name != null ? Name.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (Pattern != null ? Pattern.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (PrimaryInterfacePattern != null ? PrimaryInterfacePattern.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ CPUWarningPercent.GetHashCode();
                    hashCode = (hashCode*397) ^ CPUCriticalPercent.GetHashCode();
                    hashCode = (hashCode*397) ^ MemoryWarningPercent.GetHashCode();
                    hashCode = (hashCode*397) ^ MemoryCriticalPercent.GetHashCode();
                    hashCode = (hashCode*397) ^ DiskWarningPercent.GetHashCode();
                    hashCode = (hashCode*397) ^ DiskCriticalPercent.GetHashCode();
                    return hashCode;
                }
            }
        }

        /// <summary>
        /// Addtional per-node settings
        /// </summary>
        public class NodeSettings : INodeSettings, ISettingsCollectionItem<NodeSettings>
        {
            string ISettingsCollectionItem.Name { get { return Pattern; } }

            /// <summary>
            /// The name that appears for this category
            /// </summary>
            public string Pattern { get; set; }

            private Regex _patternRegEx;
            /// <summary>
            /// The pattern to match for these node settings
            /// </summary>
            public Regex PatternRegex
            {
                get { return _patternRegEx ?? (_patternRegEx = GetPatternMatcher(Pattern)); }
            }

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

            public bool Equals(NodeSettings other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Pattern, other.Pattern)
                       && string.Equals(PrimaryInterfacePattern, other.PrimaryInterfacePattern)
                       && CPUWarningPercent == other.CPUWarningPercent
                       && CPUCriticalPercent == other.CPUCriticalPercent
                       && MemoryWarningPercent == other.MemoryWarningPercent
                       && MemoryCriticalPercent == other.MemoryCriticalPercent
                       && DiskWarningPercent == other.DiskWarningPercent
                       && DiskCriticalPercent == other.DiskCriticalPercent;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((NodeSettings) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = (Pattern != null ? Pattern.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (PrimaryInterfacePattern != null ? PrimaryInterfacePattern.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ CPUWarningPercent.GetHashCode();
                    hashCode = (hashCode*397) ^ CPUCriticalPercent.GetHashCode();
                    hashCode = (hashCode*397) ^ MemoryWarningPercent.GetHashCode();
                    hashCode = (hashCode*397) ^ MemoryCriticalPercent.GetHashCode();
                    hashCode = (hashCode*397) ^ DiskWarningPercent.GetHashCode();
                    hashCode = (hashCode*397) ^ DiskCriticalPercent.GetHashCode();
                    return hashCode;
                }
            }
        }

        /// <summary>
        /// Data provider for dashboard data
        /// </summary>
        public class ProviderSettings
        {
            public ProviderSettings()
            {
                QueryTimeoutMs = 10 * 1000;
            }

            /// <summary>
            /// The name that appears for this provider
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The type that appears for this provider
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            /// The host for this provider
            /// </summary>
            public string Host { get; set; }

            /// <summary>
            /// The connection string for this provider
            /// </summary>
            public string ConnectionString { get; set; }

            /// <summary>
            /// Default maximum timeout in milliseconds before giving up on fetching data from this provider
            /// </summary>
            public int QueryTimeoutMs { get; set; }

            public bool Equals(ProviderSettings other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Name, other.Name)
                       && string.Equals(Type, other.Type)
                       && string.Equals(Host, other.Host)
                       && string.Equals(ConnectionString, other.ConnectionString)
                       && QueryTimeoutMs == other.QueryTimeoutMs;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((ProviderSettings)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = (Name != null ? Name.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (Type != null ? Type.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (Host != null ? Host.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (ConnectionString != null ? ConnectionString.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ QueryTimeoutMs;
                    return hashCode;
                }
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
    }
}
