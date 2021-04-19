using System;
using System.Collections.Generic;

namespace Opserver.Data.SQL
{
    public interface ISQLVersioned : IMinVersioned
    {
        ISet<SQLServerEdition> SupportedEditions { get; }

        string GetFetchSQL(in SQLServerEngine e);
    }
}
