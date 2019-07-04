namespace StackExchange.Opserver
{
    public interface ISecurableModule
    {
        bool Enabled { get; }
        // TODO: List<string>
        string ViewGroups { get; }
        string AdminGroups { get; }
    }

    public interface ISettingsCollectionItem
    {
        string Name { get; }
    }
}
