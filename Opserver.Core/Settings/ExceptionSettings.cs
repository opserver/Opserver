using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver
{
    public class ExceptionsSettings : Settings<ExceptionsSettings>
    {
        public override bool Enabled => Stores.Count > 0;

        public List<Store> Stores { get; set; } = new List<Store>();

        public List<ExceptionsGroup> Groups { get; set; } = new List<ExceptionsGroup>();

        public List<string> Applications { get; set; } = new List<string>();

        public List<StackTraceSourceLinkPattern> StackTraceReplacements { get; set; } = new List<StackTraceSourceLinkPattern>();

        /// <summary>
        /// How many exceptions before the exceptions are highlighted as a warning in the header, null (default) is ignored
        /// </summary>
        public int? WarningCount { get; set; }

        /// <summary>
        /// How many exceptions before the exceptions are highlighted as critical in the header, null (default) is ignored
        /// </summary>
        public int? CriticalCount { get; set; }

        /// <summary>
        /// How many seconds a error is considered "recent"
        /// </summary>
        public int RecentSeconds { get; set; } = 600;

        /// <summary>
        /// How many recent exceptions before the exceptions are highlighted as a warning in the header, null (default) is ignored
        /// </summary>
        public int? WarningRecentCount { get; set; }

        /// <summary>
        /// How many recent exceptions before the exceptions are highlighted as critical in the header, null (default) is ignored
        /// </summary>
        public int? CriticalRecentCount { get; set; }

        /// <summary>
        /// Default maximum timeout in milliseconds before giving up on an sources
        /// </summary>
        public bool EnablePreviews { get; set; } = true;

        /// <summary>
        /// Count to cache in memory per app, defaults to 5000
        /// </summary>
        public int PerAppCacheCount { get; set; } = 5000;

        public class ExceptionsGroup : ISettingsCollectionItem
        {
            /// <summary>
            /// The name that appears for this group, used as a key for applications
            /// </summary>
            public string Name { get; set; }
            public string Description { get; set; }
            public List<string> Applications { get; set; } = new List<string>();
        }

        public class Store : ISettingsCollectionItem
        {
            /// <summary>
            /// The name that appears for this store, used as a key for applications
            /// </summary>
            public string Name { get; set; }
            public string Description { get; set; }

            /// <summary>
            /// Maximum timeout in milliseconds before giving up on this store
            /// </summary>
            public int? QueryTimeoutMs { get; set; }

            /// <summary>
            /// How often to poll this store
            /// </summary>
            public int PollIntervalSeconds { get; set; } = 300;

            /// <summary>
            /// Connection string for this store's database
            /// </summary>
            public string ConnectionString { get; set; }
        }

        public class StackTraceSourceLinkPattern : ISettingsCollectionItem
        {
            /// <summary>
            /// The name of the deep link pattern.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// A regular expression for detecting links in stack traces.
            /// Used in conjuction with <see cref="Replacement"/>.
            /// </summary>
            public string Pattern { get; set; }

            /// <summary>
            /// A replacement pattern for rendering links from a <see cref="Pattern"/> match.
            /// matches via <see cref="Regex.Replace(string, string, string)"/>.
            /// </summary>
            public string Replacement { get; set; }

            private static readonly Regex DontMatchAnything = new Regex("(?!)");

            private Regex _regex;
            public Regex RegexPattern()
            {
                if (_regex == null)
                {
                    if (Pattern.HasValue())
                    {
                        try
                        {
                            _regex = new Regex(Pattern, RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);
                        }
                        catch (Exception ex)
                        {
                            Current.LogException($"Unable to parse source link pattern for '{nameof(Name)}': '{Pattern}'", ex);
                            _regex = DontMatchAnything;
                        }
                    }
                    else
                    {
                        _regex = DontMatchAnything;
                    }
                }
                return _regex;
            }
        }
    }
}
