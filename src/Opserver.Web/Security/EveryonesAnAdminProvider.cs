using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Security
{
    /// <summary>
    /// Does this REALLY need an explanation?
    /// </summary>
    public class EveryonesAnAdminProvider : SecurityProvider
    {
        public override string ProviderName => "Everyone's an Admin!";
        public EveryonesAnAdminProvider(SecuritySettings settings) : base(settings) { }

        public override bool InGroups(User user, string groupNames) => true;
        public override bool ValidateUser(string userName, string password) => true;
    }
}
