using System;

namespace StackExchange.Opserver
{
    public interface ISecurableSection
    {
        string ViewGroups { get; }
        string AdminGroups { get; }
    }

    public interface ISettingsCollectionItem<T> : IEquatable<T>, ISettingsCollectionItem
    {
    }

    public interface ISettingsCollectionItem
    {
        string Name { get; }
    }

    public interface IAfterLoadActions
    {
        void AfterLoad();
    }
}
