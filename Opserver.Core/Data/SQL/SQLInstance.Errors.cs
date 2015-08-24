using System;
using System.Collections.Generic;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        public Cache<List<SQLErrorLogInfo>> GetErrorLog(int minutesAgo)
        {
            return new Cache<List<SQLErrorLogInfo>>
                {
                    CacheKey = GetCacheKey("ErrorInfo-" + minutesAgo),
                    CacheForSeconds = 60,
                    CacheStaleForSeconds = 5*60,
                    UpdateCache = UpdateFromSql("Error Log last " + minutesAgo + " minutes", conn =>
                        {
                            var sql = GetFetchSQL<SQLErrorLogInfo>();
                            return conn.QueryAsync<SQLErrorLogInfo>(sql, new {minutesAgo});
                        })
                };
        }

        public class SQLErrorLogInfo : ISQLVersionedObject
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            public DateTime LogDate { get; internal set; }
            public string ProcessInfo { get; internal set; }
            public string Text { get; internal set; }

            internal const string FetchSQL = @"
Declare @Time_Start varchar(30);
Set @Time_Start = DATEADD(mi, -@minutesAgo, GETDATE());
Declare @ErrorLog Table (LogDate datetime, ProcessInfo varchar(255), Text varchar(max));
Insert Into @ErrorLog Exec master.dbo.xp_readerrorlog 0, 1, NULL, NULL, @Time_Start, NULL;
Select * From @ErrorLog Order By LogDate Desc;";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }
    }
}
