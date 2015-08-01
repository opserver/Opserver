namespace StackExchange.Opserver.Models.Security
{
    /// <summary>
    /// Does this REALLY need an explanation?
    /// </summary>
    public class EveryonesAnAdminProvider : SecurityProvider
    {
        public override bool IsAdmin => true;

        internal override bool InAdminGroups(ISecurableSection settings) { return true; }
        public override bool InGroups(string groupNames, string accountName) { return true; }
        public override bool ValidateUser(string userName, string password) { return true; }
    }
}