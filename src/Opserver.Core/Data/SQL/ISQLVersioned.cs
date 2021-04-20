using System;
using System.Collections.Generic;

namespace Opserver.Data.SQL
{
    public interface ISQLVersioned : IMinVersioned
    {
        SQLServerEditions SupportedEditions { get; }

        string GetFetchSQL(in SQLServerEngine e);
    }
}
