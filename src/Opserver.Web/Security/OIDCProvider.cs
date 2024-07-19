using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Opserver.Models;

namespace Opserver.Security
{
    /// <summary>
    /// <see cref="SecurityProvider"/> that delegates login to an OIDC login flow.
    /// </summary>
    public class OIDCProvider : SecurityProvider<OIDCSecuritySettings, OIDCToken>
    {
        public const string GroupsClaimType = "groups";
        public override string ProviderName => "OpenID Connect";
        public override SecurityProviderFlowType FlowType => SecurityProviderFlowType.OIDC;

        public OIDCProvider(OIDCSecuritySettings settings) : base(settings)
        {
        }

        protected override bool TryValidateToken(OIDCToken token, out ClaimsPrincipal claimsPrincipal)
        {
            // extract the claims we care about
            var claimsToAdd = new List<Claim>();
            foreach (var claim in token.Claims)
            {
                if (string.Equals(claim.Type, Settings.NameClaim, StringComparison.OrdinalIgnoreCase))
                {
                    claimsToAdd.Add(new Claim(ClaimTypes.Name, claim.Value));
                }
                else if (string.Equals(claim.Type, Settings.GroupsClaim, StringComparison.OrdinalIgnoreCase))
                {
                    claimsToAdd.Add(new Claim(GroupsClaimType, claim.Value));
                }
            }
            claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claimsToAdd, "login"));
            return true;
        }

        protected override bool InGroupsCore(User user, string[] groupNames)
        {
            var groupClaims = user.Principal.FindAll(x => x.Type == GroupsClaimType);
            var groupClaimValues = groupClaims.Select(x => x.Value).ToArray();
            Console.WriteLine("Checking if user is in groups {0}.  User is in these groups: [{1}]",
                string.Join(", ", groupNames),
                string.Join(", ", groupClaimValues));
            foreach (var groupClaim in groupClaimValues)
            {
                if (groupNames.Any(x => string.Equals(groupClaim, x, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("User is in group {0}", groupClaim);
                    return true;
                }
            }

            return false;
        }
    }
}
