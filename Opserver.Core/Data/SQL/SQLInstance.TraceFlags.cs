using System;
using System.Collections.Generic;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<TraceFlagInfo>> _traceFlags;
        public Cache<List<TraceFlagInfo>> TraceFlags => _traceFlags ?? (_traceFlags = SqlCacheList<TraceFlagInfo>(5*60));

        public class TraceFlagInfo : ISQLVersionedObject
        {
            // This likely works fine on 6+, need to test
            public Version MinVersion => SQLServerVersions.SQL2000.RTM;

            public int TraceFlag { get; internal set; }
            public bool Enabled { get; internal set; }
            public bool Global { get; internal set; }
            public int Session { get; internal set; }

            internal const string FetchSQL = @"
Declare @Flags Table(TraceFlag INT, Enabled BIT, Global BIT, Session INT);
Insert Into @Flags Exec('DBCC TRACESTATUS (-1) WITH NO_INFOMSGS');
Select * From @flags;";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }
    }
}
