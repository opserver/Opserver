﻿namespace StackExchange.Opserver
{
    public interface ISecurableModule
    {
        bool Enabled { get; }
        string ViewGroups { get; }
        string AdminGroups { get; }
    }

    public interface ISettingsCollectionItem
    {
        string Name { get; }
    }
}
