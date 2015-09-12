using System;
using System.Collections.Generic;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<SQLDatabaseInfo>> _databases;
        public Cache<List<SQLDatabaseInfo>> Databases => _databases ?? (_databases = SqlCacheList<SQLDatabaseInfo>(5 * 60));

        private Cache<List<SQLDatabaseBackupInfo>> _databaseBackups;
        public Cache<List<SQLDatabaseBackupInfo>> DatabaseBackups => _databaseBackups ?? (_databaseBackups = SqlCacheList<SQLDatabaseBackupInfo>(5 * 60));

        private Cache<List<SQLDatabaseFileInfo>> _databaseFiles;
        public Cache<List<SQLDatabaseFileInfo>> DatabaseFiles => _databaseFiles ?? (_databaseFiles = SqlCacheList<SQLDatabaseFileInfo>(5 * 60));

        public Cache<List<SQLDatabaseTableInfo>> GetTableInfo(string databaseName)
        {
            return new Cache<List<SQLDatabaseTableInfo>>
                {
                    CacheKey = GetCacheKey("TableInfo-" + databaseName),
                    CacheForSeconds = 60,
                    CacheStaleForSeconds = 5 * 60,
                    UpdateCache = UpdateFromSql("Table Info for " + databaseName, conn =>
                        {
                            var sql = $"Use [{databaseName}]; {GetFetchSQL<SQLDatabaseTableInfo>()}";
                            return conn.QueryAsync<SQLDatabaseTableInfo>(sql);
                        })
                };
        }

        public Cache<List<SQLDatabaseColumnInfo>> GetColumnInfo(string databaseName)
        {
            return new Cache<List<SQLDatabaseColumnInfo>>
            {
                CacheKey = GetCacheKey("ColumnInfo-" + databaseName),
                CacheForSeconds = 5 * 60,
                CacheStaleForSeconds = 30 * 60,
                UpdateCache = UpdateFromSql("Column Info for " + databaseName, conn =>
                {
                    var sql = $"Use [{databaseName}]; {GetFetchSQL<SQLDatabaseColumnInfo>()}";
                    return conn.QueryAsync<SQLDatabaseColumnInfo>(sql);
                })
            };
        }

        private Cache<List<SQLDatabaseVLFInfo>> _databaseVLFs;
        public Cache<List<SQLDatabaseVLFInfo>> DatabaseVLFs => _databaseVLFs ?? (_databaseVLFs = SqlCacheList<SQLDatabaseVLFInfo>(10 * 60, 60, affectsStatus: false));

        public static List<string> SystemDatabaseNames = new List<string>
            {
                "master",
                "model",
                "msdb",
                "tempdb"
            };

        public class SQLDatabaseInfo : ISQLVersionedObject, IMonitorStatus
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
                        return "Database is read-only";

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
            public CompatabilityLevels CompatbilityLevel { get; internal set; }
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
              Group By database_id) sti On db.database_id = sti.database_id {1}";

            public string GetFetchSQL(Version version)
            {
                if (version >= SQLServerVersions.SQL2012.RTM)
                    return string.Format(FetchSQL, FetchSQL2012Columns, FetchSQL2012Joins);

                return string.Format(FetchSQL, "", "");
            }
        }

        public class SQLDatabaseBackupInfo : ISQLVersionedObject
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            public int Id { get; internal set; }
            public string Name { get; internal set; }

            public DateTime? LastBackupStartDate { get; internal set; }
            public DateTime? LastBackupFinishDate { get; internal set; }
            public decimal? LastBackupSizeBytes { get; internal set; }
            public decimal? LastBackupCompressedSizeBytes { get; internal set; }
            public MediaDeviceTypes? LastBackupMediaDeviceType { get; internal set; }
            public string LastBackupLogicalDeviceName { get; internal set; }
            public string LastBackupPhysicalDeviceName { get; internal set; }

            public DateTime? LastFullBackupStartDate { get; internal set; }
            public DateTime? LastFullBackupFinishDate { get; internal set; }
            public decimal? LastFullBackupSizeBytes { get; internal set; }
            public decimal? LastFullBackupCompressedSizeBytes { get; internal set; }
            public MediaDeviceTypes? LastFullBackupMediaDeviceType { get; internal set; }
            public string LastFullBackupLogicalDeviceName { get; internal set; }
            public string LastFullBackupPhysicalDeviceName { get; internal set; }

            internal const string FetchSQL = @"
Select db.database_id Id,
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

        public class SQLDatabaseFileInfo : ISQLVersionedObject
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            public string VolumeId { get; internal set; }
            public string VolumeMountPoint { get; internal set; }
            public string LogicalVolumeName { get; internal set; }
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

            public double AvgReadStallMs => NumReads == 0 ? 0 : StallReadMs / NumReads;
            public double AvgWriteStallMs => NumWrites == 0 ? 0 : StallWriteMs / NumWrites;
            public long FileSizeBytes => FileSizePages*8*1024;

            public string GrowthDescription
            {
                get
                {
                    if (FileGrowthRaw == 0) return "None";

                    if (FileIsPercentGrowth) return FileGrowthRaw + "%";

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

            internal const string FetchSQL = @"
Select vs.volume_id VolumeId,
       vs.volume_mount_point VolumeMountPoint, 
       vs.logical_volume_name LogicalVolumeName,
       mf.database_id DatabaseId,
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
       fs.num_of_writes NumWrites
  From sys.dm_io_virtual_file_stats(null, null) fs
       Join sys.master_files mf 
         On fs.database_id = mf.database_id
         And fs.file_id = mf.file_id
       Cross Apply sys.dm_os_volume_stats(mf.database_id, mf.file_id) vs";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }

        public class SQLDatabaseTableInfo : ISQLVersionedObject
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

            internal const string FetchSQL = @"
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

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }

        public class SQLDatabaseColumnInfo : ISQLVersionedObject
        {
            public Version MinVersion => SQLServerVersions.SQL2005.RTM;

            public string Id => Schema + "." + TableName + "." + ColumnName;

            public string Schema { get; internal set; }
            public string TableName { get; internal set; }
            public int Position { get; internal set; }
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
                            props.Add($"{DataType}({Scale},{Precision})");
                            break;
                        default:
                            props.Add(DataType);
                            break;
                    }
                    props.Add(IsNullable ? "null" : "not null");
                    return string.Join(", ", props);
                }
            }

            internal const string FetchSQL = @"
Select s.name [Schema],
       t.name TableName,
       c.column_id Position,
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
       c.is_filestream IsFileStream,
       c.is_sparse IsSparse,
       c.is_column_set IsColumnSet,
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
       Join sys.tables t On c.object_id = t.object_id
       Join sys.schemas s On t.schema_id = s.schema_id
       Join sys.types ty On c.user_type_id = ty.user_type_id
       Left Join sys.foreign_key_columns fkc On fkc.parent_object_id = t.object_id And fkc.parent_column_id = c.column_id
       Left Join sys.tables ft On fkc.referenced_object_id = ft.object_id
       Left Join sys.schemas fs On ft.schema_id = fs.schema_id
       Left Join sys.columns fc On fkc.referenced_object_id = fc.object_id And fkc.referenced_column_id = fc.column_id
Order By 1, 2, 3";

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

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }

        public class SQLDatabaseVLFInfo : ISQLVersionedObject
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

Declare @sql nvarchar(max);
Set @sql = '';
Select @sql = @sql + ' Insert #vlfTemp Exec ' + QuoteName(name) + '.sys.sp_executesql N''DBCC LOGINFO WITH NO_INFOMSGS'';
  Insert #VLFCounts Select ' + Cast(database_id as nvarchar(10)) + ',''' + name + ''', Count(*) From #vlfTemp;
  Truncate Table #vlfTemp;'
  From sys.databases
  Where state <> 6; -- Skip OFFLINE databases as they cause errors

Exec sp_executesql @sql;
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
        
    }
}
