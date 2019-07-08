using System;
using Microsoft.Extensions.Options;

namespace StackExchange.Opserver.Security
{
    public class SecurityManager
    {
        public SecurityProvider CurrentProvider { get; }

        public SecurityManager(IOptions<SecuritySettings> settings)
        {
            _ = settings?.Value ?? throw new ArgumentNullException(nameof(settings), "SecuritySettings must be provided");
            CurrentProvider = GetProvider(settings.Value);
        }

        private SecurityProvider GetProvider(SecuritySettings settings)
        {
            switch (settings.Provider)
            {
                case "ActiveDirectory":
                    return new ActiveDirectoryProvider(settings);
                case "EveryonesAnAdmin":
                    return new EveryonesAnAdminProvider(settings);
                //case "EveryonesReadOnly":
                default:
                    return new EveryonesReadOnlyProvider(settings);
            }
        }
    }
}
