using System;
using System.Collections.Generic;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        private Cache<List<VolumeInfo>> _volumes;
        public Cache<List<VolumeInfo>> Volumes
        {
            get { return _volumes ?? (_volumes = SqlCacheList<VolumeInfo>(10*60)); }
        }

        public class VolumeInfo : ISQLVersionedObject
        {
            public Version MinVersion { get { return SQLServerVersions.SQL2008R2.SP1; } }

            public string VolumeId { get; internal set; }
            public string VolumeMountPoint { get; internal set; }
            public string LogicalVolumeName { get; internal set; }
            public string FileSystemType { get; internal set; }
            public long TotalBytes { get; internal set; }
            public long AvailableBytes { get; internal set; }
            public long UsedBytes { get { return TotalBytes - AvailableBytes; } }
            public bool IsReadOnly { get; internal set; }
            public bool IsCompressed { get; internal set; }
            public decimal AvgReadStallMS { get; internal set; }
            public decimal AvgWriteStallMS { get; internal set; }
            
            internal const string FetchSQL = @"
Select vs.volume_mount_point VolumeMountPoint, 
       vs.volume_id VolumeId, 
       vs.logical_volume_name LogicalVolumeName, 
       vs.file_system_type FileSystemType, 
       vs.total_bytes TotalBytes, 
       vs.available_bytes AvailableBytes, 
       vs.is_read_only IsReadOnly, 
       vs.is_compressed IsCompressed,
       Sum(fs.io_stall_read_ms) / Sum(fs.num_of_reads) as AvgReadStallMS,
       Sum(fs.io_stall_write_ms) / Sum(fs.num_of_writes) as AvgWriteStallMS
  From sys.dm_io_virtual_file_stats(null, null) fs
       Join sys.master_files mf 
         On fs.database_id = mf.database_id
         And fs.file_id = mf.file_id
       Cross Apply sys.dm_os_volume_stats(mf.database_id, mf.file_id) vs
 Group By vs.volume_mount_point, 
       vs.volume_id, 
       vs.logical_volume_name,
       vs.file_system_type,
       vs.total_bytes,
       vs.available_bytes,
       vs.is_read_only,
       vs.is_compressed";

            public string GetFetchSQL(Version v)
            {
                return FetchSQL;
            }
        }
    }
}