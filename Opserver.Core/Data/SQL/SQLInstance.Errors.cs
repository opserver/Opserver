using Dapper;
using System;
using System.Collections.Generic;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        public LightweightCache<List<SQLErrorLogInfo>> GetErrorLog(int minutesAgo)
        {
            return TimedCache("ErrorInfo-" + minutesAgo.ToString(),
                conn =>
                {
                    var sql = GetFetchSQL<SQLErrorLogInfo>();
                    var results = conn.Query<SQLErrorLogInfo>(sql, new { minutesAgo }).AsList();

                    var timezone = ServerProperties.Data.TimeZoneInfo;

                    foreach (var result in results)
                    {
                        result.LogDate = result.LogDate.ToUniversalTime(timezone);
                    }

                    return results;
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
