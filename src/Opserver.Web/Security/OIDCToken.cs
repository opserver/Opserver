using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Opserver.Security
{
    /// <summary>
    /// <see cref="ISecurityProviderToken"/> that wraps the claims retrieved from an OpenId Connect login flow.
    /// </summary>
    public class OIDCToken : ISecurityProviderToken
    {
        public OIDCToken(IEnumerable<Claim> claims)
        {
            Claims = claims ?? throw new ArgumentNullException(nameof(claims));
        }

        /// <summary>
        /// Gets the claims retrieved as a result of an OpenId Connect login flow.
        /// </summary>
        public IEnumerable<Claim> Claims { get;  }
    }
}
