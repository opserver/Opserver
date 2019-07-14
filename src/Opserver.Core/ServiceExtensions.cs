using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Opserver.Data;
using Opserver.Helpers;

namespace Opserver
{
    public static class ServiceExtensions
    {
        /// <summary>
        /// Registers all core Opserver services needed for data and polling.
        /// </summary>
        /// <param name="services">The service collection to add to.</param>
        /// <param name="_configuration">The configuration</param>
        /// <returns>The <see cref="IServiceCollection" /> for chaining.</returns>
        public static IServiceCollection AddCoreOpserverServices(this IServiceCollection services, IConfiguration _configuration)
        {
            // Configure top level settings
            services.Configure<OpserverSettings>(_configuration);
            // Register IOptions<T> version of settings
            services.AddTransient(s => s.GetRequiredService<IOptions<OpserverSettings>>().Value);

            // Register our address cache for DNS/IP lookups
            services.AddSingleton<AddressCache>();

            // Add the polling service as a concrete singleton and as a IHostedService so it starts, etc.
            services.AddSingleton<PollingService>()
                    .AddSingleton<IHostedService>(x => x.GetRequiredService<PollingService>());

            // Register all the modules
            services.AddStatusModules(_configuration);

            return services;
        }
    }
}
