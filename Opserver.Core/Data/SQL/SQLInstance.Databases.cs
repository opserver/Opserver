using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<Database>> _databases;
        public Cache<List<Database>> Databases
        {
            get
            {
                return _databases ?? (_databases = new Cache<List<Database>>
                {
                    CacheForSeconds = 5 * 60, // TODO: Revisit
                    UpdateCache = UpdateFromSql(nameof(Databases), async conn =>
                    {
                        var sql = QueryLookup.GetOrAdd(Tuple.Create(nameof(Databases), Version), k =>
                            GetFetchSQL<Database>(k.Item2) + "\n" +
                            GetFetchSQL<DatabaseLastBackup>(k.Item2) + "\n" +
                            GetFetchSQL<DatabaseFile>(k.Item2) + "\n" +
                            GetFetchSQL<DatabaseVLF>(k.Item2)
                            );

                        List<Database> dbs;
                        using (var multi = await conn.QueryMultipleAsync(sql).ConfigureAwait(false))
                        {
                            dbs = await multi.ReadAsync<Database>().ConfigureAwait(false).AsList().ConfigureAwait(false);
                            var backups = await multi.ReadAsync<DatabaseLastBackup>().ConfigureAwait(false).AsList().ConfigureAwait(false);
                            var files = await multi.ReadAsync<DatabaseFile>().ConfigureAwait(false).AsList().ConfigureAwait(false);
                            var vlfs = await multi.ReadAsync<DatabaseVLF>().ConfigureAwait(false).AsList().ConfigureAwait(false);

                            // Safe groups
                            var backupLookup = backups.GroupBy(b => b.DatabaseId).ToDictionary(g => g.Key, g => g.ToList());
                            var fileLookup = files.GroupBy(f => f.DatabaseId).ToDictionary(g => g.Key, g => g.ToList());
                            var vlfsLookup = vlfs.GroupBy(f => f.DatabaseId).ToDictionary(g => g.Key, g => g.FirstOrDefault());

                            foreach (var db in dbs)
                            {
                                List<DatabaseLastBackup> b;
                                db.Backups = backupLookup.TryGetValue(db.Id, out b) ? b : new List<DatabaseLastBackup>();

                                List<DatabaseFile> f;
                                db.Files = fileLookup.TryGetValue(db.Id, out f) ? f : new List<DatabaseFile>();

                                DatabaseVLF v;
                                db.VLFCount = vlfsLookup.TryGetValue(db.Id, out v) ? v?.VLFCount : null;
                            }
                        }
                        return dbs;
                    })
                });
            }
        }
        
        public Cache<List<DatabaseFile>> GetFileInfo(string databaseName) =>
            DatabaseFetch<DatabaseFile>(databaseName);

        public Cache<List<DatabaseTable>> GetTableInfo(string databaseName) =>
            DatabaseFetch<DatabaseTable>(databaseName, 60, 5*60);

        public Cache<List<DatabaseView>> GetViewInfo(string databaseName) =>
            DatabaseFetch<DatabaseView>(databaseName, 60, 5*60);

        public Cache<List<StoredProcedure>> GetStoredProcedureInfo(string databaseName) =>
    DatabaseFetch<StoredProcedure>(databaseName, 60, 5 * 60);


        public Cache<List<DatabaseBackup>> GetBackupInfo(string databaseName) =>
            DatabaseFetch<DatabaseBackup>(databaseName, RefreshInterval, 60);

        public Cache<List<MissingIndex>> GetMissingIndexes(string databaseName) =>
            DatabaseFetch<MissingIndex>(databaseName, RefreshInterval, 60);

        public Cache<List<RestoreHistory>> GetRestoreInfo(string databaseName) => 
            DatabaseFetch<RestoreHistory>(databaseName, RefreshInterval, 60);

        public Cache<List<DatabaseColumn>> GetColumnInfo(string databaseName) =>
            DatabaseFetch<DatabaseColumn>(databaseName);

        public Cache<List<DatabaseDataSpace>> GetDataSpaceInfo(string databaseName) =>
            DatabaseFetch<DatabaseDataSpace>(databaseName);

        public Database GetDatabase(string databaseName) => Databases.Data?.FirstOrDefault(db => db.Name == databaseName);

        private Cache<List<T>> DatabaseFetch<T>(string databaseName, int duration = 5*60, int staleDuration = 30*60)
            where T : ISQLVersioned, new() =>
                Cache<List<T>>.WithKey(
                    GetCacheKey(typeof (T).Name + "Info-" + databaseName),
                    UpdateFromSql(typeof (T).Name + " Info for " + databaseName,
                        conn =>
                        {
                            conn.ChangeDatabase(databaseName);
                            return conn.QueryAsync<T>(GetFetchSQL<T>(), new { databaseName });
                        },
                        logExceptions: true),
                    duration, staleDuration);

        public static readonly HashSet<string> SystemDatabaseNames = new HashSet<string>
            {
                "master",
                "model",
                "msdb",
                "tempdb"
            };

        public class Database : ISQLVersioned, IMonitorStatus
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            public string OverallStateDescription
            {
                get
                {
                    if (IsReadOnly) return "Read-only";
                    // TODO: Other statuses, e.g. Not Synchronizing
                    return State.GetDescription();
                }
            }

            public MonitorStatus MonitorStatus
            {
                get
                {
                    if (IsReadOnly)
                        return MonitorStatus.Warning;

                    switch (State)
                    {
                        case DatabaseStates.Restoring:
                        case DatabaseStates.Recovering:
                        case DatabaseStates.RecoveryPending:
                            return MonitorStatus.Unknown;
                        case DatabaseStates.Copying:
                            return MonitorStatus.Warning;
                        case DatabaseStates.Suspect:
                        case DatabaseStates.Emergency:
                        case DatabaseStates.Offline:
                            return MonitorStatus.Critical;
                        //case DatabaseStates.Online:
                        default:
                            return MonitorStatus.Good;
                    }
                }
            }
            public string MonitorStatusReason
            {
                get
                {
                    if (IsReadOnly)
                        return Name + " is read-only";

                    switch (State)
                    {
                        case DatabaseStates.Online:
                            return null;
                        default:
                            return "Database State: " + State.GetDescription();
                    }
                }
            }

            private bool? _isSystemDatabase;
            public int Id { get; internal set; }
            public bool IsSystemDatabase => (_isSystemDatabase ?? (_isSystemDatabase = SystemDatabaseNames.Contains(Name))).Value;
            public string Name { get; internal set; }
            public DatabaseStates State { get; internal set; }
            public CompatabilityLevels CompatibilityLevel { get; internal set; }
            public RecoveryModels RecoveryModel { get; internal set; }
            public PageVerifyOptions PageVerifyOption { get; internal set; }
            public LogReuseWaits LogReuseWait { get; internal set; }
            public Guid? ReplicaId { get; internal set; }
            public Guid? GroupDatabaseId { get; internal set; }
            public UserAccesses UserAccess { get; internal set; }
            public bool IsFullTextEnabled { get; internal set; }
            public bool IsReadOnly { get; internal set; }
            public bool IsReadCommittedSnapshotOn { get; internal set; }
            public SnapshotIsolationStates SnapshotIsolationState { get; internal set; }
            public Containments? Containment { get; internal set; }
            public string LogVolumeId { get; internal set; }
            public double TotalSizeMB { get; internal set; }
            public double RowSizeMB { get; internal set; }
            public double StreamSizeMB { get; internal set; }
            public double TextIndexSizeMB { get; internal set; }
            public double? LogSizeMB { get; internal set; }
            public double? LogSizeUsedMB { get; internal set; }

            public List<DatabaseLastBackup> Backups { get; internal set; }
            public List<DatabaseFile> Files { get; internal set; }
            public int? VLFCount { get; internal set; }

            public double? LogPercentUsed => LogSizeMB > 0 ? 100 * LogSizeUsedMB / LogSizeMB : null;

            internal const string FetchSQL2012Columns = @"
       db.replica_id ReplicaId,
       db.group_database_id GroupDatabaseId,
       db.containment Containment, 
       v.LogVolumeId,
";
            internal const string FetchSQL2012Joins = @"
     Left Join (Select mf.database_id, vs.volume_id LogVolumeId
                  From sys.master_files mf
                       Cross Apply sys.dm_os_volume_stats(mf.database_id, mf.file_id) vs
                 Where type = 1) v On db.database_id = v.database_id
";

            internal const string FetchSQL = @"
Select db.database_id Id,
       db.name Name,
       db.state State,
       db.compatibility_level CompatibilityLevel,
       db.recovery_model RecoveryModel,
       db.page_verify_option PageVerifyOption,
       db.log_reuse_wait LogReuseWait,
       db.user_access UserAccess,
       db.is_fulltext_enabled IsFullTextEnabled,
       db.is_read_only IsReadOnly,
       db.is_read_committed_snapshot_on IsReadCommittedSnapshotOn,
       db.snapshot_isolation_state SnapshotIsolationState, {0}
       (Cast(st.TotalSize As Float)*8)/1024 TotalSizeMB,
       (Cast(sr.RowSize As Float)*8)/1024 RowSizeMB,
       (Cast(ss.StreamSize As Float)*8)/1024 StreamSizeMB,
       (Cast(sti.TextIndexSize As Float)*8)/1024 TextIndexSizeMB,
       Cast(logs.cntr_value as Float)/1024 LogSizeMB,
       Cast(logu.cntr_value as Float)/1024 LogSizeUsedMB
From sys.databases db
     Left Join sys.dm_os_performance_counters logu On db.name = logu.instance_name And logu.counter_name LIKE N'Log File(s) Used Size (KB)%' 
     Left Join sys.dm_os_performance_counters logs On db.name = logs.instance_name And logs.counter_name LIKE N'Log File(s) Size (KB)%' 
     Left Join (Select database_id, Sum(Cast(size As Bigint)) TotalSize 
                  From sys.master_files 
              Group By database_id) st On db.database_id = st.database_id
     Left Join (Select database_id, Sum(Cast(size As Bigint)) RowSize 
                  From sys.master_files 
                 Where type = 0 
              Group By database_id) sr On db.database_id = sr.database_id
     Left Join (Select database_id, Sum(Cast(size As Bigint)) StreamSize 
                  From sys.master_files 
                 Where type = 2
              Group By database_id) ss On db.database_id = ss.database_id
     Left Join (Select database_id, Sum(Cast(size As Bigint)) TextIndexSize 
                  From sys.master_files 
                 Where type = 4
              Group By database_id) sti On db.database_id = sti.database_id {1};";

            public string GetFetchSQL(Version v)
            {
                if (v >= SQLServerVersions.SQL2012.RTM)
                    return string.Format(FetchSQL, FetchSQL2012Columns, FetchSQL2012Joins);

                return string.Format(FetchSQL, "", "");
            }
        }

        public class DatabaseLastBackup : ISQLVersioned
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            public int DatabaseId { get; internal set; }
            public string Name { get; internal set; }
            
            public char? LastBackupType { get; internal set; }
            public string LastBackupTypeDescription => DatabaseBackup.GetTypeDescription(LastBackupType);
            public DateTime? LastBackupStartDate { get; internal set; }
            public DateTime? LastBackupFinishDate { get; internal set; }
            public long? LastBackupSizeBytes { get; internal set; }
            public long? LastBackupCompressedSizeBytes { get; internal set; }
            public MediaDeviceTypes? LastBackupMediaDeviceType { get; internal set; }
            public string LastBackupLogicalDeviceName { get; internal set; }
            public string LastBackupPhysicalDeviceName { get; internal set; }

            public DateTime? LastFullBackupStartDate { get; internal set; }
            public DateTime? LastFullBackupFinishDate { get; internal set; }
            public long? LastFullBackupSizeBytes { get; internal set; }
            public long? LastFullBackupCompressedSizeBytes { get; internal set; }
            public MediaDeviceTypes? LastFullBackupMediaDeviceType { get; internal set; }
            public string LastFullBackupLogicalDeviceName { get; internal set; }
            public string LastFullBackupPhysicalDeviceName { get; internal set; }

            internal const string FetchSQL = @"
Select db.database_id DatabaseId,
       db.name Name, 
       lb.type LastBackupType,
       lb.backup_start_date LastBackupStartDate,
       lb.backup_finish_date LastBackupFinishDate,
       lb.backup_size LastBackupSizeBytes,
       lb.compressed_backup_size LastBackupCompressedSizeBytes,
       lbmf.device_type LastBackupMediaDeviceType,
       lbmf.logical_device_name LastBackupLogicalDeviceName,
       lbmf.physical_device_name LastBackupPhysicalDeviceName,
       fb.backup_start_date LastFullBackupStartDate,
       fb.backup_finish_date LastFullBackupFinishDate,
       fb.backup_size LastFullBackupSizeBytes,
       fb.compressed_backup_size LastFullBackupCompressedSizeBytes,
       fbmf.device_type LastFullBackupMediaDeviceType,
       fbmf.logical_device_name LastFullBackupLogicalDeviceName,
       fbmf.physical_device_name LastFullBackupPhysicalDeviceName
  From sys.databases db
       Left Outer Join (Select *
                          From (Select backup_set_id, 
                                       database_name,
                                       backup_start_date,
                                       backup_finish_date,
                                       backup_size,
                                       compressed_backup_size,
                                       media_set_id,
                                       type,
                                       Row_Number() Over(Partition By database_name Order By backup_start_date Desc) as rownum
                                  From msdb.dbo.backupset) b                                  
                           Where rownum = 1) lb 
         On lb.database_name = db.name
       Left Outer Join msdb.dbo.backupmediafamily lbmf
         On lb.media_set_id = lbmf.media_set_id And lbmf.media_count = 1
       Left Outer Join (Select *
                          From (Select backup_set_id, 
                                       database_name,
                                       backup_start_date,
                                       backup_finish_date,
                                       backup_size,
                                       compressed_backup_size,
                                       media_set_id,
                                       type,
                                       Row_Number() Over(Partition By database_name Order By backup_start_date Desc) as rownum
                                  From msdb.dbo.backupset
                                 Where type = 'D') b                                  
                           Where rownum = 1) fb 
         On fb.database_name = db.name
       Left Outer Join msdb.dbo.backupmediafamily fbmf
         On fb.media_set_id = fbmf.media_set_id And fbmf.media_count = 1";

            public string GetFetchSQL(Version v)
            {
                // Compressed backup info added in 2008
                if (v < SQLServerVersions.SQL2008.RTM)
                {
                    return FetchSQL.Replace("compressed_backup_size,", "null compressed_backup_size,");
                }
                return FetchSQL;
            }
        }
        public class MissingIndex : ISQLVersioned
        {
            public string SchemaName { get; internal set; }
            public string TableName { get; internal set; }
            public decimal AvgTotalUserCost { get; internal set; }
            public decimal AvgUserImpact { get; internal set; }
            public int UserSeeks { get; internal set; }
            public int UserScans { get; internal set; }
            public int UniqueCompiles { get; internal set; }
            public string EqualityColumns { get; internal set; }
            public string InEqualityColumns { get; internal set; }
            public string IncludedColumns { get; internal set; }
            public decimal EstimatedImprovement { get; internal set; }
            public Version MinVersion => SQLServerVersions.SQL2008.SP1;
            
            public string GetFetchSQL(Version v)
            {
                return @"
 Select s.name SchemaName,
        o.name TableName,
        avg_total_user_cost AvgTotalUserCost,
        avg_user_impact AvgUserImpact,
        user_seeks UserSeeks,
        user_scans UserScans,
		unique_compiles UniqueCompiles,
		equality_columns EqualityColumns,
		inequality_columns InEqualityColumns,
		included_columns IncludedColumns,
        avg_total_user_cost* avg_user_impact *(user_seeks + user_scans) EstimatedImprovement
   From sys.dm_db_missing_index_details mid
        Join sys.dm_db_missing_index_groups mig On mig.index_handle = mid.index_handle
        Join sys.dm_db_missing_index_group_stats migs On migs.group_handle = mig.index_group_handle
        Join sys.databases d On d.database_id = mid.database_id
        Join sys.objects o On mid.object_id = o.object_id
        Join sys.schemas s On o.schema_id = s.schema_id
  Where d.name = @databaseName
    And avg_total_user_cost * avg_user_impact * (user_seeks + user_scans) > 0
  Order By EstimatedImprovement Desc";
            }
        }


        public class StoredProcedure : ISQLVersioned
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;
            public string SchemaName { get; internal set; }
            public string ProcedureName { get; internal set; }
            public DateTime CreationDate { get; internal set; }
            public DateTime LastModifiedDate { get; internal set; }
            public DateTime? LastExecuted { get; internal set; }
            public int? ExecutionCount { get; internal set; }
            public int? LastElapsedTime { get; internal set; }
            public int? MaxElapsedTime { get; internal set; }
            public int? MinElapsedTime { get; internal set; }
            public string Definition { get; internal set; }
            public string GetFetchSQL(Version v) => @"
Select p.object_id,
       s.name as SchemaName,
       p.name ProcedureName,
       p.create_date CreationDate,
       p.modify_date LastModifiedDate,
       Max(ps.last_execution_time) as LastExecuted,
       Max(ps.execution_count) as ExecutionCount,
       Max(ps.last_elapsed_time/1000) as LastElapsedTime,
       Max(ps.max_elapsed_time/1000) as MaxElapsedTime,
       Max(ps.min_elapsed_time/1000) as MinElapsedTime,
       sm.definition [Definition]
  From sys.procedures p
       Join sys.schemas s
         On p.schema_id = s.schema_id
       Join sys.sql_modules sm 
         On p.object_id  = sm.object_id 
       Left Join sys.dm_exec_procedure_stats ps 
         On p.object_id	= ps.object_id
 Where p.is_ms_shipped = 0
 Group By p.object_id, s.name, p.name, p.create_date, p.modify_date, sm.definition";
        }
        public class RestoreHistory : ISQLVersioned
        {
            public DateTime RestoreFinishDate { get; internal set; }
            public string  UserName { get; internal set; }
            public string BackupMedia { get; internal set; }
            public DateTime BackupStartDate { get; internal set; }
            public DateTime BackupFinishDate { get; internal set; }
            public char? RestoreType { get; internal set; }
            public string RestoreTypeDescription => GetTypeDescription(RestoreType);

            public static string GetTypeDescription(char? type)
            {
                switch (type)
                {
                    case 'D':
                        return "Database";
                    case 'F':
                        return "File";
                    case 'G':
                        return "Filegroup";
                    case 'I':
                        return "Differential";
                    case 'L':
                        return "Log";
                    case 'V':
                        return "Verify Only";
                    case null:
                        return "";
                    default:
                        return "Unknown";
                }
            }

            public Version MinVersion =>  SQLServerVersions.SQL2008.SP1;
            public string GetFetchSQL(Version v)
            {
                return @"
Select r.restore_date RestoreFinishDate, 
       r.user_name UserName, 
       bmf.physical_device_name BackupMedia,
	   bs.backup_start_date BackupStartDate,
	   bs.backup_finish_date BackupFinishDate,
	   r.restore_type RestoreType
  From msdb.dbo.[restorehistory] r 
       Join [msdb].[dbo].[backupset] bs 
         On r.backup_set_id = bs.backup_set_id
       Join [msdb].[dbo].[backupmediafamily] bmf 
         On bs.media_set_id = bmf.media_set_id
 Where r.destination_database_name = @databaseName"; 
            }
        }
        public class DatabaseBackup : ISQLVersioned
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;
            
            public char? Type { get; internal set; }
            public string TypeDescription => GetTypeDescription(Type);
            public DateTime? StartDate { get; internal set; }
            public DateTime? FinishDate { get; internal set; }
            public long? SizeBytes { get; internal set; }
            public long? CompressedSizeBytes { get; internal set; }
            public MediaDeviceTypes? MediaDeviceType { get; internal set; }
            public string LogicalDeviceName { get; internal set; }
            public string PhysicalDeviceName { get; internal set; }

            public static string GetTypeDescription(char? type)
            {
                switch (type)
                {
                    case 'D':
                        return "Full";
                    case 'I':
                        return "Differential (DB)";
                    case 'L':
                        return "Log";
                    case 'F':
                        return "File/Filegroup";
                    case 'G':
                        return "Differential (File)";
                    case 'P':
                        return "Partial";
                    case 'Q':
                        return "Differential (Partial)";
                    case null:
                        return "";
                    default:
                        return "Unknown";
                }
            }

            internal const string FetchSQL = @"
Select Top 100
       b.type Type,
       b.backup_start_date StartDate,
       b.backup_finish_date FinishDate,
       b.backup_size SizeBytes,
       b.compressed_backup_size CompressedSizeBytes,
       bmf.device_type MediaDeviceType,
       bmf.logical_device_name LogicalDeviceName,
       bmf.physical_device_name PhysicalDeviceName
  From msdb.dbo.backupset b
       Left Join msdb.dbo.backupmediafamily bmf
         On b.media_set_id = bmf.media_set_id And bmf.media_count = 1
  Where b.database_name = @databaseName
  Order By FinishDate Desc";

            public string GetFetchSQL(Version v)
            {
                // Compressed backup info added in 2008
                if (v < SQLServerVersions.SQL2008.RTM)
                {
                    return FetchSQL.Replace("b.compressed_backup_size", "null");
                }
                return FetchSQL;
            }
        }

        public class DatabaseFile : ISQLVersioned
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            private string _volumeMountPoint;
            public string VolumeMountPoint => _volumeMountPoint ?? (_volumeMountPoint = PhysicalName?.Split(StringSplits.Colon).First());
            public int DatabaseId { get; internal set; }
            public string DatabaseName { get; internal set; }
            public int FileId { get; internal set; }
            public string FileName { get; internal set; }
            public string PhysicalName { get; internal set; }
            public int DataSpaceId { get; internal set; }
            public DatabaseFileTypes FileType { get; internal set; }
            public DatabaseFileStates FileState { get; internal set; }
            public long FileSizePages { get; internal set; }
            public long FileMaxSizePages { get; internal set; }
            public long FileGrowthRaw { get; internal set; }
            public bool FileIsPercentGrowth { get; internal set; }
            public bool FileIsReadOnly { get; internal set; }
            public long StallReadMs { get; internal set; }
            public long NumReads { get; internal set; }
            public long StallWriteMs { get; internal set; }
            public long NumWrites { get; internal set; }
            public long UsedSizeBytes { get; internal set; }
            public long TotalSizeBytes { get; internal set; }

            public double AvgReadStallMs => NumReads == 0 ? 0 : StallReadMs / (double)NumReads;
            public double AvgWriteStallMs => NumWrites == 0 ? 0 : StallWriteMs / (double)NumWrites;

            private static readonly Regex _ShortPathRegex = new Regex(@"C:\\Program Files\\Microsoft SQL Server\\MSSQL\d+.MSSQLSERVER\\MSSQL\\DATA", RegexOptions.Compiled);
            private string _shortPhysicalName;
            public string ShortPhysicalName =>
                    _shortPhysicalName ?? (_shortPhysicalName = _ShortPathRegex.Replace(PhysicalName ?? "", @"C:\Program...MSSQLSERVER\MSSQL\DATA"));
            
            public string GrowthDescription
            {
                get
                {
                    if (FileGrowthRaw == 0) return "None";

                    if (FileIsPercentGrowth) return FileGrowthRaw.ToString() + "%";

                    // Growth that's not percent-based is 8KB pages rounded to the nearest 64KB
                    return (FileGrowthRaw*8*1024).ToHumanReadableSize();
                }
            }

            public string MaxSizeDescription
            {
                get
                {
                    switch (FileMaxSizePages)
                    {
                        case 0:
                            return "At Max - No Growth";
                        case -1:
                            return "No Limit - Disk Capacity";
                        case 268435456:
                            return "2 TB";
                        default:
                            return (FileMaxSizePages*8*1024).ToHumanReadableSize();
                    }
                }
            }

            public string GetFetchSQL(Version v) => @"
Select mf.database_id DatabaseId,
       DB_Name(mf.database_id) DatabaseName,
       mf.file_id FileId,
       mf.name FileName,
       mf.physical_name PhysicalName,
       mf.data_space_id DataSpaceId,
       mf.type FileType,
       mf.state FileState,
       mf.size FileSizePages,
       mf.max_size FileMaxSizePages,
       mf.growth FileGrowthRaw,
       mf.is_percent_growth FileIsPercentGrowth,
       mf.is_read_only FileIsReadOnly,
       fs.io_stall_read_ms StallReadMs,
       fs.num_of_reads NumReads,
       fs.io_stall_write_ms StallWriteMs,
       fs.num_of_writes NumWrites,
       Cast(FileProperty(mf.name, 'SpaceUsed') As BigInt)*8*1024 UsedSizeBytes,
       Cast(mf.size as BigInt)*8*1024 TotalSizeBytes
  From sys.dm_io_virtual_file_stats(null, null) fs
       Join sys.master_files mf 
         On fs.database_id = mf.database_id
         And fs.file_id = mf.file_id
 Where (FileProperty(mf.name, 'SpaceUsed') Is Not Null Or DB_Name() = 'master')";
        }

        public class DatabaseDataSpace : ISQLVersioned
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            public int Id { get; internal set; }
            public string Name { get; internal set; }
            public string Type { get; internal set; }
            public string TypeDescription => GetTypeDescription(Type);
            public bool IsDefault { get; internal set; }
            public bool IsSystem { get; internal set; }

            public static string GetTypeDescription(string type)
            {
                switch (type)
                {
                    case "FG":
                        return "Filegroup";
                    case "PS":
                        return "Partition Scheme";
                    case "FD":
                        return "FILESTREAM";
                    case null:
                        return "";
                    default:
                        return "Unknown";
                }
            }

            public string GetFetchSQL(Version v) => @"
Select data_space_id Id,
       name Name,
       type Type,
       is_default IsDefault,
       is_system IsSystem
  From sys.data_spaces";
        }

        public class DatabaseVLF : ISQLVersioned
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            public int DatabaseId { get; internal set; }
            public string DatabaseName { get; internal set; }
            public int VLFCount { get; internal set; }

            internal const string FetchSQL = @"
Create Table #VLFCounts (DatabaseId int, DatabaseName sysname, VLFCount int);
Create Table #vlfTemp (
    RecoveryUnitId int,
    FileId int, 
    FileSize nvarchar(255), 
    StartOffset nvarchar(255), 
    FSeqNo nvarchar(255), 
    Status int, 
    Parity int, 
    CreateLSN nvarchar(255)
);

Declare @dbId int, @dbName sysname;
Declare dbs Cursor Local Fast_Forward For (
    Select db.database_id, db.name From sys.databases db
    Left Join sys.database_mirroring m ON db.database_id = m.database_id
    Where db.state <> 6
        and ( db.state <> 1 
            or ( m.mirroring_role = 2 and m.mirroring_state = 4 )
            )
    );
Open dbs;
Fetch Next From dbs Into @dbId, @dbName;
While @@FETCH_STATUS = 0
Begin
    Insert Into #vlfTemp
    Exec('DBCC LOGINFO(''' + @dbName + ''') WITH NO_INFOMSGS');
    Insert Into #VLFCounts (DatabaseId, DatabaseName, VLFCount)
    Values (@dbId, @dbName, @@ROWCOUNT);
    Truncate Table #vlfTemp;
    Fetch Next From dbs Into @dbId, @dbName;
End
Close dbs;
Deallocate dbs;

Select * From #VLFCounts;

Drop Table #VLFCounts;
Drop Table #vlfTemp;";

            public string GetFetchSQL(Version v)
            {
                if (v < SQLServerVersions.SQL2012.RTM)
                    return FetchSQL.Replace("RecoveryUnitId int,", "");
                return FetchSQL;
            }
        }

        public class DatabaseTable : ISQLVersioned
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            public int Id { get; internal set; }
            public string SchemaName { get; internal set; }
            public string TableName { get; internal set; }
            public DateTime CreationDate { get; internal set; }
            public int IndexCount { get; internal set; }
            public long RowCount { get; internal set; }
            public long DataTotalSpaceKB { get; internal set; }
            public long IndexTotalSpaceKB { get; internal set; }
            public long UsedSpaceKB { get; internal set; }
            public long TotalSpaceKB { get; internal set; }
            public long FreeSpaceKB => TotalSpaceKB - UsedSpaceKB;
            public TableTypes TableType { get; internal set; }

            public string GetFetchSQL(Version v) => @"
Select t.object_id Id,
       s.name SchemaName,
       t.name TableName,
       t.create_date CreationDate,
       Count(i.index_id) IndexCount,
       Max(ddps.row_count) [RowCount],
       Sum(Case When i.type <= 1 Then a.total_pages Else 0 End) * 8 DataTotalSpaceKB,
       Sum(Case When i.type > 1 Then a.total_pages Else 0 End) * 8 IndexTotalSpaceKB,
       Sum(a.used_pages) * 8 UsedSpaceKB,
       Sum(a.total_pages) * 8 TotalSpaceKB,
       (Case Max(i.type) When 0 Then 0 Else 1 End) as TableType
  From sys.tables t
       Join sys.schemas s
         On t.schema_id = s.schema_id
       Join sys.indexes i 
         On t.object_id = i.object_id
       Join sys.partitions p 
         On i.object_id = p.object_id 
         And i.index_id = p.index_id
       Join (Select container_id,
                    Sum(used_pages) used_pages,
                    Sum(total_pages) total_pages
               From sys.allocation_units
           Group By container_id) a
         On p.partition_id = a.container_id
       Left Join sys.dm_db_partition_stats ddps
         On i.object_id = ddps.object_id
         And i.index_id = ddps.index_id
         And i.type < 2         
 Where t.is_ms_shipped = 0
   And i.object_id > 255
Group By t.object_id, t.Name, t.create_date, s.name";
        }
        
        public class DatabaseView : ISQLVersioned
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            public int Id { get; internal set; }
            public string SchemaName { get; internal set; }
            public string ViewName { get; internal set; }
            public DateTime CreationDate { get; internal set; }
            public DateTime LastModifiedDate { get; internal set; }
            public bool IsReplicated { get; internal set; }
            public string Definition { get; internal set; }

            public string GetFetchSQL(Version v) => @"
Select v.object_id Id,
       s.name SchemaName,
       v.name ViewName,
       v.create_date CreationDate,
       v.modify_date LastModifiedDate,
       v.is_replicated IsReplicated,
       sm.definition Definition
  From sys.views v
       Join sys.schemas s
         On v.schema_id = s.schema_id
       Join sys.sql_modules sm 
         On sm.object_id = v.object_id  
 Where v.is_ms_shipped = 0";
        }

        public class DatabaseColumn : ISQLVersioned
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            public string Id => Schema + "." + TableName + "." + ColumnName;

            public string Schema { get; internal set; }
            public string TableName { get; internal set; }
            public string ViewName { get; internal set; }
            public int Position { get; internal set; }
            public int ObjectId { get; internal set; }
            public string ColumnName { get; internal set; }
            public string DataType { get; internal set; }
            public bool IsNullable { get; internal set; }
            public int MaxLength { get; internal set; }
            public byte Scale { get; internal set; }
            public byte Precision { get; internal set; }
            public string ColumnDefault { get; internal set; }
            public string CollationName { get; internal set; }
            public bool IsIdentity { get; internal set; }
            public bool IsComputed { get; internal set; }
            public bool IsFileStream { get; internal set; }
            public bool IsSparse { get; internal set; }
            public bool IsColumnSet { get; internal set; }
            public string PrimaryKeyConstraint { get; internal set; }
            public string ForeignKeyConstraint { get; internal set; }
            public string ForeignKeyTargetSchema { get; internal set; }
            public string ForeignKeyTargetTable { get; internal set; }
            public string ForeignKeyTargetColumn { get; internal set; }

            public string DataTypeDescription
            {
                get
                {
                    var props = new List<string>();
                    if (IsSparse) props.Add("sparse");
                    switch (DataType)
                    {
                        case "varchar":
                        case "nvarchar":
                        case "varbinary":
                            props.Add($"{DataType}({(MaxLength == -1 ? "max" : MaxLength.ToString())})");
                            break;
                        case "decimal":
                        case "numeric":
                            props.Add($"{DataType}({Scale.ToString()},{Precision.ToString()})");
                            break;
                        default:
                            props.Add(DataType);
                            break;
                    }
                    props.Add(IsNullable ? "null" : "not null");
                    return string.Join(", ", props);
                }
            }

// For non-SQL later
//            internal const string FetchSQL = @"
//Select c.TABLE_SCHEMA [Schema],
//       c.TABLE_NAME TableName,
//       c.ORDINAL_POSITION Position,
//       c.COLUMN_NAME ColumnName,
//       c.COLUMN_DEFAULT ColumnDefault,
//       Cast(Case c.IS_NULLABLE When 'YES' Then 1 Else 0 End as BIT) IsNullable,
//       c.DATA_TYPE DataType,
//       c.CHARACTER_MAXIMUM_LENGTH MaxLength,
//       c.NUMERIC_PRECISION Precision,
//       c.NUMERIC_PRECISION_RADIX NumericPrecisionRadix,
//       c.NUMERIC_SCALE NumericScale,
//       c.DATETIME_PRECISION DatetimePrecision,
//       c.COLLATION_NAME CollationName,
//       kcu.PrimaryKeyConstraint,
//       kcu.ForeignKeyConstraint,
//       kcu.ForeignKeyTargetSchema,
//       kcu.ForeignKeyTargetTable,
//       kcu.ForeignKeyTargetColumn
//  From INFORMATION_SCHEMA.COLUMNS c
//       Left Join (Select cu.TABLE_SCHEMA, 
//                         cu.TABLE_NAME, 
//                         cu.COLUMN_NAME,
//                         (Case When OBJECTPROPERTY(OBJECT_ID(cu.CONSTRAINT_NAME), 'IsPrimaryKey') = 1
//                               Then cu.CONSTRAINT_NAME
//                               Else Null
//                          End) as PrimaryKeyConstraint,
//                          rc.CONSTRAINT_NAME ForeignKeyConstraint,
//                          cut.TABLE_SCHEMA ForeignKeyTargetSchema,
//                          cut.TABLE_NAME ForeignKeyTargetTable,
//                          cut.COLUMN_NAME ForeignKeyTargetColumn
//                    From INFORMATION_SCHEMA.KEY_COLUMN_USAGE cu
//                         Left Join INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
//                           On cu.CONSTRAINT_CATALOG = rc.CONSTRAINT_CATALOG
//                          And cu.CONSTRAINT_SCHEMA = rc.CONSTRAINT_SCHEMA
//                          And cu.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
//                         Left Join INFORMATION_SCHEMA.KEY_COLUMN_USAGE cut
//                          On rc.UNIQUE_CONSTRAINT_CATALOG = cut.CONSTRAINT_CATALOG
//                          And rc.UNIQUE_CONSTRAINT_SCHEMA = cut.CONSTRAINT_SCHEMA
//                          And rc.UNIQUE_CONSTRAINT_NAME = cut.CONSTRAINT_NAME) kcu
//         On c.TABLE_SCHEMA = kcu.TABLE_SCHEMA
//         And c.TABLE_NAME = kcu.TABLE_NAME
//         And c.COLUMN_NAME = kcu.COLUMN_NAME
//Order By c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION";
            internal const string FetchSQL2008Columns = @"
       c.is_sparse IsSparse,
       c.is_column_set IsColumnSet,
";

            internal const string FetchSQL = @"
Select s.name [Schema],
       t.name TableName,
       v.name ViewName,
       c.column_id Position,
       c.object_id ObjectId,
       c.name ColumnName,
       ty.name DataType,
       c.is_nullable IsNullable,
       (Case When ty.name In ('nchar', 'ntext','nvarchar') And c.max_length <> -1 Then c.max_length / 2 Else c.max_length End) MaxLength,
       c.scale Scale,
       c.precision Precision,
       object_definition(c.default_object_id) ColumnDefault,
       c.collation_name CollationName,
       c.is_identity IsIdentity,
       c.is_computed IsComputed,
       c.is_filestream IsFileStream, {0}
       (Select Top 1 i.name
          From sys.indexes i 
               Join sys.index_columns ic On i.object_id = ic.object_id And i.index_id = ic.index_id
         Where i.object_id = t.object_id
           And ic.column_id = c.column_id
           And i.is_primary_key = 1) PrimaryKeyConstraint,
       object_name(fkc.constraint_object_id) ForeignKeyConstraint,
       fs.name ForeignKeyTargetSchema,
       ft.name ForeignKeyTargetTable,
       fc.name ForeignKeyTargetColumn
  From sys.columns c
       Left Join sys.tables t On c.object_id = t.object_id
       Left Join sys.views v On c.object_id = v.object_id
       Join sys.schemas s On (t.schema_id = s.schema_id Or v.schema_id = s.schema_id)
       Join sys.types ty On c.user_type_id = ty.user_type_id
       Left Join sys.foreign_key_columns fkc On fkc.parent_object_id = t.object_id And fkc.parent_column_id = c.column_id
       Left Join sys.tables ft On fkc.referenced_object_id = ft.object_id
       Left Join sys.schemas fs On ft.schema_id = fs.schema_id
       Left Join sys.columns fc On fkc.referenced_object_id = fc.object_id And fkc.referenced_column_id = fc.column_id
Order By 1, 2, 3";

            public string GetFetchSQL(Version v)
            {
                if (v >= SQLServerVersions.SQL2008.RTM)
                    return string.Format(FetchSQL, FetchSQL2008Columns);

                return string.Format(FetchSQL, "");
            }

        }

    }
}
