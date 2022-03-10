using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Opserver.Security
{
    public class SecurityManager
    {
        public SecurityProvider CurrentProvider { get; }
        public bool IsConfigured => CurrentProvider != null && !(CurrentProvider is UnconfiguredProvider);

        /// <summary>
        /// Instantiates a new instane of <see cref="SecurityManager"/>.
        /// </summary>
        /// <remarks>
        /// Yes, requiring a <see cref="IServiceProvider"/> here is an anti-pattern, but
        /// the options / configuration framework does not support polymorphic binding,
        /// so we're stuck with this less than optimal approach. We've registered our
        /// various implementations of <see cref="SecuritySettings"/> with the options
        /// framework so we can resolve the concrete types in <see cref="GetProvider"/>
        /// </remarks>
        public SecurityManager(IServiceProvider serviceProvider, IMemoryCache cache)
        {
            _ = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _ = cache ?? throw new ArgumentNullException(nameof(cache));
            CurrentProvider = GetProvider(serviceProvider, cache);
        }

        private static SecurityProvider GetProvider(IServiceProvider serviceProvider, IMemoryCache cache)
        {
            TSettings GetSettings<TSettings>() where TSettings : SecuritySettings, new()
                => serviceProvider.GetRequiredService<IOptions<TSettings>>().Value;

            var baseSettings = GetSettings<SecuritySettings>();
            return baseSettings.Provider switch
            {
                "AD" when OperatingSystem.IsWindows() => new ActiveDirectoryProvider(GetSettings<ActiveDirectorySecuritySettings>(), cache),
                "ActiveDirectory" when OperatingSystem.IsWindows() => new ActiveDirectoryProvider(GetSettings<ActiveDirectorySecuritySettings>(), cache),
                "EveryonesAnAdmin" => new EveryonesAnAdminProvider(baseSettings),
                "EveryonesReadOnly" => new EveryonesReadOnlyProvider(baseSettings),
                "OIDC" => new OIDCProvider(GetSettings<OIDCSecuritySettings>()),
                _ => new UnconfiguredProvider(baseSettings)
            };
        }
    }
}
