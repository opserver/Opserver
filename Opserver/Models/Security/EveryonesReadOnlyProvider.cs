namespace StackExchange.Opserver.Models.Security
{
    /// <summary>
    /// Does this REALLY need an explanation?
    /// </summary>
    public class EveryonesReadOnlyProvider : SecurityProvider
    {
        public override bool IsAdmin => false;

        internal override bool InReadGroups(ISecurableSection settings) { return true; }
        public override bool InGroups(string groupNames, string accountName) { return true; }
        public override bool ValidateUser(string userName, string password) { return true; }
    }
}