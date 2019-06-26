using StackExchange.Opserver.Models.Security;

namespace StackExchange.Opserver
{
    public static partial class Current
    {
        private static SecurityProvider _security;
        public static SecurityProvider Security => _security ?? (_security = GetSecurityProvider());

        private static SecurityProvider GetSecurityProvider()
        {
            if (SecuritySettings.Current == null || !SecuritySettings.Current.Enabled)
                return new EveryonesReadOnlyProvider();
            switch (SecuritySettings.Current.Provider.ToLowerInvariant())
            {
                case "activedirectory":
                case "ad":
                    return new ActiveDirectoryProvider(SecuritySettings.Current);
                case "alladmin":
                    return new EveryonesAnAdminProvider();
                //case "allview":
                default:
                    return new EveryonesReadOnlyProvider();
            }
        }

        public static string ApiKey { get; } = SecuritySettings.Current?.ApiKey;

        public static bool IsValidApiRequest()
        {
            // Only treat this as valid if there is an API key set at all
            return ApiKey.HasValue() && string.Equals(ApiKey, Request?.Query["key"]);
        }
    }
}
