using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public class ExceptionsSettings : Settings<ExceptionsSettings>
    {
        public override bool Enabled => Stores.Any();

        public List<Store> Stores { get; set; } = new List<Store>();

        public List<ExceptionsGroup> Groups { get; set; } = new List<ExceptionsGroup>();

        public List<string> Applications { get; set; } = new List<string>();
        
        /// <summary>
        /// How many exceptions before the exceptions are highlighted as a warning in the header, 0 (default) is ignored
        /// </summary>
        public int WarningCount { get; set; }

        /// <summary>
        /// How many exceptions before the exceptions are highlighted as critical in the header, 0 (default) is ignored
        /// </summary>
        public int CriticalCount { get; set; }

        /// <summary>
        /// How many seconds a error is considered "recent"
        /// </summary>
        public int RecentSeconds { get; set; } = 600;

        /// <summary>
        /// How many recent exceptions before the exceptions are highlighted as a warning in the header, 0 (default) is ignored
        /// </summary>
        public int WarningRecentCount { get; set; }

        /// <summary>
        /// How many recent exceptions before the exceptions are highlighted as critical in the header, 0 (default) is ignored
        /// </summary>
        public int CriticalRecentCount { get; set; }

        /// <summary>
        /// Default maximum timeout in milliseconds before giving up on an sources
        /// </summary>
        public bool EnablePreviews { get; set; } = true;

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
    }
}
