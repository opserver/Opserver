using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Opserver.Data;
using Opserver.Data.Dashboard;

namespace Opserver
{
    public abstract class StatusModule<T> : StatusModule where T : ModuleSettings, new()
    {
        private readonly IOptions<T> _settings;
        public T Settings => _settings.Value;
        public override ISecurableModule SecuritySettings => Settings;
        protected StatusModule(IConfiguration config, PollingService poller) : base(poller)
        {
            // TODO: See if there's a way to get back to a proper reloadable IOptions here?
            // To be fair, it needs proper stpport down the whole module tree, e.g. which instances are created and such
            _settings = Options.Create(config.GetSection("Modules").GetSection(typeof(T).Name.Replace("Settings", "")).Get<T>() ?? new T());
        }
        protected StatusModule(IOptions<T> settings, PollingService poller) : base(poller)
        {
            _settings = settings;
        }
    }

    /// <summary>
    /// The base of all plugins on the data side
    /// </summary>
    public abstract class StatusModule
    {
        public abstract string Name { get; }
        public abstract bool Enabled { get; }
        public abstract MonitorStatus MonitorStatus { get; }
        public abstract bool IsMember(string node);
        public bool IsMember(Node node) => IsMember(node.PrettyName);
        public abstract ISecurableModule SecuritySettings { get; }
        public PollingService Poller { get; }

        protected StatusModule(PollingService poller) => Poller = poller;
    }

    public static class StatusModuleExtensions
    {
        public static IServiceCollection AddStatusModules(this IServiceCollection services)
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.DefinedTypes).Where(t => !t.IsAbstract);
            // Register all modules
            foreach (var type in allTypes.Where(typeof(StatusModule).IsAssignableFrom))
            {
                services.Add(new ServiceDescriptor(type, type, ServiceLifetime.Singleton));
                services.Add(new ServiceDescriptor(typeof(StatusModule), x => x.GetService(type), ServiceLifetime.Singleton));
            }

            return services;
        }
    }
}
