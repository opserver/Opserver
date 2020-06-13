using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Opserver.Security
{
    public class SecurityManager
    {
        public SecurityProvider CurrentProvider { get; }
        public bool IsConfigured => CurrentProvider != null && !(CurrentProvider is UnconfiguredProvider);

        public SecurityManager(IOptions<SecuritySettings> settings, IMemoryCache cache)
        {
            _ = settings?.Value ?? throw new ArgumentNullException(nameof(settings), "SecuritySettings must be provided");
            CurrentProvider = GetProvider(settings.Value, cache);
        }

        private SecurityProvider GetProvider(SecuritySettings settings, IMemoryCache cache) =>
            settings.Provider switch
            {
                "AD" => new ActiveDirectoryProvider(settings, cache),
                "ActiveDirectory" => new ActiveDirectoryProvider(settings, cache),
                "EveryonesAnAdmin" => new EveryonesAnAdminProvider(settings),
                "EveryonesReadOnly" => new EveryonesReadOnlyProvider(settings),
                _ => new UnconfiguredProvider(settings)
            };
    }
}
