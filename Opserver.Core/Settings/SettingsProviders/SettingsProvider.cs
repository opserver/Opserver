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
        public PagerdutySettings Pagerduty { get { return GetSettings<PagerdutySettings>(); } }
        public CloudFlareSettings CloudFlare { get { return GetSettings<CloudFlareSettings>(); } }
        public DashboardSettings Dashboard { get { return GetSettings<DashboardSettings>(); } }
        public ElasticSettings Elastic { get { return GetSettings<ElasticSettings>(); } }
        public ExceptionsSettings Exceptions { get { return GetSettings<ExceptionsSettings>(); } }
        public HAProxySettings HAProxy { get { return GetSettings<HAProxySettings>(); } }
        public PollingSettings Polling { get { return GetSettings<PollingSettings>(); } }
        public RedisSettings Redis { get { return GetSettings<RedisSettings>(); } }
        public SQLSettings SQL { get { return GetSettings<SQLSettings>(); } }
        // Generic build settings later
        public TeamCitySettings TeamCity { get { return GetSettings<TeamCitySettings>(); } }

        public abstract T GetSettings<T>() where T : Settings<T>, new();
        public abstract T SaveSettings<T>(T settings) where T : class, new();

        protected SettingsProvider(SettingsSection settings)
        {
            Name = settings.Name;
            Path = settings.Path;
            ConnectionString = settings.ConnectionString;
        }

        private static SettingsProvider _current;
        public static SettingsProvider Current { get { return _current ?? (_current = GetCurrentProvider()); } }
        
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
                throw new ConfigurationErrorsException(string.Format("Could not resolve type '{0}' ('{1}')", section.Provider, provider));
            }
            var p = (SettingsProvider) Activator.CreateInstance(t, section);
            return p;
        }
    }
}
