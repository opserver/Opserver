using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Data.Dashboard;

namespace StackExchange.Opserver
{
    public abstract class StatusModule<T> : StatusModule where T : ModuleSettings, new()
    {
        private readonly IOptions<T> _settings;
        public T Settings => _settings.Value;
        public override ISecurableModule SecuritySettings => Settings;
        protected StatusModule(IOptions<T> settings)
        {
            _settings = settings;
        }
    }

    /// <summary>
    /// The base of all plugins on the data side
    /// </summary>
    public abstract class StatusModule
    {
        // TODO: Top tab registration
        public abstract string Name { get; }
        public abstract bool Enabled { get; }
        public abstract MonitorStatus MonitorStatus { get; }
        public abstract bool IsMember(string node);
        public bool IsMember(Node node) => IsMember(node.PrettyName);
        public abstract ISecurableModule SecuritySettings { get; }
    }

    public static class StatusModuleExtensions
    {
        public static IServiceCollection AddStatusModules(this IServiceCollection services, IConfiguration _configuration)
        {
            void Add<TSettings, TModule>(string settingsSection) where TSettings : ModuleSettings where TModule : StatusModule
            {
                services.Configure<TSettings>(_configuration.GetSection(settingsSection))   // Add settings
                        .AddSingleton<TModule>()                                            // Actual Singleton
                        .AddSingleton<StatusModule, TModule>(x => x.GetService<TModule>()); // For IEnumerable discovery
            }

            // TODO: Discovery instead
            Add<DashboardSettings, DashboardModule>("Dashboard");
            Add<SQLSettings, Data.SQL.SQLModule>("SQL");
            Add<RedisSettings, Data.Redis.RedisModule>("Redis");
            Add<ElasticSettings, Data.Elastic.ElasticModule>("Elastic");
            Add<PagerDutySettings, Data.PagerDuty.PagerDutyModule>("PagerDuty");
            Add<ExceptionsSettings, Data.Exceptions.ExceptionsModule>("Exceptions");
            Add<HAProxySettings, Data.HAProxy.HAProxyModule>("HAProxy");
            //Add<CloudFlareSettings, Data.CloudFlare.CloudFlareModule>("CloudFlare");

            return services;
        }
    }
}
