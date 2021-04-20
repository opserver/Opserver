using System;
using System.Collections.Generic;

namespace Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<TraceFlagInfo>> _traceFlags;
        public Cache<List<TraceFlagInfo>> TraceFlags => _traceFlags ??= SqlCacheList<TraceFlagInfo>(5.Minutes());

        public class TraceFlagInfo : ISQLVersioned
        {
            // This likely works fine on 6+, need to test
            Version IMinVersioned.MinVersion => SQLServerVersions.SQL2000.RTM;
            ISet<SQLServerEdition> ISQLVersioned.SupportedEditions => SQLServerEditions.All;

            public int TraceFlag { get; internal set; }
            public bool Enabled { get; internal set; }
            public bool Global { get; internal set; }
            public int Session { get; internal set; }

            public string GetFetchSQL(in SQLServerEngine e) => @"
Declare @Flags Table(TraceFlag INT, Enabled BIT, Global BIT, Session INT);
Insert Into @Flags Exec('DBCC TRACESTATUS (-1) WITH NO_INFOMSGS');
Select * From @Flags;";
        }
    }
}
