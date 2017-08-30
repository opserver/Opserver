using System;

namespace StackExchange.Opserver.Data.SQL
{
    public interface ISQLVersioned : IMinVersioned
    {
        string GetFetchSQL(Version v);
    }
}
