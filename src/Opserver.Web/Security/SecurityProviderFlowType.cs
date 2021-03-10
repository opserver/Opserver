namespace Opserver.Security
{
    /// <summary>
    /// Types of login flows supported by a <see cref="SecurityProvider" />.
    /// </summary>
    public enum SecurityProviderFlowType
    {
        None,
        Username,
        UsernamePassword,
        OIDC,
    }
}
