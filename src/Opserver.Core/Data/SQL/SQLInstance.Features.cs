using System;
using System.Collections.Generic;

namespace Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<SQLServerFeatures> _serverFeatures;
        public Cache<SQLServerFeatures> ServerFeatures => _serverFeatures ??= SqlCacheSingle<SQLServerFeatures>(60.Minutes());

        public class SQLServerFeatures : ISQLVersioned
        {
            Version IMinVersioned.MinVersion => SQLServerVersions.SQL2000.RTM;
            SQLServerEditions ISQLVersioned.SupportedEditions => SQLServerEditions.All;

            public bool HasSPWhoIsActive { get; internal set; }
            public bool HasSPBlitz { get; internal set; }
            public bool HasSPBlitzIndex { get; internal set; }
            public bool HasSPAskBrent { get; internal set; }

            public string GetFetchSQL(in SQLServerEngine e)
            {
                const string sql = @"
With Procs (name) as (
Select name COLLATE Latin1_General_CI_AS From sys.objects Where type = 'P'
--Union 
--Select name COLLATE Latin1_General_CI_AS From master.sys.objects Where type = 'P'
)

Select (Select Cast(Count(*) As BIT) From Procs Where name = 'sp_whoIsActive') as HasSPWhoIsActive,
       (Select Cast(Count(*) As BIT) From Procs Where name = 'sp_Blitz') as HasSPBlitz,
       (Select Cast(Count(*) As BIT) From Procs Where name = 'sp_BlitzIndex') as HasSPBlitzIndex,
       (Select Cast(Count(*) As BIT) From Procs Where name = 'sp_AskBrent') as HasSPAskBrent";

                if (e.Edition != SQLServerEditions.Azure)
                {
                    return sql.Replace("--", "");
                }

                return sql;
            }
        }
    }
}
