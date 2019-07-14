using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Opserver.Security
{
    public class SecurityManager
    {
        public SecurityProvider CurrentProvider { get; }

        public SecurityManager(IOptions<SecuritySettings> settings, IMemoryCache cache)
        {
            _ = settings?.Value ?? throw new ArgumentNullException(nameof(settings), "SecuritySettings must be provided");
            CurrentProvider = GetProvider(settings.Value, cache);
        }

        private SecurityProvider GetProvider(SecuritySettings settings, IMemoryCache cache)
        {
            switch (settings.Provider)
            {
                case "ActiveDirectory":
                    return new ActiveDirectoryProvider(settings, cache);
                case "EveryonesAnAdmin":
                    return new EveryonesAnAdminProvider(settings);
                //case "EveryonesReadOnly":
                default:
                    return new EveryonesReadOnlyProvider(settings);
            }
        }
    }
}
