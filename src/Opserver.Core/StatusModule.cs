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
        // TODO: Top tab registration, but that needs monitor status...
        public abstract bool Enabled { get; }
        public abstract bool IsMember(string node);
        public abstract MonitorStatus MonitorStatus { get; }
    }

    public static class StatusModuleExtensions
    {
        public static bool IsMember(this StatusModule module, Node node) =>
            module.IsMember(node.PrettyName);

        public static IServiceCollection AddStatusModules(this IServiceCollection services)
        {
            // TODO: Discovery instead
            services.AddSingleton<DashboardModule>();
            services.AddSingleton<Data.SQL.SQLModule>();
            services.AddSingleton<Data.Redis.RedisModule>();
            services.AddSingleton<Data.Elastic.ElasticModule>();
            services.AddSingleton<Data.PagerDuty.PagerDutyModule>();
            services.AddSingleton<Data.Exceptions.ExceptionsModule>();
            services.AddSingleton<Data.HAProxy.HAProxyModule>();
            //services.AddSingleton<Data.CloudFlare.CloudFlareModule>();
            return services;
        }
    }
}
