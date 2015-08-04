using System;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<SQLServerFeatures> _serverFeatures;
        public Cache<SQLServerFeatures> ServerFeatures => _serverFeatures ?? (_serverFeatures = SqlCacheSingle<SQLServerFeatures>(60 * 60));

        public class SQLServerFeatures : ISQLVersionedObject
        {
            public Version MinVersion => SQLServerVersions.SQL2000.RTM;
            
            public bool HasSPWhoIsActive { get; internal set; }
            public bool HasSPBlitz { get; internal set; }
            public bool HasSPBlitzIndex { get; internal set; }
            public bool HasSPAskBrent { get; internal set; }

            internal const string FetchSQL = @"
With Procs (name) as (
Select name COLLATE Latin1_General_CI_AS From sys.objects Where type = 'P'
Union 
Select name COLLATE Latin1_General_CI_AS From master.sys.objects Where type = 'P')

Select (Select Cast(Count(*) As BIT) From Procs Where name = 'sp_whoIsActive') as HasSPWhoIsActive,
       (Select Cast(Count(*) As BIT) From Procs Where name = 'sp_Blitz') as HasSPBlitz,
       (Select Cast(Count(*) As BIT) From Procs Where name = 'sp_BlitzIndex') as HasSPBlitzIndex,
       (Select Cast(Count(*) As BIT) From Procs Where name = 'sp_AskBrent') as HasSPAskBrent";

            internal const string BatchFetchSQL = @"
Declare @Supported Table (name sysname, header varchar(200));
Insert Into @Supported
Select 'sp_whoIsActive', 'Who is Active?' 
Union
Select 'sp_Blitz', 'sp_Blitz (TM)' 
Union
Select 'sp_BlitzIndex', 'sp_BlitzIndex (TM)';

Select  o.name Collate Latin1_General_CI_AS as Name, 
       SubString(sm.definition, 
                 CharIndex(s.header, sm.definition), 
                 CharIndex(char(13), sm.definition, CharIndex(s.header, sm.definition)) - CharIndex(s.header, sm.definition)) Header
  From sys.objects o 
       Join sys.sql_modules sm On o.object_id = sm.object_id 
       Join @Supported s On o.name = s.name
 Where type = 'P'
Union 
Select o.name Collate Latin1_General_CI_AS, 
       SubString(sm.definition, 
                 CharIndex(s.header, sm.definition), 
                 CharIndex(char(13), sm.definition, CharIndex(s.header, sm.definition)) - CharIndex(s.header, sm.definition))
  From master.sys.objects o 
       Join master.sys.sql_modules sm On o.object_id = sm.object_id 
       Join @Supported s On o.name = s.name
 Where type = 'P'";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }
        
    }
}
