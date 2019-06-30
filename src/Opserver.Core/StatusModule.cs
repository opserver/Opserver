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
            // TODO: Discovery instead
            services.Configure<DashboardSettings>(_configuration.GetSection("Dashboard"))
                    .AddSingleton<DashboardModule>();

            services.Configure<SQLSettings>(_configuration.GetSection("SQL"))
                    .AddSingleton<Data.SQL.SQLModule>();

            services.Configure<RedisSettings>(_configuration.GetSection("Redis"))
                    .AddSingleton<Data.Redis.RedisModule>();

            services.Configure<ElasticSettings>(_configuration.GetSection("Elastic"))
                    .AddSingleton<Data.Elastic.ElasticModule>();

            services.Configure<PagerDutySettings>(_configuration.GetSection("PagerDuty"))
                    .AddSingleton<Data.PagerDuty.PagerDutyModule>();

            services.Configure<ExceptionsSettings>(_configuration.GetSection("Exceptions"))
                    .AddSingleton<Data.Exceptions.ExceptionsModule>();

            services.Configure<HAProxySettings>(_configuration.GetSection("HAProxy"))
                    .AddSingleton<Data.HAProxy.HAProxyModule>();

            //services.Configure<CloudFlareSettings>(_configuration.GetSection("CloudFlare"));
            //services.AddSingleton<Data.CloudFlare.CloudFlareModule>();
            return services;
        }
    }
}
