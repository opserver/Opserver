using System;
using System.Configuration;

namespace StackExchange.Opserver.SettingsProviders
{
    public abstract class SettingsProvider
    {
        public abstract string ProviderType { get; }

        public string Name { get; set; }
        public string Path { get; set; }
        public string ConnectionString { get; set; }

        // Accessors for built-in types
        public PagerDutySettings PagerDuty => GetSettings<PagerDutySettings>();
        public CloudFlareSettings CloudFlare => GetSettings<CloudFlareSettings>();
        public DashboardSettings Dashboard => GetSettings<DashboardSettings>();
        public ElasticSettings Elastic => GetSettings<ElasticSettings>();
        public ExceptionsSettings Exceptions => GetSettings<ExceptionsSettings>();
        public HAProxySettings HAProxy => GetSettings<HAProxySettings>();
        public PollingSettings Polling => GetSettings<PollingSettings>();
        public RedisSettings Redis => GetSettings<RedisSettings>();
        public SQLSettings SQL => GetSettings<SQLSettings>();
        // Generic build settings later
        public TeamCitySettings TeamCity => GetSettings<TeamCitySettings>();
        public JiraSettings Jira => GetSettings<JiraSettings>();

        public abstract T GetSettings<T>() where T : Settings<T>, new();
        public abstract T SaveSettings<T>(T settings) where T : class, new();

        protected SettingsProvider(SettingsSection settings)
        {
            Name = settings.Name;
            Path = settings.Path;
            ConnectionString = settings.ConnectionString;
        }

        private static SettingsProvider _current;
        public static SettingsProvider Current => _current ?? (_current = GetCurrentProvider());

        public static SettingsProvider GetCurrentProvider()
        {
            var section = ConfigurationManager.GetSection("Settings") as SettingsSection;
            if (section == null)
            {
                throw new ConfigurationErrorsException("No settings section found in the config");
            }
            var provider = section.Provider;
            if (!provider.EndsWith("SettingsProvider")) provider += "SettingsProvider";
            if (!provider.Contains(".")) provider = "StackExchange.Opserver.SettingsProviders." + provider;

            var t = Type.GetType(provider, false);
            if (t == null)
            {
                throw new ConfigurationErrorsException($"Could not resolve type '{section.Provider}' ('{provider}')");
            }
            var p = (SettingsProvider) Activator.CreateInstance(t, section);
            return p;
        }
    }
}
