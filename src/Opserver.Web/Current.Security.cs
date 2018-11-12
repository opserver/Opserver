using StackExchange.Opserver.Models;
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

        public static bool IsInRole(Roles roles)
        {
            if (User == null)
            {
                return (RequestRoles & roles) != 0;
            }
            return ((User.Role | RequestRoles) & roles) != Roles.None || (User.Role & Roles.GlobalAdmin) != 0;
        }

        public static Roles RequestRoles
        {
            get
            {
                var roles = Context.Items[nameof(RequestRoles)];
                if (roles != null) return (Roles)roles;

                var result = Roles.None;
                if (Context.Connection.IsLocal) result |= Roles.LocalRequest;
                if (Security.IsInternalIP(RequestIP)) result |= Roles.InternalRequest;
                if (IsValidApiRequest()) result |= Roles.ApiRequest;

                Context.Items[nameof(RequestRoles)] = result;
                return result;
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
