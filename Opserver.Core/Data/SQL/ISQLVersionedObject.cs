using System;
using Jil;

namespace StackExchange.Opserver.Data.SQL
{
    public interface ISQLVersionedObject
    {
        [JilDirective(Ignore = true)]
        Version MinVersion { get; }
        string GetFetchSQL(Version v);
    }
}
