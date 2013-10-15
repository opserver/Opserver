using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;

namespace StackExchange.Opserver
{
    public class SecuritySettings : ConfigurationSection
    {
        private static readonly SecuritySettings _settings = ConfigurationManager.GetSection("SecuritySettings") as SecuritySettings;
        public static SecuritySettings Current { get { return _settings; } }

        public bool Enabled { get { return Provider.HasValue(); } }

        /// <summary>
        /// Security Provider to use, e.g. "ActiveDirectory"
        /// </summary>
        [ConfigurationProperty("provider")]
        public string Provider { get { return this["provider"] as string ?? ""; } }

        [ConfigurationProperty("server")]
        public string Server { get { return this["server"] as string ?? ""; } }

        [ConfigurationProperty("authUser"), DefaultValue("")]
        public string AuthUser { get { return this["authUser"] as string ?? ""; } }

        [ConfigurationProperty("authPassword"), DefaultValue("")]
        public string AuthPassword { get { return this["authPassword"] as string ?? ""; } }

        [ConfigurationProperty("InternalNetworks")]
        public SettingsCollection<Network> InternalNetworks
        {
            get
            {
                return this["InternalNetworks"] as SettingsCollection<Network>;
            }
        }

        public class Network : ConfigurationElement, ISettingsElementNamed
        {
            /// <summary>
            /// The name for this network
            /// </summary>
            [ConfigurationProperty("name", IsRequired = true)]
            public string Name { get { return this["name"] as string; } }

            /// <summary>
            /// The CIDR notation for this node
            /// </summary>
            [ConfigurationProperty("cidr")]
            public string CIDR { get { return this["cidr"] as string; } }

            /// <summary>
            /// The IP for this network, optionally used with Mask
            /// </summary>
            [ConfigurationProperty("ip")]
            public string IP { get { return this["ip"] as string; } }

            /// <summary>
            /// The subnet mask for this node
            /// </summary>
            [ConfigurationProperty("subnet")]
            public string Subnet { get { return this["subnet"] as string; } }
        }

        public class SettingsCollection<T> : ConfigurationElementCollection where T : ConfigurationElement, ISettingsElementNamed, new()
        {
            public virtual string Name
            {
                get { return typeof(T).Name; }
            }
            public T this[int index]
            {
                get { return BaseGet(index) as T; }
            }
            protected override ConfigurationElement CreateNewElement()
            {
                return new T();
            }

            protected override object GetElementKey(ConfigurationElement element)
            {
                return ((T)element).Name;
            }

            public void Add(T item)
            {
                BaseAdd(item);
            }

            public override ConfigurationElementCollectionType CollectionType
            {
                get { return ConfigurationElementCollectionType.BasicMapAlternate; }
            }

            protected override string ElementName
            {
                get { return typeof(T).Name; }
            }

            protected override bool IsElementName(string elementName)
            {
                return (elementName == typeof(T).Name);
            }

            public bool Any()
            {
                return Count > 0;
            }

            private List<T> _all;
            public List<T> All
            {
                get { return _all ?? (_all = this.Cast<T>().ToList()); }
            }
        }

        public interface ISettingsElementNamed
        {
            string Name { get; }
        }
    }
}