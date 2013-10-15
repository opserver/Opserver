using System;

namespace StackExchange.Opserver.Data.SQL
{
    public interface ISQLVersionedObject
    {
        Version MinVersion { get; }
        string GetFetchSQL(Version v);
    }
}
