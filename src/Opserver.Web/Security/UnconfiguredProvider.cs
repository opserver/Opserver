using Opserver.Models;

namespace Opserver.Security
{
    /// <summary>
    /// Used when no valid configuration is present, as a default to slam the door
    /// If people want read-all access...it should be explicit. Default is no access.
    /// </summary>
    public class UnconfiguredProvider : SecurityProvider
    {
        public override string ProviderName => "Unconfigured";
        public override bool IsConfigured => false;
        public UnconfiguredProvider(SecuritySettings settings) : base(settings) { }

        public override bool InGroups(User user, string groupNames) => false;
        public override bool ValidateUser(string userName, string password) => false;
    }
}
