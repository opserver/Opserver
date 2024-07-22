namespace Opserver.Security
{
    /// <summary>
    /// Security settings specific to the OIDC provider.
    /// </summary>
    public class OIDCSecuritySettings : SecuritySettings
    {
        public static readonly string[] DefaultScopes = {"openid"};

        /// <summary>
        /// Gets or sets the client id for the OIDC provider.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the client secret for the OIDC provider.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the URL used to obtain an authorization code from the OIDC provider.
        /// </summary>
        public string AuthorizationUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL used to obtain an access token / ID token from the OIDC provider.
        /// </summary>
        public string AccessTokenUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL used to obtain user info from the OIDC provider.
        /// </summary>
        public string UserInfoUrl { get; set; }

        /// <summary>
        /// Gets or sets a list of scopes to request from the OIDC provider.
        /// </summary>
        public string[] Scopes { get; set; } = DefaultScopes;

        /// <summary>
        /// Gets or sets the name of the "name" claim.
        /// </summary>
        public string NameClaim { get; set; } = "nameIdentifier";

        /// <summary>
        /// Gets or sets the name of the "name" claim.
        /// </summary>
        public string GroupsClaim { get; set; } = "groups";

        /// <summary>
        /// When redirecting to an OIDC provider, whether to always use https for the redirect/referral.
        /// </summary>
        public bool UseHttpsForRedirects { get; set; } = false;
    }
}
