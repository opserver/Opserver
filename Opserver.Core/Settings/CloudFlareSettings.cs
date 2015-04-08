using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StackExchange.Opserver
{
    public class CloudFlareSettings : Settings<CloudFlareSettings>, IAfterLoadActions
    {
        public override bool Enabled { get { return (Email.HasValue() && APIKey.HasValue()) || Railguns.Any(); } }
        public ObservableCollection<Railgun> Railguns { get; set; }
        public event EventHandler<Railgun> RailgunAdded = delegate { };
        public event EventHandler<List<Railgun>> RailgunChanged = delegate { };
        public event EventHandler<Railgun> RailgunRemoved = delegate { };
        public ObservableCollection<DataCenter> DataCenters { get; set; }
        public event EventHandler<DataCenter> DataCenterAdded = delegate { };
        public event EventHandler<List<DataCenter>> DataCenterChanged = delegate { };
        public event EventHandler<DataCenter> DataCenterRemoved = delegate { };

        /// <summary>
        /// Email for the CloudFlare account
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// APIKey for the CloudFlare account
        /// </summary>
        public string APIKey { get; set; }

        public CloudFlareSettings()
        {
            Railguns = new ObservableCollection<Railgun>();
            DataCenters = new ObservableCollection<DataCenter>();
        }

        public void AfterLoad()
        {
            Railguns.AddHandlers(this, RailgunAdded, RailgunChanged, RailgunRemoved);
            DataCenters.AddHandlers(this, DataCenterAdded, DataCenterChanged, DataCenterRemoved);
        }

        public class Railgun : ISettingsCollectionItem<Railgun>
        {
            public Railgun()
            {
                // Defaults
                RefreshIntervalSeconds = 30;
                Port = 24088;
            }

            /// <summary>
            /// The name of this railgun instance
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Host for this instance
            /// </summary>
            public string Host { get; set; }

            /// <summary>
            /// Connection port for this instance
            /// </summary>
            public int Port { get; set; }

            /// <summary>
            /// Unused currently
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// How many seconds before polling the railgun status endpoint for status again
            /// </summary>
            public int RefreshIntervalSeconds { get; set; }

            public bool Equals(Railgun other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Name, other.Name)
                    && string.Equals(Host, other.Host)
                    && string.Equals(Description, other.Description)
                    && Port == other.Port
                    && RefreshIntervalSeconds == other.RefreshIntervalSeconds;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Railgun)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = 0;
                    hashCode = (hashCode*397) ^ (Name != null ? Name.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Host != null ? Host.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (Description != null ? Description.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ RefreshIntervalSeconds;
                    hashCode = (hashCode*397) ^ Port;
                    return hashCode;
                }
            }
        }



        public class DataCenter : ISettingsCollectionItem<DataCenter>
        {
            /// <summary>
            /// The name for this data center
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The IP ranges for this data center, in CIDR format
            /// </summary>
            public List<string> Ranges { get; set; }

            /// <summary>
            /// The masked IP ranges for this data center, in CIDR format
            /// </summary>
            public List<string> MaskedRanges { get; set; }

            public DataCenter()
            {
                Ranges = new List<string>();
                MaskedRanges = new List<string>();
            }

            public bool Equals(DataCenter other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Name, other.Name)
                       && Ranges.SequenceEqual(other.Ranges)
                       && MaskedRanges.SequenceEqual(other.MaskedRanges);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((DataCenter)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = 0;
                    foreach (var r in Ranges)
                        hashCode = (hashCode * 397) ^ r.GetHashCode();
                    hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }
    }
}
