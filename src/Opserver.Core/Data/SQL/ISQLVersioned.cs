using System;

namespace Opserver.Data.SQL
{
    public interface ISQLVersioned : IMinVersioned
    {
        string GetFetchSQL(Version v);
    }
}
