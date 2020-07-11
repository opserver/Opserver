using Opserver.Models;

namespace Opserver.Security
{
    /// <summary>
    /// Does this REALLY need an explanation?
    /// </summary>
    public class EveryonesReadOnlyProvider : SecurityProvider
    {
        public override string ProviderName => "Everyone's Read-only";
        public EveryonesReadOnlyProvider(SecuritySettings settings) : base(settings) { }

        internal override bool InAdminGroups(User user, StatusModule module) => false;
        internal override bool InReadGroups(User user, StatusModule module) => true;
        public override bool ValidateUser(string userName, string password) => true;
    }
}
