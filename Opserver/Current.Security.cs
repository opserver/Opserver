using StackExchange.Opserver.Models.Security;

namespace StackExchange.Opserver
{
    public partial class Current
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
    }
}