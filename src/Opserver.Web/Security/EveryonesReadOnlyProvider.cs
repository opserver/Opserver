using System.Security.Claims;
using Opserver.Models;

namespace Opserver.Security
{
    /// <summary>
    /// Does this REALLY need an explanation?
    /// </summary>
    public class EveryonesReadOnlyProvider : SecurityProvider<SecuritySettings, UserNamePasswordToken>
    {
        public override string ProviderName => "Everyone's Read-only";
        public override SecurityProviderFlowType FlowType => SecurityProviderFlowType.Username;
        public EveryonesReadOnlyProvider(SecuritySettings settings) : base(settings) { }

        internal override bool InAdminGroups(User user, StatusModule module) => false;
        internal override bool InReadGroups(User user, StatusModule module) => true;
        protected override bool TryValidateToken(UserNamePasswordToken token, out ClaimsPrincipal claimsPrincipal)
        {
            claimsPrincipal = CreateNamedPrincipal(token.UserName);
            return true;
        }
    }
}
