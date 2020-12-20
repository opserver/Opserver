namespace Opserver.Security
{
    /// <summary>
    /// Marker interface used to indicate a class is a token consumed by a <see cref="SecurityProvider"/>.
    /// </summary>
    public interface ISecurityProviderToken
    {
    }

    public class UserNamePasswordToken : ISecurityProviderToken
    {
        public UserNamePasswordToken(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        public string UserName { get; }
        public string Password { get; }
    }
}
