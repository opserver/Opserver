using Opserver.Data.Exceptions;
using StackExchange.Exceptional;
using System;
using System.Collections.Generic;

namespace Opserver
{
    public class ExceptionsSettings : ModuleSettings
    {
        public EmailSettings EmailSettings { get; set; } = new EmailSettings();
        public override bool Enabled => Stores.Count > 0;
        public override string AdminRole => ExceptionsRoles.Admin;
        public override string ViewRole => ExceptionsRoles.Viewer;

        public int PageSize { get; set; } = 500;

        public List<Store> Stores { get; set; } = new List<Store>();

        public List<ExceptionsGroup> Groups { get; set; } = new List<ExceptionsGroup>();

        public JiraSettings Jira { get; set; } = new JiraSettings();

        public List<string> Applications { get; set; } = new List<string>();

        public List<StackTraceSourceLinkPattern> StackTraceReplacements { get; set; } = new List<StackTraceSourceLinkPattern>();

        private StackTraceSettings _stackTraceSettings;
        public StackTraceSettings StackTraceSettings => _stackTraceSettings ??= GetStackTraceSettings();

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
            /// The name of the table exceptions are stored in. "Exceptions" is the default.
            /// </summary>
            public string TableName { get; set; }

            /// <summary>
            /// Whether to include this store in the header total
            /// </summary>
            public bool IncludeInTotal { get; set; } = true;

            /// <summary>
            /// Store specific groups, if not specified then the top level groups will be used.
            /// </summary>
            public List<ExceptionsGroup> Groups { get; set; }

            /// <summary>
            /// Store specific applications, if not specified then the top level applications will be used.
            /// </summary>
            public List<string> Applications { get; set; }

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

        private StackTraceSettings GetStackTraceSettings()
        {
            var result = new StackTraceSettings();
            foreach (var str in StackTraceReplacements)
            {
                if (str.Pattern.HasValue())
                {
                    try
                    {
                        result.AddReplacement(str.Pattern, str.Replacement);
                    }
                    catch (Exception ex)
                    {
                        new Exception($"Unable to parse source link pattern for '{str.Name}': '{str.Pattern}'", ex).Log();
                    }
                }
            }
            return result;
        }

        public class StackTraceSourceLinkPattern : ISettingsCollectionItem
        {
            /// <summary>
            /// The name of the deep link pattern.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// A regular expression for detecting links in stack traces.
            /// Used in conjunction with <see cref="Replacement"/>.
            /// </summary>
            public string Pattern { get; set; }

            /// <summary>
            /// A replacement pattern for rendering links from a <see cref="Pattern"/> match.
            /// matches via <see cref="Regex.Replace(string, string, string)"/>.
            /// </summary>
            public string Replacement { get; set; }
        }
    }
}
