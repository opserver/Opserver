using System;

namespace Opserver.Security
{
    /// <summary>
    /// <see cref="ISecurityProviderToken"/> that wraps the ID token that resulted from a successful OpenId Connect login flow.
    /// </summary>
    public class OIDCToken : ISecurityProviderToken
    {
        public OIDCToken(string idToken)
        {
            IdToken = idToken ?? throw new ArgumentNullException(nameof(idToken));
        }

        /// <summary>
        /// Gets the ID token retrieved as a result of an OpenId Connect login flow.
        /// </summary>
        public string IdToken { get;  }
    }
}
