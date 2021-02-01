using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Opserver.Models;

namespace Opserver.Security
{
    /// <summary>
    /// <see cref="SecurityProvider"/> that delegates login to an OIDC login flow.
    /// </summary>
    public class OIDCProvider : SecurityProvider<OIDCSecuritySettings, OIDCToken>
    {
        public const string GroupsClaimType = "groups";

        public override string ProviderName => "OpenId Connect" +
                                               "";
        public override SecurityProviderFlowType FlowType => SecurityProviderFlowType.OIDC;
        private HashSet<string> GroupNames { get; } = new HashSet<string>();

        public OIDCProvider(OIDCSecuritySettings settings) : base(settings)
        {
        }

        protected override bool TryValidateToken(OIDCToken token, out ClaimsPrincipal claimsPrincipal)
        {
            try
            {
                // NOTE: we do not need to validate here - we get this
                // JWT as part of an authorization token exchange from the provider
                // which is contacted over a TLS transport so it is from a trusted source.
                // here we just parse the JWT and generate a ClaimsPrincipal to return
                // to the caller
                var jwtHandler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwt;
                try
                {
                    jwt = jwtHandler.ReadJwtToken(token.IdToken);
                }
                catch (Exception ex)
                {
                    ex.Log();
                    claimsPrincipal = CreateAnonymousPrincipal();
                    return false;
                }

                // extract the claims we care about
                var claimsToAdd = new List<Claim>();
                foreach (var claim in jwt.Claims)
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
            catch (SecurityTokenException ex)
            {
                ex.Log();
                claimsPrincipal = CreateAnonymousPrincipal();
                throw;
                //return false;
            }
        }

        protected override bool InGroupsCore(User user, string[] groupNames)
        {
            var groupClaims = user.Principal.FindAll(x => x.Type == GroupsClaimType);
            foreach (var groupClaim in groupClaims)
            {
                if (groupNames.Any(x => string.Equals(groupClaim.Value, x, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
