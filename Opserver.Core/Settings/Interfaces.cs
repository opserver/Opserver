using System;

namespace StackExchange.Opserver
{
    public interface ISecurableSection
    {
        bool Enabled { get; }
        string ViewGroups { get; }
        string AdminGroups { get; }
    }

    public interface ISettingsCollectionItem<T> : ISettingsCollectionItem
    {
    }

    public interface ISettingsCollectionItem
    {
        string Name { get; }
    }
}
