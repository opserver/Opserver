using System.Collections.Generic;
using System.Security.Claims;
using Opserver.Models;

namespace Opserver.Security
{
    /// <summary>
    /// Does this REALLY need an explanation?
    /// </summary>
    public class EveryonesAnAdminProvider : SecurityProvider<SecuritySettings, UserNamePasswordToken>
    {
        public override string ProviderName => "Everyone's an Admin!";
        public override SecurityProviderFlowType FlowType => SecurityProviderFlowType.Username;
        public EveryonesAnAdminProvider(SecuritySettings settings) : base(settings) { }

        protected override bool InGroupsCore(User user, string[] groupNames) => true;

        protected override bool TryValidateToken(UserNamePasswordToken token, out ClaimsPrincipal claimsPrincipal)
        {
            claimsPrincipal = CreateNamedPrincipal(token.UserName);
            return true;
        }
    }
}
