using System.ComponentModel;
using System.Configuration;

namespace StackExchange.Opserver
{
    public class SettingsSection : ConfigurationSection
    {
        [ConfigurationProperty("provider"), DefaultValue("JSONFile")]
        public string Provider => this["provider"] as string;

        [ConfigurationProperty("name")]
        public string Name => this["name"] as string;

        [ConfigurationProperty("path")]
        public string Path => this["path"] as string;

        [ConfigurationProperty("connectionString")]
        public string ConnectionString => this["connectionString"] as string;
    }
}