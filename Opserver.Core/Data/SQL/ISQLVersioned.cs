using System;
using System.Runtime.Serialization;

namespace StackExchange.Opserver.Data.SQL
{
    public interface ISQLVersioned
    {
        [IgnoreDataMember]
        Version MinVersion { get; }
        string GetFetchSQL(Version v);
    }
}
