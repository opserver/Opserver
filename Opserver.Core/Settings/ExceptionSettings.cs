using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver
{
    public class ExceptionsSettings : Settings<ExceptionsSettings>
    {
        public override bool Enabled => Stores.Any();

        public List<Store> Stores { get; set; }

        public List<string> Applications { get; set; }
        
        public ExceptionsSettings()
        {
            // Defaults
            RecentSeconds = 600;
            EnablePreviews = true;
            Applications = new List<string>();
            Stores = new List<Store>();
        }

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
        public int RecentSeconds { get; set; }

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
        public bool EnablePreviews { get; set; }

        public class Store : ISettingsCollectionItem<Store>
        {
            public Store()
            {
                PollIntervalSeconds = 5*60;
            }

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
            public int PollIntervalSeconds { get; set; }

            /// <summary>
            /// Connection string for this store's database
            /// </summary>
            public string ConnectionString { get; set; }

            public bool Equals(Store other)
            {
                return string.Equals(Name, other.Name)
                       && string.Equals(Description, other.Description)
                       && QueryTimeoutMs == other.QueryTimeoutMs
                       && PollIntervalSeconds == other.PollIntervalSeconds
                       && string.Equals(ConnectionString, other.ConnectionString);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((Store) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Name?.GetHashCode() ?? 0;
                    hashCode = (hashCode*397) ^ (Description?.GetHashCode() ?? 0);
                    hashCode = (hashCode*397) ^ QueryTimeoutMs.GetHashCode();
                    hashCode = (hashCode*397) ^ PollIntervalSeconds;
                    hashCode = (hashCode*397) ^ (ConnectionString?.GetHashCode() ?? 0);
                    return hashCode;
                }
            }
        }
    }
}
