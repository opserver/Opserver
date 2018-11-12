namespace StackExchange.Opserver.Models.Security
{
    public static class SecuritySettingsExtensions
    {
        public static bool HasAccess(this ISecurableModule settings)
        {
            return Current.Security.InReadGroups(settings);
        }

        public static bool IsAdmin(this ISecurableModule settings)
        {
            return Current.Security.InAdminGroups(settings);
        }
    }
}