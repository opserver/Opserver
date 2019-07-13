using Dapper;
using System;
using System.Collections.Generic;

namespace Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        public LightweightCache<List<SQLErrorLogInfo>> GetErrorLog(int minutesAgo)
        {
            return TimedCache("ErrorInfo-" + minutesAgo.ToString(),
                conn =>
                {
                    var sql = GetFetchSQL<SQLErrorLogInfo>();
                    return conn.Query<SQLErrorLogInfo>(sql, new { minutesAgo }).AsList();
                }, RefreshInterval, 5.Minutes());
        }

        public class SQLErrorLogInfo : ISQLVersioned
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            public DateTime LogDate { get; internal set; }
            public string ProcessInfo { get; internal set; }
            public string Text { get; internal set; }

            public string GetFetchSQL(Version v) => @"
Declare @Time_Start varchar(30);
Set @Time_Start = DATEADD(mi, -@minutesAgo, GETDATE());
Declare @ErrorLog Table (LogDate datetime, ProcessInfo varchar(255), Text varchar(max));
Insert Into @ErrorLog Exec master.dbo.xp_readerrorlog 0, 1, NULL, NULL, @Time_Start, NULL;
Select * From @ErrorLog Order By LogDate Desc;";
        }
    }
}
