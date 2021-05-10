namespace Opserver.Security
{
    /// <summary>
    /// Security settings for configuring the <see cref="ActiveDirectoryProvider"/>.
    /// </summary>
    public class ActiveDirectorySecuritySettings : SecuritySettings
    {
        /// <summary>
        /// Gets or sets the server to bind to in order to authenticate users.
        /// </summary>
        public string Server { get; set; }
        /// <summary>
        /// Gets or sets the username used to bind to AD in order to authenticate users.
        /// </summary>
        public string AuthUser { get; set; }
        /// <summary>
        /// Gets or sets the password used to bind to AD in order to authenticate users.
        /// </summary>
        public string AuthPassword { get; set; }
    }
}
