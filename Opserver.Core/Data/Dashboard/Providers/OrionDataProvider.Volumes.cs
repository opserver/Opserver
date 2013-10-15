using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public partial class OrionDataProvider
    {
        public override List<Volume> AllVolumes
        {
            get { return VolumeCache.Data ?? new List<Volume>(); }
        }

        private Cache<List<Volume>> _volumeCache;
        public Cache<List<Volume>> VolumeCache
        {
            get { return _volumeCache ?? (_volumeCache = ProviderCache(GetAllVolumes, 10)); }
        }

        public List<Volume> GetAllVolumes()
        {
            using (MiniProfiler.Current.Step("Get Volumes"))
            {
                using (var conn = GetConnection())
                {
                    const string sql = @"
Select VolumeID as Id,
       NodeID as NodeId,
       LastSync,
       VolumeIndex as [Index],
       FullName as Name,
       Caption,
       VolumeDescription as [Description],
       VolumeType as Type,
       VolumeSize as Size,
       VolumeSpaceUsed as Used,
       VolumeSpaceAvailable as Available,
       VolumePercentUsed as PercentUsed
From Volumes";
                    var volumes = conn.Query<Volume>(sql, commandTimeout: QueryTimeoutMs).ToList();
                    volumes.ForEach(v => v.DataProvider = this);
                    return volumes;
                }
            }
        }
        
        public override IEnumerable<Volume.VolumeUtilization> GetUtilization(Volume volume, DateTime? start, DateTime? end, int? pointCount = null)
        {
            const string allSql = @"
Select v.DateTime,
       v.AvgDiskUsed,
       v.MaxDiskUsed,
       v.DiskSize,
       v.PercentDiskUsed,
       Row_Number() Over(Order By v.DateTime) as RowNumber
  From VolumeUsage v
 Where {dateRange}
   And v.VolumeID = @id";

            const string sampledSql = @"
Select * 
  From (Select v.DateTime,
               v.AvgDiskUsed,
               v.DiskSize,
               v.PercentDiskUsed,
               Row_Number() Over(Order By v.DateTime) as RowNumber
          From VolumeUsage v
         Where {dateRange}
           And v.VolumeID = @id) v
 Where v.RowNumber % ((Select Count(*) + @intervals
                         From VolumeUsage v
                        Where {dateRange}
                          And v.VolumeID = @id)/@intervals) = 0
 Order By v.DateTime";
            using (var conn = GetConnection())
            {
                return conn.Query<Volume.VolumeUtilization>(
                    (pointCount.HasValue ? sampledSql : allSql)
                        .Replace("{dateRange}", GetOptionalDateClause("v.DateTime", start, end)),
                    new {id = volume.Id, start, end, intervals = pointCount});
            }
        }
    }
}
