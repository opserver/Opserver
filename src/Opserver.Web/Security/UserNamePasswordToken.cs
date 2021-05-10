namespace Opserver.Security
{
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
